using UnityEngine;

public class Projectile : MonoBehaviour
{
    public ShipController firingShip;

    public ParticleSystem hitEffect;

    public Rigidbody2D rigidbody2d;

    private float timer = 1f;

    // Update is called once per frame
    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
            Destroy(gameObject);
    }

    public void Launch(Vector2 force)
    {
        rigidbody2d.AddForce(force);
    }

    // Pri kolizii s objektom
    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.gameObject != firingShip.gameObject)
        {
            // Efekt vybuchu
            hitEffect.Play();
            // Znici strelu pri zrazke s objektom
            Destroy(this.gameObject, 0.1f);
        }
    }
}
