using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{

    public float torque = 150.0f;

    public float speed = 20.0f;

    private Rigidbody2D rb2D;

    private ParticleSystem[] pSystems;

    private float axisH = 0;

    private float axisV = 0;

    // Start is called before the first frame update
    void Start()
    {
        rb2D = GetComponent<Rigidbody2D>();
        pSystems = GetComponentsInChildren<ParticleSystem>();
    }

    private void Update()
    {
        axisH = Input.GetAxis("Horizontal");
        axisV = Input.GetAxis("Vertical");

        // Zapni animaciu & zvuk pohonu
        if (System.Math.Abs(axisH) > 0.0f || System.Math.Abs(axisV) > 0.0f)
        {
            foreach (var pSystem in pSystems)
            {
                if (!pSystem.isEmitting)
                    pSystem.Play();
            }
        }
        else
        {
            foreach (var pSystem in pSystems)
            {
                pSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }
    }


    // Update is called once per frame
    private void FixedUpdate()
    {        
        // Pridaj silu do rotacie lode
        rb2D.AddTorque(torque * (-axisH) * Time.deltaTime, ForceMode2D.Force);

        // Pridaj silu do pohybu lode
        rb2D.AddRelativeForce(new Vector2((speed * axisV * Time.deltaTime), 0.0F), ForceMode2D.Impulse);        
    }
}