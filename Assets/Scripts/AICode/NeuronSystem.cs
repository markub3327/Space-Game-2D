using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

public class NeuronSystem : JobComponentSystem
{
    [BurstCompile]
    private struct NeuronJob : IJobParallelFor
    {
        // Rezia neuronu
        [NativeDisableParallelForRestriction, ReadOnly]
        public BufferFromEntity<NeuronInput> Inputs;

        [NativeDisableParallelForRestriction]
        public BufferFromEntity<NeuronWeight> Weights;

        [NativeDisableParallelForRestriction, ReadOnly]
        public BufferFromEntity<NeuronEdge> Edges;

        // Rezia vlakna
        [NativeDisableParallelForRestriction]
        public ComponentDataFromEntity<Neuron> allNeurons;
        
        [DeallocateOnJobCompletion, ReadOnly]
        public NativeArray<Entity> entities; // The request entities

        // Praca neuronu
        public void Execute(int index)
        {     
            var entity = this.entities[index];
            var neuron = allNeurons[entity];

            var inputsBuffer = this.Inputs[entity];
            var weightsBuffer = this.Weights[entity];
            var edgesBuffer = this.Edges[entity];

            neuron.Output = 0f;
            neuron.State = NeuronState.IsReady; 
            for (int i = 0; i < edgesBuffer.Length; i++)
            {
                var edgeNeuron = allNeurons[edgesBuffer[i].entity];
                if (edgeNeuron.State == NeuronState.Calculating)
                {
                    neuron.State = NeuronState.Calculating;
                    break;
                }
                neuron.Output +=  edgeNeuron.Output * weightsBuffer[0].weight;
            }
            if (neuron.State == NeuronState.IsReady)
            {
                for (int i = 0; i < inputsBuffer.Length; i++)
                {
                    neuron.Output += inputsBuffer[i].input * weightsBuffer[i].weight;
                }
                neuron.Output = NeuronMathf.ELU(neuron.Output, 0.6f);

                Debug.Log($"{entity}: output = {neuron.Output}, state = {neuron.State.ToString()}");
            }

            // Copy changes back to nenuron
            allNeurons[entity] = neuron;
        }        
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var entities  = EntityManager.GetAllEntities(Allocator.TempJob);

        var job = new NeuronJob
        {
            entities = entities,
            allNeurons = GetComponentDataFromEntity<Neuron>(false),
            Inputs = GetBufferFromEntity<NeuronInput>(true),
            Weights = GetBufferFromEntity<NeuronWeight>(false),
            Edges = GetBufferFromEntity<NeuronEdge>(true)            
        };
        return job.Schedule(entities.Length, 1, inputDeps);
    }
}