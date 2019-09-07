using UnityEngine;
using System.Collections;

public class ExampleShipControl : MonoBehaviour {

	public float acceleration_amount = 1f;
	public float rotation_speed = 1f;
	public GameObject turret;
	public float turret_rotation_speed = 3f;
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	
		if (Input.GetKeyDown(KeyCode.Escape))
			Screen.lockCursor = !Screen.lockCursor;	
	
	
	
		if (Input.GetKey(KeyCode.W)) {
			GetComponent<Rigidbody2D>().AddForce(transform.up * acceleration_amount * Time.deltaTime);
		
		}
		if (Input.GetKey(KeyCode.S)) {
			GetComponent<Rigidbody2D>().AddForce((-transform.up) * acceleration_amount * Time.deltaTime);
			
		}
		
		if (Input.GetKey(KeyCode.A) && Input.GetKey(KeyCode.LeftShift)) {
			GetComponent<Rigidbody2D>().AddForce((-transform.right) * acceleration_amount * 0.6f  * Time.deltaTime);
			//print ("strafeing");
		}
		if (Input.GetKey(KeyCode.D) && Input.GetKey(KeyCode.LeftShift)) {
			GetComponent<Rigidbody2D>().AddForce((transform.right) * acceleration_amount * 0.6f  * Time.deltaTime);
			
		}
		
		if (Input.GetKey(KeyCode.D) && !Input.GetKey(KeyCode.LeftShift)) {
			GetComponent<Rigidbody2D>().AddTorque(-rotation_speed  * Time.deltaTime);
			
		}
		if (Input.GetKey(KeyCode.A) && !Input.GetKey(KeyCode.LeftShift)) {
			GetComponent<Rigidbody2D>().AddTorque(rotation_speed  * Time.deltaTime);
			
		}	
		if (Input.GetKey(KeyCode.C)) {
			GetComponent<Rigidbody2D>().angularVelocity = Mathf.Lerp(GetComponent<Rigidbody2D>().angularVelocity, 0, rotation_speed * 0.06f * Time.deltaTime);
			GetComponent<Rigidbody2D>().velocity = Vector2.Lerp(GetComponent<Rigidbody2D>().velocity, Vector2.zero, acceleration_amount * 0.06f * Time.deltaTime);
		}	
		
		
		if (Input.GetKey(KeyCode.H)) {
			transform.position = new Vector3(0,0,0);
		}	
		
		
		
		
	}
}
