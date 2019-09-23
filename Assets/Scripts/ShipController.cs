using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipController : MonoBehaviour
{
    public ParticleSystem[] Motors;

    public float speed = 3.0f;

    public int maxHealth = 10;
    public float timeInvincible = 2.0f;

    public int health { get; private set; }
    bool isInvincible;
    float invincibleTimer;

    Rigidbody2D rigidbody2d;

    // Start is called before the first frame update
    void Start()
    {
        rigidbody2d = GetComponent<Rigidbody2D>();

        health = maxHealth;
    }

    // Update is called once per frame
    void Update()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector2 move = new Vector2(horizontal, vertical);

        // Ak je lod v pohybe zapni particle system
        if (!Mathf.Approximately(move.x, 0.0f) || !Mathf.Approximately(move.y, 0.0f))
        {           
            foreach (var motor in Motors)
            {
                if (!motor.isEmitting)
                    motor.Play();
            }
        }
        else
        {
            foreach (var motor in Motors)
            {
                motor.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        Vector2 position = rigidbody2d.position;

        position += move * speed * Time.deltaTime;

        rigidbody2d.MovePosition(position);

        if (isInvincible)
        {
            invincibleTimer -= Time.deltaTime;
            if (invincibleTimer < 0)
                isInvincible = false;
        }
    }

    public void ChangeHealth(int amount)
    {
        if (amount < 0)
        {
            if (isInvincible)
                return;

            isInvincible = true;
            invincibleTimer = timeInvincible;
        }

        health = Mathf.Clamp(health + amount, 0, maxHealth);

        UIHealthBar.instance.SetValue(health / (float)maxHealth);
    }
}