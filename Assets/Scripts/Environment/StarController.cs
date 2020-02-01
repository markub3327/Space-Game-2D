using UnityEngine;

public class StarController : MonoBehaviour
{
    public AudioClip damageClip;

    // Ak narazi objekt do nebezpecnej zony
    private void OnTriggerStay2D(Collider2D collider)
    {
        if (collider.gameObject.tag == "Player")
        {
            var controller = collider.gameObject.GetComponent<ShipController>();
            // Prahra clip zasahu
            controller.PlaySound(damageClip);
            // Uberie sa hracovi zivot
            controller.ChangeHealth(-1);              
        }           
    }
}
