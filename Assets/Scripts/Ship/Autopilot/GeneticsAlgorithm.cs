using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.IO;

public class GeneticsAlgorithm : MonoBehaviour
{
    public AgentDDQN bestAgent = null;

    public List<AgentDDQN> agents;

    public void Start()
    {
        // Random seed
        UnityEngine.Random.InitState((int)System.DateTime.Now.Ticks); 
    }

    public void Update()
    {        
        // Ak vsetci hraci su zniceny obnov populaciu hracov
        if (agents.Where(p => p.IsDestroyed == false).Count() == 0)
        {
            // Replace agents with agents list ordered by his fitness
            this.agents.Sort(new AgentsComparer());
            this.bestAgent = agents[0];
            Debug.Log($"The best agent is {bestAgent.name}, fitness = {bestAgent.score}");
            Debug.Log($"The worst agent is {agents[agents.Count-1].name}, fitness = {agents[agents.Count-1].score}");

            if (agents[0].episode >= 1000) 
            {   
            
                   #if UNITY_EDITOR
                        UnityEditor.EditorApplication.isPlaying = false;
                    #else
                        Application.Quit();
                    #endif
            }

            // Krizenie
            this.Crossover();

            // obnova lodi - respawn
            foreach (var a in agents)
            {
                a.score = 0.0f;
                a.RespawnShip();
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
                Debug.Log($"parrentA={agents.IndexOf(parrentA)}, parrentB={agents.IndexOf(parrentB)}");

                for (int i = 0; i < a.QNet.neuronLayers.Count; i++)
                {
                    for (int k = 0; k < a.QNet.neuronLayers[i].neurons.Length; k++)
                    {
                        // Random slicing point
        	    		slicing_point = Random.Range(1, a.QNet.neuronLayers[i].neurons[k].weights.Length-1);

                        // first part of the child's chromosome contains the parrentA genes    
                        for (int j = 0; j < slicing_point; j++)
                        {                        
    		    			a.QNet.neuronLayers[i].neurons[k].weights[j] = parrentA.QNet.neuronLayers[i].neurons[k].weights[j];
    		    			a.QTargetNet.neuronLayers[i].neurons[k].weights[j] = parrentA.QTargetNet.neuronLayers[i].neurons[k].weights[j];
	    			    }

    				    // second part of the child's chromosome contains the parrentB genes
	    			    for (int j = slicing_point; j < a.QNet.neuronLayers[i].neurons[k].weights.Length; j++)
    		    		{
        		    		a.QNet.neuronLayers[i].neurons[k].weights[j] = parrentB.QNet.neuronLayers[i].neurons[k].weights[j];
    	    	    		a.QTargetNet.neuronLayers[i].neurons[k].weights[j] = parrentB.QTargetNet.neuronLayers[i].neurons[k].weights[j];
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

            //using (var outf = new StreamWriter("DDQN_bestAgent_fitness.log"))
            //{
            //    for (int i = 0; i < fitnessList.Count; i++)
            //    {
            //        outf.WriteLine($"fitness={fitnessList[i]};error={errorList[i]}");
            //    }
            //}

            Debug.Log("DDQN saved!");
        }
    }
}

public class AgentsComparer : IComparer<AgentDDQN>
{
    public int Compare(AgentDDQN x, AgentDDQN y)
    {
        return x.score.CompareTo(y.score) * (-1);
    }
}