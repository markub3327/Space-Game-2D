using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeneticsAlgorithm : MonoBehaviour
{
    public AIControlledShip[] ships;

    public int maxSelectTime = 5;        // kazdych 5 sekund vyber najlepsieho hraca, ktory sa bude klonovat
    private float selectTimer;                // casovac uchovavajuci cas do vyberu noveho najsilnejsieho jedinca

    private Unity.Mathematics.Random randomGen = new Unity.Mathematics.Random((uint)System.DateTime.Now.Ticks);


    // Start is called before the first frame update
    void Start()
    {
        this.selectTimer = maxSelectTime;
    }

    // Update is called once per frame
    void Update()
    {
        AIControlledShip bestShip = ships[0];

        selectTimer -= Time.deltaTime;
        if (selectTimer < 0.0f)   // ak vyprsal cas
        {
            foreach (var ship in ships)
            {
                // Ak ma lod vyssie skore stava sa novou najlepsou v hre
                if (ship.fitness != 0f && ship.fitness > bestShip.fitness)
                {
                    bestShip = ship;
                }                
            }
            Debug.Log($"bestShip.fitness = {bestShip.fitness}");

            // Skopiruj vedomost najlepsej lode do ostatnych lodi
            foreach (var ship in ships)
            {
                if (ship != bestShip)
                {                    
                    // Skopiruj parametre sieti
                    for (int i = 0; i < 4; i++)
                    {
                        for (int j = 0; j < bestShip.net1.neuronLayers[i].Weights.Count; j++)
                        {
                            if (randomGen.NextFloat() <= 0.20f) // 20% nahoda, 80% podla vedomosti
                            {
                                ship.net2.neuronLayers[i].deltaWeights[j] = ship.net1.neuronLayers[i].deltaWeights[j] = 0f;
                                ship.net2.neuronLayers[i].Weights[j]      = ship.net1.neuronLayers[i].Weights[j]      = randomGen.NextFloat(-1f, 1f);
                            }
                            else
                            {
                                ship.net2.neuronLayers[i].deltaWeights[j] = ship.net1.neuronLayers[i].deltaWeights[j] = bestShip.net1.neuronLayers[i].deltaWeights[j];
                                ship.net2.neuronLayers[i].Weights[j]      = ship.net1.neuronLayers[i].Weights[j]      = bestShip.net1.neuronLayers[i].Weights[j];
                            }
                        }
                    }
                }
            }
            this.selectTimer = maxSelectTime;
        }
    }
}
