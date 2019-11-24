using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeneticsAlgorithm : MonoBehaviour
{
    public AIControlledShip[] ships;

    private const float maxSelectTime = 5f;        // kazdych 5 sekund
    private float selectTimer;                // casovac uchovavajuci cas do vyberu noveho najsilnejsieho jedinca


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
                if (ship.fitness > bestShip.fitness)
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
                        ship.net2.neuronLayers[i].deltaWeights = ship.net1.neuronLayers[i].deltaWeights = bestShip.net1.neuronLayers[i].deltaWeights;
                        ship.net2.neuronLayers[i].Weights      = ship.net1.neuronLayers[i].Weights      = bestShip.net1.neuronLayers[i].Weights;
                    }
                }
            }
            this.selectTimer = maxSelectTime;
        }
    }
}
