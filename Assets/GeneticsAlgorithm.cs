﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class GeneticsAlgorithm : MonoBehaviour
{
    public AIControlledShip[] ships;

    public int maxSelectTime = 10;        // kazdych 5 sekund vyber najlepsieho hraca, ktory sa bude klonovat
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
        // Ak existuje uz iba posledna najlepsia lod
        this.selectTimer -= Time.deltaTime;
        if (this.selectTimer <= 0.0f)
        {
            // Ak maju vsetky lode 0 score z hry, pre pripad ze este hraju epizodu
            var count = ships.Where(s => s.fitness == 0f).Count();
            if (count == ships.Length)
                return;

            //Debug.Log($"count = {count}");

            AIControlledShip bestShip = ships[0];
            foreach (var ship in ships)
            {
                // Ak ma lod vyssie skore stava sa novou najlepsou v hre
                if (ship.fitness > bestShip.fitness)
                {
                    bestShip = ship;
                }                
            }
            Debug.Log($"bestShip.fitness = {bestShip.fitness}, Best ship is {bestShip.gameObject.name}!");

            // Vycisti pouzite score
            //foreach (var ship in ships)
            //{
            //    ship.fitness = 0f;
            //}

            // Skopiruj vedomost najlepsej lode do ostatnych lodi
            foreach (var ship in ships)
            {
                if (ship != bestShip)
                {                    
                    // Skopiruj parametre sieti
                    for (int i = 0; i < 4; i++)
                    {
                        for (int j = 0; j < bestShip.Qnet.neuronLayers[i].Weights.Count; j++)
                        {
                            if (randomGen.NextFloat() > ship.eps) // 10% nahoda, 85% podla vedomosti
                            {
                                ship.Qnet.neuronLayers[i].Weights[j] = bestShip.Qnet.neuronLayers[i].Weights[j];
                                ship.QTargetNet.neuronLayers[i].Weights[j] = bestShip.QTargetNet.neuronLayers[i].Weights[j];

                                ship.Qnet.neuronLayers[i].deltaWeights[j] = bestShip.Qnet.neuronLayers[i].deltaWeights[j];
                            }
                            else
                            {
                                ship.Qnet.neuronLayers[i].Weights[j] = randomGen.NextFloat(-1f, 1f);
                                ship.QTargetNet.neuronLayers[i].Weights[j] = bestShip.QTargetNet.neuronLayers[i].Weights[j];

                                ship.Qnet.neuronLayers[i].deltaWeights[j] = 0f;//bestShip.Qnet.neuronLayers[i].deltaWeights[j];
                            }
                        }
                    }
                }
            }
            this.selectTimer = maxSelectTime;
        }
    }
}

