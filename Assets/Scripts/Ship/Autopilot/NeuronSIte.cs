using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;
using UnityEngine;

public class NeuronSite
{

    private struct NeuronsJob : IJobParallelFor
    {        
        NativeArray<> XXX = new NativeArray<>(size, Allocator.TempJob);

        public void Execute(int index)
        {

        }
    }

    public void Run()
    {
        var job = new NeuronsJob
        {

        };

        // Rozdelenie prace medzi CPU
        var batchSize = 1 / SystemInfo.processorCount;
        Debug.Log($"batchSize = {batchSize}, Num. of CPUs = {batchSize}");

        job.Schedule(1, batchSize).Complete();        
    }
}