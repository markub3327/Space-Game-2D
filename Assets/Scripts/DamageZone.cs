using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageZone : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        ShipController controller = other.GetComponent<ShipController>();

        if (controller != null)
        {
            controller.ChangeHealth(-1);
        }
    }
}
