using UnityEngine;

public class StarController : MonoBehaviour
{
    public AudioClip damageClip;

    // Ak narazi objekt do nebezpecnej zony
    private void OnTriggerStay2D(Collider2D collision)
    {
        switch (collision.gameObject.layer)
        {
            case 10:    // Ship
                {
                    var controller = collision.gameObject.GetComponent<ShipController>();
                    // Prahra clip zasahu
                    controller.PlaySound(damageClip);
                    // Uberie sa hracovi zivot
                    controller.ChangeHealth(-1);
                    break;
                }
            case 12:    // Asteroid
                {
                    var controller = collision.gameObject.GetComponent<AsteroidController>();
                    if (controller != null)
                        // Znici asteroid
                        controller.Destroy();
                    break;
                }
        }           
    }
}
