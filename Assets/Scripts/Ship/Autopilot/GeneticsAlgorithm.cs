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
            agents.Sort(new AgentComparer());
            bestAgent = agents[0];
            Debug.Log($"best_fitness[{bestAgent.name}] = {bestAgent.fitness}");

            foreach (var a in agents)
            {
                if (a != bestAgent && !a.testMode)
                {
                    Debug.Log($"fitness[{a.name}] = {a.fitness}");

                    for (int i = 0; i < bestAgent.QNet.neuronLayers.Count; i++)
                    {
                        for (int j = 0; j < bestAgent.QNet.neuronLayers[i].Weights.Count; j++)
                        {                        
                            if (randGen.NextFloat() > (1f/(float)bestAgent.QNet.neuronLayers[i].Weights.Count))
                            {
                                a.QNet.neuronLayers[i].Weights[j] = bestAgent.QNet.neuronLayers[i].Weights[j];
                                a.QTargetNet.neuronLayers[i].Weights[j] = bestAgent.QTargetNet.neuronLayers[i].Weights[j];

                                a.QNet.neuronLayers[i].deltaWeights[j] = bestAgent.QNet.neuronLayers[i].deltaWeights[j];
                                a.QTargetNet.neuronLayers[i].deltaWeights[j] = bestAgent.QTargetNet.neuronLayers[i].deltaWeights[j];
                            }
                            else
                            {
                                a.QNet.neuronLayers[i].Weights[j] = randGen.NextFloat(-1f, 1f);
                                a.QTargetNet.neuronLayers[i].Weights[j] = randGen.NextFloat(-1f, 1f);

                                a.QNet.neuronLayers[i].deltaWeights[j] = a.QTargetNet.neuronLayers[i].deltaWeights[j] = 0f;

                                Debug.Log($"Mutating W[{i}][{j}]!");
                            }                            
                        }

                        for (int j = 0; j < bestAgent.QNet.neuronLayers[i].Neurons.Count; j++)
                        {
                            var neuron_QNet = a.QNet.neuronLayers[i].Neurons[j];
                            var neuron_QTargetNet = a.QTargetNet.neuronLayers[i].Neurons[j];

                            var neuron_QNet_best = bestAgent.QNet.neuronLayers[i].Neurons[j];
                            var neuron_QTargetNet_best = bestAgent.QTargetNet.neuronLayers[i].Neurons[j];

                            if (randGen.NextFloat() > (1f/(float)bestAgent.QNet.neuronLayers[i].Neurons.Count))
                            {
                                neuron_QNet.learning_rate = neuron_QNet_best.learning_rate;
                                //neuron_QNet.momentum = neuron_QNet_best.momentum;
                                neuron_QTargetNet.learning_rate = neuron_QTargetNet_best.learning_rate;
                                //neuron_QTargetNet.momentum = neuron_QTargetNet_best.momentum;
                            }
                            else
                            {                            
                                neuron_QNet.learning_rate = randGen.NextFloat(0f, 0.5f);
                                neuron_QTargetNet.learning_rate = randGen.NextFloat(0f, 0.5f);
                                Debug.Log($"Mutating learning_rate[{i}][{j}]!");
                                //Debug.Log($"Mutating momentum[{i}][{j}]!");
                            }

                            a.QNet.neuronLayers[i].Neurons[j] = neuron_QNet;
                            a.QTargetNet.neuronLayers[i].Neurons[j] = neuron_QTargetNet;
                        }
                    }
                    a.fitness = 0f;                    
                }
                a.presiel10Epizod = false;
            }
            Debug.Log($"The best agent is {bestAgent.name}");
            //bestChanged = false;
            bestAgent.fitness = 0f;
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
    //public List<float> Momentums;
}

class AgentComparer : IComparer<AgentDDQN>
{
    public int Compare(AgentDDQN x, AgentDDQN y)
    {
        return x.fitness.CompareTo(y.fitness);
    }
}