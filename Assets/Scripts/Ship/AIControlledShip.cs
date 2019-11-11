using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using System.Collections.Generic;

public class AIControlledShip : ShipController
{
    private static EntityManager entityManager;

    public override void Start()
    {
        base.Start();

        // Set entityManager
        entityManager = World.Active.GetExistingManager<EntityManager>();

        List<Entity> entities = new List<Entity>();
        // Create entity prefab from the game object hierarchy once
        for (int i = 0; i < 10; i++)
        {
            var entity = entityManager.CreateEntity(typeof(Neuron));        

            var buffer = entityManager.AddBuffer<NeuronInput>(entity);
            buffer.Add(new NeuronInput { input = i * 0.8f });

            entityManager.AddBuffer<NeuronEdge>(entity);

            var buffer2 = entityManager.AddBuffer<NeuronWeight>(entity);
            buffer2.Add(new NeuronWeight { weight = 0.2f });

            entities.Add(entity);
        }

        // Create entity prefab from the game object hierarchy once
        for (int i = 0; i < entities.Count; i++)
        {
            var entity = entityManager.CreateEntity(typeof(Neuron));            

            entityManager.AddBuffer<NeuronInput>(entity);

            var buffer = entityManager.AddBuffer<NeuronEdge>(entity);
            for (int j = 0; j < entities.Count; j++)
                buffer.Add(new NeuronEdge { entity = entities[j] });

            var buffer2 = entityManager.AddBuffer<NeuronWeight>(entity);
            buffer2.Add(new NeuronWeight { weight = 0.4f });
        }
    }
    public override void Update()
    {
        base.Update();        
    }   
}