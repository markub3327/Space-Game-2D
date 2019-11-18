using Unity.Mathematics;
using UnityEngine;
using System.Collections.Generic;

public class AIControlledShip : ShipController
{

    private readonly float[][] inputTestVector = new float[][] 
    {
        new float[] { 0f, 0f },
        new float[] { 1f, 0f },
        new float[] { 0f, 1f },
        new float[] { 1f, 1f }
    };

    private readonly float[][] outputTestVector = new float[][] 
    {
        new float[] { 0f },
        new float[] { 1f },
        new float[] { 1f },
        new float[] { 0f }
    };

    public override void Start()
    {
        base.Start();
        
       
    }

    public override void Update()
    {
        base.Update();
        
        
    }   
}