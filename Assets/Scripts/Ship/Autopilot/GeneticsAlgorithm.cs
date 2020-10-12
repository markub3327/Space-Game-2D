using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.IO;

public class GeneticsAlgorithm : MonoBehaviour
{
    private AgentDDQN bestAgent = null;

    public ShipController humanPlayer;

    private List<AgentDDQN> agents;
    
    private StreamWriter log_file;


    public void Start()
    {
        // Random seed
        UnityEngine.Random.InitState((int)System.DateTime.Now.Ticks); 

        agents = new List<AgentDDQN>(GetComponentsInChildren<AgentDDQN>());
        log_file = File.CreateText("bestAgent.log");
    }

    public void Update()
    {
        // Ak vsetci hraci su zniceny obnov populaciu hracov
        if ((agents.Count > 0 && agents.Where(p => p.IsDestroyed == false).Count() == 0) && (humanPlayer.enabled == true && humanPlayer.IsDestroyed == true))
        {
            // Replace agents with agents list ordered by his fitness
            this.agents.Sort(new AgentsComparer());
            this.bestAgent = agents[0];
            Debug.Log($"The best agent is {bestAgent.Nickname}, score = {bestAgent.score}");
            Debug.Log($"The worst agent is {agents[agents.Count-1].Nickname}, score = {agents[agents.Count-1].score}");

            if (agents[0].episode >= 1000) 
            {
                #if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
                #else
                    Application.Quit();
                #endif
            }

            // Krizenie
            //if (bestAgent.testMode == false)
            //    this.Crossover();

            // log bestAgent to file
            this.log_file.WriteLine($"{this.bestAgent.episode};{this.bestAgent.step};{this.bestAgent.score};{this.bestAgent.epLoss};{ReplayBuffer.Count};{this.bestAgent.myPlanets.Count};{this.bestAgent.Health};{this.bestAgent.Ammo};{this.bestAgent.Fuel}");

            // obnova lodi - respawn
            int idx;
            bool[] usedPoints = new bool[ShipController.respawnPoints.Length];
            foreach (var a in agents)
            {
                a.score = 0f;
                a.step = 0;
                a.epLoss = 0f;
                a.episode += 1;

                do {
                    idx = UnityEngine.Random.Range(0, ShipController.respawnPoints.Length);
                } while(usedPoints[idx] == true);
                usedPoints[idx] = true;
                a.RespawnShip(ShipController.respawnPoints[idx]);
            }

            // respawn human player
            this.humanPlayer.score = 0f;
            this.humanPlayer.step = 0;
            this.humanPlayer.episode += 1;

            do {
                idx = UnityEngine.Random.Range(0, ShipController.respawnPoints.Length);                    
            } while(usedPoints[idx] == true);
            usedPoints[idx] = true;
            this.humanPlayer.RespawnShip(ShipController.respawnPoints[idx]);
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
        if (this.bestAgent != null && this.bestAgent.testMode == false)
        {
            File.WriteAllText(Application.dataPath + "/DDQN_Weights_QNet.save", bestAgent.QNet.ToString());
            Debug.Log("DDQN saved!");
        }

        log_file.Close();
    }
}

public class AgentsComparer : IComparer<AgentDDQN>
{
    public int Compare(AgentDDQN x, AgentDDQN y)
    {
        return x.score.CompareTo(y.score) * (-1);
    }
}