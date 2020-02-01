using UnityEngine;

public class AsteroidController : MonoBehaviour
{
    public AudioClip damageClip;

    public float rotationSpeed = 10;

    public Transform starTransform;

    // Update is called once per frame
    void Update()
    {
        // Spin the object around the world origin at 20 degrees/second.
        transform.RotateAround(starTransform.position, Vector3.forward, rotationSpeed * Time.deltaTime);
    }

    // Ak narazi objekt do nebezpecnej zony
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            var controller = collision.gameObject.GetComponent<ShipController>();
            // Prahra clip zasahu
            controller.PlaySound(damageClip);
            // Uberie sa hracovi zivot
            controller.ChangeHealth(-1);       
        }           
    }
}
