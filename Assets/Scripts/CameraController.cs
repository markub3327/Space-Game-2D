using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour
{

	public Transform playerTransform;

	//public float follow_tightness = 1.0f;


	// Use this for initialization
	void Start()
    {

	}


    // LateUpdate is called after all Update functions have been called
    void LateUpdate()
    {
        // Nastav kamere novu poziciu
        transform.position = new Vector3(playerTransform.position.x, playerTransform.position.y, transform.position.z);
	}	
}