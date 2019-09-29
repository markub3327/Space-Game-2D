using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AsteroidController : MonoBehaviour
{
    public float rotationSpeed = 20;

    public Transform starTransform;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        // Spin the object around the world origin at 20 degrees/second.
        transform.RotateAround(new Vector3(starTransform.position.x, (starTransform.position.y/2.0f), starTransform.position.z), Vector3.forward, rotationSpeed * Time.deltaTime);
    }	
}
