using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageZone : MonoBehaviour
{
	public AudioClip damageClip;

    // Ak narazi objekt do nebezpecnej zony
    private void OnCollisionStay2D(Collision2D other)
    {
        ShipController controller = other.gameObject.GetComponent<ShipController>();

        if (controller != null)
        {
            // Uberie sa hracovi zivot
            controller.ChangeHealth(-1);

			// Prehraj zvuk poskodenia
            if (!controller.IsPlayingClip)
    			controller.PlaySound(damageClip);
		}
	}
}
