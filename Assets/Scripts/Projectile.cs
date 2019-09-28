using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public GameObject firingShip;

    public AudioClip hitClip;

    public ParticleSystem hitEffect;

    Rigidbody2D rigidbody2d;


    // Start is called before the first frame update
    void Awake()
    {
        rigidbody2d = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void FixedUpdate()
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

    // Pri kolizii s objektom
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject != firingShip)
        {
            //we also add a debug log to know what the projectile touch
            //Debug.Log("Projectile Collision with " + other.gameObject);

            switch (other.gameObject.tag)
            {
                // Ak zasiahol inu lod
                case "Player":
					{
						ShipController player = other.gameObject.GetComponent<ShipController>();

						if (player != null && player.enabled)
						{
							// Znizi zivoty hracovi
							player.ChangeHealth(-1);

							// Prehra zvuk zasahu lode
							player.PlaySound(hitClip);
						}
						break;
					}
                // Ak zasiahol asteroid
                case "Asteroid":
                    {
                        // Prehra zvuk zasahu asteroidu
                        firingShip.GetComponent<ShipController>().PlaySound(hitClip);
                        // Znic asteroid
                        Destroy(other.gameObject);
                        break;
                    }
            }

            // Efekt vybuchu
            hitEffect.Play();

            // Znici strelu pri zrazke s objektom
            Destroy(gameObject, 1.0f);
        }
    }
}
