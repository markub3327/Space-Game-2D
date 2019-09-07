using UnityEngine;
using System.Collections;

public class ParallaxLayer : MonoBehaviour {

	Vector3 wantedPosition;
	public float movement_resistance = 1f; //1 = no movement, 0.9 = some movement, 0.5 = more movement, etc, 0 = centered at origin, layer is now foreground

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void FixedUpdate () {
		wantedPosition = Camera.main.transform.position * movement_resistance;
			wantedPosition.z = transform.position.z;
			transform.position = wantedPosition;
			
	}
}
