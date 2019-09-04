using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float torque;

    public float speed;

    private Rigidbody2D rb2D;

    // Use this for initialization
    void Start()
    {
        rb2D = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        rb2D.AddTorque(torque * (-h) * Time.deltaTime, ForceMode2D.Force);

        rb2D.AddRelativeForce(new Vector2((speed * v * Time.deltaTime), 0.0F), ForceMode2D.Impulse);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        
    }
}
