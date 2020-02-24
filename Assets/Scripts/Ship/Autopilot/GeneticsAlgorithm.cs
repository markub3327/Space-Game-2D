using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.IO;

public class GeneticsAlgorithm : MonoBehaviour
{
    public AgentDDQN bestAgent = null;

    public List<AgentDDQN> agents;

    private List<float> fitnessList = new List<float>();
    private List<float> errorList = new List<float>();

    public void Update()
    {        
        if (agents.Where(p => p.presiel10Epizod == false).Count() == 0)
        {
            // Replace agents with agents list ordered by his fitness
            this.agents.Sort(new AgentsComparer());
            this.bestAgent = agents[0];
            //Debug.Log($"The best agent is {bestAgent.name}, fitness = {bestAgent.fitness}");

            // Krizenie
            this.Crossover();

            // Mutacia
            this.Mutation();
            
            this.fitnessList.Add(bestAgent.fitness);
            this.errorList.Add(bestAgent.avgErr);
            for (int i = 0; i < agents.Count; i++)
            {
                Debug.Log($"order = {i+1}., name = {agents[i].Nickname}, fitness = {agents[i].fitness}");   
                agents[i].fitness = 0.0f;
                agents[i].presiel10Epizod = false;                
            }            
        }
    }

    private void Crossover()
	{
        int slicing_point;

        foreach (var a in agents)
		{
            if (a != bestAgent)
			{
				// Pick 2 parents
                AgentDDQN parrentA, parrentB;
                do {
                    parrentA = agents[UnityEngine.Random.Range(0, agents.Count)];
                    parrentB = agents[UnityEngine.Random.Range(0, agents.Count)];
                } while (parrentA == parrentB);

                for (int i = 0; i < a.QNet.neuronLayers.Count; i++)
                {
                    // Random slicing point
    	    		slicing_point = (Random.Range(1, a.QNet.neuronLayers[i].Weights.Count-1));

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
                    var num_of_inputs = a.QNet.neuronLayers[i].Weights.Count / a.QNet.neuronLayers[i].Neurons.Count;
                    var k = Unity.Mathematics.math.sqrt(2f/num_of_inputs);
                    //Debug.Log($"num_of_inputs = {num_of_inputs}");

                    for (int j = 0; j < a.QNet.neuronLayers[i].Weights.Count; j++)
                    {                        
                        if (Random.Range(0.0f, 1.0f) < (0.03f/(float)a.QNet.neuronLayers[i].Weights.Count))                        
                        {
                            var newW = Random.Range(-1.0f, 1.0f) * k;
                            a.QNet.neuronLayers[i].Weights[j] = newW;//0.0002f*newW + (1.0f-0.0002f)*a.QNet.neuronLayers[i].Weights[j];
                            Debug.Log($"Mutating W[{i}][{j}]({a.Nickname})={newW}!");
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

            using (var outf = new StreamWriter("DDQN_bestAgent_fitness.log"))
            {
                for (int i = 0; i < fitnessList.Count; i++)
                {
                    outf.WriteLine($"fitness = {fitnessList[i]}, error = {errorList[i]}");
                }
            }

            Debug.Log("DDQN saved!");
        }
    }
}

public class AgentsComparer : IComparer<AgentDDQN>
{
    public int Compare(AgentDDQN x, AgentDDQN y)
    {
        return x.fitness.CompareTo(y.fitness) * (-1);
    }
}