using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.IO;

public class GeneticsAlgorithm : MonoBehaviour
{
    public AgentDDQN bestAgent = null;

    public AgentDDQN[] agents;

    protected Unity.Mathematics.Random randGen;

    private bool bestChanged = false;

    public void Start()
    {
        this.randGen = new Unity.Mathematics.Random((uint)System.DateTime.Now.Ticks);
    }

    public void Update()
    {
        foreach (var a in agents)
        {
            // kazdu 10tu hernu epizodu sa vygeneruje nova populacia
            if (!a.novyJedinec)
            {
                if (!bestAgent) bestAgent = a;
                else
                {
                    if (a.fitness > bestAgent.fitness)
                    {
                        bestAgent = a;
                        bestChanged = true;
                    }
                    else if (a != bestAgent)
                    {
                        a.fitness = 0f;
                    }
                }
            }
        }

        if (bestChanged)
        {
            foreach (var a in agents)
            {
                if (a != bestAgent || !a.novyJedinec)
                {
                    for (int i = 0; i < bestAgent.QNet.neuronLayers.Count; i++)
                    {
                        for (int j = 0; j < bestAgent.QNet.neuronLayers[i].Weights.Count; j++)
                        {                        
                            if (randGen.NextFloat() > 0.00001f)
                            {
                                a.QNet.neuronLayers[i].Weights[j] = bestAgent.QNet.neuronLayers[i].Weights[j];
                                a.QTargetNet.neuronLayers[i].Weights[j] = bestAgent.QTargetNet.neuronLayers[i].Weights[j];

                                a.QNet.neuronLayers[i].deltaWeights[j] = bestAgent.QNet.neuronLayers[i].deltaWeights[j];
                                a.QTargetNet.neuronLayers[i].deltaWeights[j] = bestAgent.QTargetNet.neuronLayers[i].deltaWeights[j];
                            }
                            else
                            {
                                a.QNet.neuronLayers[i].Weights[j] = a.QTargetNet.neuronLayers[i].Weights[j] = randGen.NextFloat(-1f, 1f);
                                a.QNet.neuronLayers[i].deltaWeights[j] = a.QTargetNet.neuronLayers[i].deltaWeights[j] = 0f;

                                Debug.Log($"Mutating W[{i}][{j}]!");
                            }
                        }
                    }
                    a.novyJedinec = true;
                }
            }
            bestChanged = false;

            Debug.Log($"best_fitness = {bestAgent.fitness}");
            bestAgent.fitness = 0f;
        }
    }
}

[System.Serializable]
public class JSON_NET
{
    public List<float> Weights;
}