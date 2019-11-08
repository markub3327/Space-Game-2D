using UnityEngine;

public class AsteroidController : MonoBehaviour
{
    public float rotationSpeed = 10;

    public Transform starTransform;

    public GameObject explosionAnim;

    // Update is called once per frame
    void Update()
    {
        // Spin the object around the world origin at 20 degrees/second.
        transform.RotateAround(new Vector3(starTransform.position.x, (starTransform.position.y/2.0f), starTransform.position.z), Vector3.forward, rotationSpeed * Time.deltaTime);
    }

    // Pri zniceni asteroidu
    public void Destroy()
    {
        // Znic asteroid
        Destroy(this.gameObject);

        // Prehraj animaciu vybuchu a znic animovany objekt
        var gObject = Instantiate(explosionAnim, transform.position, Quaternion.identity);
        Destroy(gObject, 0.75f);
    }
}
