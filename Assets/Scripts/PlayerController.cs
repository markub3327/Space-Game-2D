using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{

    public float torque = 150.0f;

    public float speed = 20.0f;

    private Rigidbody2D rb2D;

    private ParticleSystem[] pSystems;

    // Start is called before the first frame update
    void Start()
    {
        rb2D = GetComponent<Rigidbody2D>();
        pSystems = GetComponentsInChildren<ParticleSystem>();
    }


    // Update is called once per frame
    void FixedUpdate()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        // Pridaj silu do rotacie lode
        rb2D.AddTorque(torque * (-h) * Time.deltaTime, ForceMode2D.Force);

        // Pridaj silu do pohybu lode
        rb2D.AddRelativeForce(new Vector2((speed * v * Time.deltaTime), 0.0F), ForceMode2D.Impulse);

        // Zapni animaciu pohonu
        if (System.Math.Abs(h) > 0.0f || System.Math.Abs(v) > 0.0f)
        {
            foreach (var pSystem in pSystems)
            {
                if (!pSystem.isEmitting)
                    pSystem.Play(true);
            }
        }
    }
}