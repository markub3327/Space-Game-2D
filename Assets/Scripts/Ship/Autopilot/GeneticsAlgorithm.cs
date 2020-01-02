using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.IO;

public class GeneticsAlgorithm : MonoBehaviour
{
    private AgentDDQN bestAgent = null;

    public List<AgentDDQN> agents;


    public void Start()
    {

    }

    public void Update()
    {        
        if (agents.Where(p => p.presiel100Epizod == true).Count() == agents.Count)
        {
            // Replace agents with agents list ordered by his fitness
            this.agents.Sort(new AgentsComparer());
            this.bestAgent = agents[0];
            Debug.Log($"The best agent is {bestAgent.name}, fitness = {bestAgent.fitness}");

            // Krizenie
            this.Crossover();

            // Mutacia
            this.Mutation();

            for (int i = 0; i < agents.Count; i++)
            {
                Debug.Log($"order = {i+1}., name = {agents[i].name}, fitness = {agents[i].fitness}");
                agents[i].presiel100Epizod = false;
                agents[i].fitness = 0f;
            }
        }
    }

    private void Crossover()
	{
        foreach (var a in agents)
		{
            if (a != bestAgent)
			{
				// Pick 2 parents
                AgentDDQN parrentA, parrentB;
                do {
    				parrentA = agents[Random.Range(0, agents.Count)];
	    			parrentB = agents[Random.Range(0, agents.Count)];
                } while (parrentA == parrentB);
				//RouletteWheelMechanism(&_parrentA, &_parrentB);

                for (int i = 0; i < a.QNet.neuronLayers.Count; i++)
                {
                    // Random slicing point
    	    		var slicing_point = (Random.Range(1, a.QNet.neuronLayers[i].Weights.Count-1));

                    // first part of the child's chromosome contains the parrentA genes    
                    for (int j = 0; j < slicing_point; j++)
                    {                        
    					a.QNet.neuronLayers[i].Weights[j] = parrentA.QNet.neuronLayers[i].Weights[j];
    					a.QTargetNet.neuronLayers[i].Weights[j] = parrentA.QTargetNet.neuronLayers[i].Weights[j];
	    			}

    				// second part of the child's chromosome contains the parrentB genes
	    			for (int j = slicing_point; j < a.QNet.neuronLayers[i].Weights.Count; j++)
		    		{
    					a.QNet.neuronLayers[i].Weights[j] = parrentB.QNet.neuronLayers[i].Weights[j];
    					a.QTargetNet.neuronLayers[i].Weights[j] = parrentB.QTargetNet.neuronLayers[i].Weights[j];
				    }
                }
			}
		}
	}

    private void Mutation()
    {
        foreach (var a in agents)
        {
            if (a != bestAgent)
            {
                for (int i = 0; i < a.QNet.neuronLayers.Count; i++)
                {
                    for (int j = 0; j < a.QNet.neuronLayers[i].Weights.Count; j++)
                    {                        
                        if (Random.Range(0.0f, 1.0f) < (0.5f/(float)a.QNet.neuronLayers[i].Weights.Count))                        
                        {
                            a.QNet.neuronLayers[i].Weights[j] = Random.Range(-1.0f, 1.0f);
                            a.QTargetNet.neuronLayers[i].Weights[j] = Random.Range(-1.0f, 1.0f);
                            Debug.Log($"Mutating W[{i}][{j}]!");
                        }                            
                    }
                }
            }
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

public class AgentsComparer : IComparer<AgentDDQN>
{
    public int Compare(AgentDDQN x, AgentDDQN y)
    {
        if (x.fitness < y.fitness)
            return 1;
        else if (x.fitness > y.fitness)
            return -1;
        else
            return 0;
    }
}

[System.Serializable]
public class JSON_NET
{
    public List<float> Weights;
    //public List<float> Learning_rates;
    public List<float> error;
    //public List<float> Momentums;
}