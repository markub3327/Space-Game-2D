﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthCollectible : MonoBehaviour
{
    // Zvukovy klip
    public AudioClip collectibleClip;

    // Pri kolizii
    void OnTriggerEnter2D(Collider2D other)
    {
        ShipController controller = other.GetComponent<ShipController>();

        if (controller != null)
        {
            // Pouzi zivot ked nemas plny zivot
            if (controller.Health < controller.maxHealth)
            {
                // Pridaj zivot hracovi
                controller.ChangeHealth(1);

                // Prehraj klip
                controller.PlaySound(collectibleClip);

                // Znic objekt zivota
                Destroy(gameObject);
            }
        }
    }
}