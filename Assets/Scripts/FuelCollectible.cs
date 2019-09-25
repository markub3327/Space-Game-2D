using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FuelCollectible : MonoBehaviour
{
    // Zvukovy klip
    public AudioClip collectibleClip;

    // Pri kolizii
    void OnTriggerStay2D(Collider2D other)
    {
        ShipController controller = other.GetComponent<ShipController>();

        if (controller != null)
        {
            // Pouzi zivot ked nemas plny zivot
            if (controller.Fuel < controller.maxFuel)
            {
                // Pridaj zivot hracovi
                controller.ChangeFuel(1);

                // Prehraj klip
                controller.PlaySound(collectibleClip);
            }
        }
    }

}
