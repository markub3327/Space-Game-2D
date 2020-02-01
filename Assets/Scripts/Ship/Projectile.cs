using UnityEngine;

public class Projectile : MonoBehaviour
{
    public ShipController firingShip;

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
        // Po prejdeni maximalnej velkosti vektora pohybu strely objekt zanikne
        if (transform.position.magnitude > 20.0f)
        {
            Destroy(gameObject);
        }
    }

    public void Launch(Vector2 force)
    {
        rigidbody2d.AddForce(force);
    }

    // Pri kolizii s objektom
    void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.gameObject != firingShip.gameObject)
        {
            //Debug.Log($"Projectile {this.name} collide with {other.gameObject.name}");

            if (collider.gameObject.tag == "Player")
            {
                var controller = collider.gameObject.GetComponent<ShipController>();
                if (controller != null)
                    // Znizi zivoty hracovi
                    controller.ChangeHealth(-1);
            }

            // Prehra zvuk zasahu projektilu
            firingShip.PlaySound(hitClip);

            // Efekt vybuchu
            hitEffect.Play();

            // Znici strelu pri zrazke s objektom
            Destroy(this.gameObject, 0.1f);
        }
    }
}
