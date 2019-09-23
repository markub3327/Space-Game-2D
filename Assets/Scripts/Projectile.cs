using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public GameObject firingShip;

    Rigidbody2D rigidbody2d;

    // Start is called before the first frame update
    void Awake()
    {
        rigidbody2d = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        if (transform.position.magnitude > 1000.0f)
        {
            Destroy(gameObject);
        }
    }

    public void Launch(Vector2 force)
    {
        rigidbody2d.AddForce(force);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject != firingShip)
        {
            //we also add a debug log to know what the projectile touch
            Debug.Log("Projectile Collision with " + other.gameObject);

            Destroy(gameObject);
        }
    }
}
