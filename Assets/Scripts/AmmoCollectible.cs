using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AmmoCollectible : MonoBehaviour
{
    // Mnozstvo municie ziskane z tohoto baliku
    public int amount = 20;

    // Zvukovy klip
    public AudioClip collectibleClip;

    // Pri kolizii
    void OnTriggerEnter2D(Collider2D other)
    {
        ShipController controller = other.GetComponent<ShipController>();

        if (controller != null)
        {
            // Pouzi zivot ked nemas plny zivot
            if (controller.Ammo < controller.maxAmmo)
            {
                // Pridaj zivot hracovi
                controller.ChangeAmmo(amount);

                // Prehraj klip
                controller.PlaySound(collectibleClip);

                // Znic objekt zivota
                Destroy(gameObject);
            }
        }
    }
}
