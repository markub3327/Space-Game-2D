using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.IO;

public class GeneticsAlgorithm : MonoBehaviour
{
    public AgentDDQN bestAgent = null;

    public List<AgentDDQN> agents;

    private float tau=0.01f;

    private List<float> fitnessList = new List<float>();

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

            // Soft update Q Target network
            foreach (var a in agents)
    		{
                if (a != bestAgent)
			    {
                    for (int j = 0; j < a.QNet.neuronLayers.Count; j++)
                    {
                        for (int k = 0; k < a.QNet.neuronLayers[j].Weights.Count; k++)
                        {
                            a.QTargetNet.neuronLayers[j].Weights[k] = tau*a.QNet.neuronLayers[j].Weights[k] + (1.0f-tau)*a.QTargetNet.neuronLayers[j].Weights[k];                    
                        }
                    }
                }
            }

            this.fitnessList.Add(bestAgent.fitness);
            for (int i = 0; i < agents.Count; i++)
            {
                Debug.Log($"order = {i+1}., name = {agents[i].name}, fitness = {agents[i].fitness}");   
                agents[i].fitness = 0.0f;
                agents[i].presiel10Epizod = false;                
            }
            for (int j = 0; j < bestAgent.wMean.Length; j++)
            {
                Debug.Log($"wMean[{j}]={bestAgent.wMean[j]}");
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
                AgentDDQN parrentA=null, parrentB=null;
				RouletteWheelMechanism(ref parrentA, ref parrentB);

                for (int i = 0; i < a.QNet.neuronLayers.Count; i++)
                {
                    // Random slicing point
    	    		slicing_point = (Random.Range(1, a.QNet.neuronLayers[i].Weights.Count-1));

                    // first part of the child's chromosome contains the parrentA genes    
                    for (int j = 0; j < slicing_point; j++)
                    {                        
    					a.QNet.neuronLayers[i].Weights[j] = parrentA.QNet.neuronLayers[i].Weights[j];
	    			}

    				// second part of the child's chromosome contains the parrentB genes
	    			for (int j = slicing_point; j < a.QNet.neuronLayers[i].Weights.Count; j++)
		    		{
    					a.QNet.neuronLayers[i].Weights[j] = parrentB.QNet.neuronLayers[i].Weights[j];
				    }
                }

                // Random slicing point
    	    	slicing_point = (Random.Range(1, a.wMean.Length-1));

                // first part of the child's chromosome contains the parrentA genes    
                for (int j = 0; j < slicing_point; j++)
                {                        
                    a.wMean[j] = parrentA.wMean[j];
                }
    			// second part of the child's chromosome contains the parrentB genes
	    		for (int j = slicing_point; j < a.wMean.Length; j++)
		    	{
                    a.wMean[j] = parrentB.wMean[j];
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
                        if (Random.Range(0.0f, 1.0f) < (0.01f/(float)a.QNet.neuronLayers[i].Weights.Count))                        
                        {
                            var newW = Random.Range(-1.0f, 1.0f) * k;
                            a.QNet.neuronLayers[i].Weights[j] = newW;//0.0002f*newW + (1.0f-0.0002f)*a.QNet.neuronLayers[i].Weights[j];
                            Debug.Log($"Mutating W[{i}][{j}]({a.name})={newW}!");
                        }                        
                    }
                }

                for (int j = 0; j < a.wMean.Length; j++)
                {                        
                    if (Random.Range(0.0f, 1.0f) < (0.1f/(float)a.wMean.Length))                        
                    {
                        float newW;
                        do {
                            newW = Random.Range(0.0f, 1.0f);
                        } while (newW == 0f);
                        a.wMean[j] = newW;
                        //Debug.Log($"Mutating LR[{i}][{j}]({a.name})={newLR}!");
                    }
                }                
            }
        }
    }

    void RouletteWheelMechanism(ref AgentDDQN idxA, ref AgentDDQN idxB)
	{
		float[] probability = new float[agents.Count];
		float sum = 0.0f;

		// Softmax function
		/*********************************************************/
		for (int i = 0; i < agents.Count; i++)
		{
			sum += Unity.Mathematics.math.exp(agents[i].fitness);
		}
		for (int i = 0; i < agents.Count; i++)
		{
			probability[i] = Unity.Mathematics.math.exp(agents[i].fitness) / sum;
			//Debug.Log($"probability = {probability[i]}");
			//std::cout << "fitness = " << this->populations[i]->getFitness() << std::endl;
		}
		//std::cout << std::endl;
		/*********************************************************/

		// Pseudorandom selection
		/*********************************************************/
        do {
		float a = UnityEngine.Random.Range(0f, 1f);
		float b = UnityEngine.Random.Range(0f, 1f);
		float min = 0, max = 0;

        for (int i = 0; i < agents.Count; i++)
		{
            min = max;
			max += probability[i];

			// if is a value in range
			if (min <= a && a < max)
			{
				//std::cout << "a = " << a << std::endl;
				//std::cout << "interval = <" << min << "; " << max << ")" << std::endl;
				//std::cout << "index_of_selectedA = " << i << std::endl;
				//std::cout << "probability_of_selectedA = " << probability[i] << std::endl;
				//std::cout << "fitness_of_selectedA = " << this->populations[i]->getFitness() << std::endl;
				//break;

				idxA = agents[i];
                //Debug.Log($"idxA={i}");
			}

			// if is b value in range
			if (min <= b && b < max)
			{
				//std::cout << "b = " << b << std::endl;
				//std::cout << "interval = <" << min << "; " << max << ")" << std::endl;
				//std::cout << "index_of_selectedB = " << i << std::endl;
				//std::cout << "probability_of_selectedB = " << probability[i] << std::endl;
				//std::cout << "fitness_of_selectedB = " << this->populations[i]->getFitness() << std::endl;
				//break;

				idxB = agents[i];
                //Debug.Log($"idxB={i}");
			}
		}
        } while (idxA == idxB);
		//std::cout << std::endl;
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
                foreach (var x in fitnessList)
                {
                    outf.WriteLine($"fitness = {x}");
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