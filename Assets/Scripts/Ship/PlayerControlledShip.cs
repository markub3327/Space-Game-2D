using UnityEngine;
using System.Collections;

public class PlayerControlledShip : ShipController
{
    // Use this for initialization
    public override void Start()
    {
        // Spusti prvu funkciu nadtriedy
        base.Start();        
    }

    // Update is called once per frame
    public void Update()
    {
        // Nacitaj zmenu polohy z klavesnice alebo joysticku
        float axisH = Input.GetAxis("Horizontal");
        float axisV = Input.GetAxis("Vertical");
        Vector2 move = new Vector2(axisH, axisV).normalized;

        // Ak je lod v pohybe zapni motory
        if (Mathf.Abs(move.magnitude) > 0.0f)
        {
            // Pohni s lodou podla vstupu od uzivatela
            MoveShip(move);
        }

        // Ak je lod znicena
        if (IsDestroyed)
        {
            respawnTimer -= Time.deltaTime;
            if (respawnTimer < 0.0f)
            {
                RespawnShip();
                animator.SetTrigger("Respawn");
            }
        }
    }
}
