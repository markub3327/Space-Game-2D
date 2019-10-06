using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AsteroidController : MonoBehaviour
{
    public float rotationSpeed = 10;

    public Transform starTransform;

    public GameObject explosionAnim;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        // Spin the object around the world origin at 20 degrees/second.
        transform.RotateAround(new Vector3(starTransform.position.x, (starTransform.position.y/2.0f), starTransform.position.z), Vector3.forward, rotationSpeed * Time.deltaTime);
    }

    public void DestroyAsteroid()
    {
        if (this.enabled)
        {
            // Deaktivuj pohyb asteroidu
            this.enabled = false;

            // Prehraj animaciu vybuchu
            Instantiate(explosionAnim, transform.position, Quaternion.identity);

            // Po animacie znic objekt
            Destroy(this.gameObject, 0.25f);
        }
    }
}
