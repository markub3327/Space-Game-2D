using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoonController : MonoBehaviour
{
    public float rotationSpeed = 20;

    public Transform planetTransform;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        // Spin the object around the world origin at 20 degrees/second.
		transform.RotateAround(new Vector3(planetTransform.position.x, (planetTransform.position.y / 2.5f), planetTransform.position.z), Vector3.forward, rotationSpeed * Time.deltaTime);
	}
}
