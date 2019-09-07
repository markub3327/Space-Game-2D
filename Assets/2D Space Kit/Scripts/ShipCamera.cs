using UnityEngine;
using System.Collections;

public class ShipCamera : MonoBehaviour {

	public Transform target_object;
	public float follow_tightness;
	Vector3 wanted_position;
	
	

	
	
	// Use this for initialization
	void Start () {
	
	
	}
	
	// Update is called once per frame
	void FixedUpdate () {
		
		wanted_position = target_object.position;
		wanted_position.z = transform.position.z;
		transform.position = Vector3.Lerp(transform.position, wanted_position, Time.deltaTime * follow_tightness);
		
	}
	
	
	
}
