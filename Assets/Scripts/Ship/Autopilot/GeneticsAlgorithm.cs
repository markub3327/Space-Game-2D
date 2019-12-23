using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.IO;

public class GeneticsAlgorithm : MonoBehaviour
{
    public AgentDDQN bestAgent = null;

    public List<AgentDDQN> agents;

    protected Unity.Mathematics.Random randGen;

    //private bool bestChanged = false;

    public void Start()
    {
        this.randGen = new Unity.Mathematics.Random((uint)System.DateTime.Now.Ticks);
    }

    public void Update()
    {        
        if (agents.Where(p => p.presiel10Epizod == true).Count() == agents.Count)
        {
            bestAgent = agents[0];
            foreach (var a in agents)
            {
                if (a.fitness > bestAgent.fitness)
                    bestAgent = a;
            }

            foreach (var a in agents)
            {
                Debug.Log($"fitness[{a.name}] = {a.fitness}");

                if (a != bestAgent && !a.testMode)
                {
                    for (int i = 0; i < bestAgent.QNet.neuronLayers.Count; i++)
                    {
                        for (int j = 0; j < bestAgent.QNet.neuronLayers[i].Weights.Count; j++)
                        {                        
                            if (randGen.NextFloat() > (1f/(float)bestAgent.QNet.neuronLayers[i].Weights.Count))
                            {
                                a.QNet.neuronLayers[i].Weights[j] = bestAgent.QNet.neuronLayers[i].Weights[j];
                                a.QTargetNet.neuronLayers[i].Weights[j] = bestAgent.QTargetNet.neuronLayers[i].Weights[j];
                            }
                            else
                            {
                                a.QNet.neuronLayers[i].Weights[j] = randGen.NextFloat(-1f, 1f);
                                a.QTargetNet.neuronLayers[i].Weights[j] = randGen.NextFloat(-1f, 1f);
                                Debug.Log($"Mutating W[{i}][{j}]!");
                            }                            
                            a.QNet.neuronLayers[i].deltaWeights[j] = a.QTargetNet.neuronLayers[i].deltaWeights[j] = 0f;
                        }
                    }
                }
                a.presiel10Epizod = false;
                a.fitness = 0f;                    
            }
            Debug.Log($"The best agent is {bestAgent.name}");
        }
    }

    public void OnApplicationQuit()
    {
        // Ak existuje bestAgent
        if (this.bestAgent != null)
        {
            File.WriteAllText(Application.dataPath + "/DDQN_Weights_QNet.save", bestAgent.QNet.ToString());
            File.WriteAllText(Application.dataPath + "/DDQN_Weights_QTargetNet.save", bestAgent.QTargetNet.ToString());                        
            Debug.Log("DDQN saved!");
        }
    }
}

[System.Serializable]
public class JSON_NET
{
    public List<float> Weights;
    public List<float> Learning_rates;
    //public List<float> error;
    //public List<float> Momentums;
}