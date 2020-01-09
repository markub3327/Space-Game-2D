using UnityEngine;

public class AmmoCollectible : MonoBehaviour
{
    // Zvukovy klip
    public AudioClip collectibleClip;

    // Pri kolizii
    void OnTriggerEnter2D(Collider2D other)
    {
        ShipController controller = other.gameObject.GetComponent<ShipController>();

        if (controller != null && controller.Ammo < ShipController.maxAmmo)
        {
            // Pridaj zivot hracovi
            controller.ChangeAmmo(+100);

            // Prehraj klip
            controller.PlaySound(collectibleClip);

            // Znic objekt zivota
            Destroy(gameObject);
        }
    }

    // Pri konci existencie objektu
    private void OnDestroy()
    {
        var generator = GetComponentInParent<AmmoGenerator>();
        if (generator != null)
            generator.freePoints.Add(transform.position);
    }
}
