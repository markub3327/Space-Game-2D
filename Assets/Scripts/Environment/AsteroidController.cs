using UnityEngine;

public class AsteroidController : MonoBehaviour
{
    public float rotationSpeed = 10;

    public Transform starTransform;

    public GameObject explosionAnim;

    // Update is called once per frame
    void FixedUpdate()
    {
        // Spin the object around the world origin at 20 degrees/second.
        transform.RotateAround(starTransform.position, Vector3.forward, rotationSpeed * Time.fixedDeltaTime);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Star")
        {
            Destroy(this.gameObject);
        
            // Prehraj animaciu vybuchu a znic animovany objekt
            var gObject = Instantiate(explosionAnim, transform.position, Quaternion.identity);
            gObject.name = "Asteroid";
            Destroy(gObject, 0.75f);
        }
    }
}
