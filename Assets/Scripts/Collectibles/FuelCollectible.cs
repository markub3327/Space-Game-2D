using UnityEngine;

public class FuelCollectible : MonoBehaviour
{
    // Zvukovy klip
    public AudioClip collectibleClip;

    // Pri kolizii
    void OnTriggerStay2D(Collider2D other)
    {
        ShipController controller = other.gameObject.GetComponent<ShipController>();

        // Pouzi zivot ked nemas plny zivot
        if (controller != null && controller.Fuel < controller.maxFuel)
        {
            // Pridaj zivot hracovi
            controller.ChangeFuel(+1);

            // Prehraj klip
            controller.PlaySound(collectibleClip);
        }
    }

}
