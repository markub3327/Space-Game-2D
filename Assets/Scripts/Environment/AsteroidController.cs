using UnityEngine;

public class AsteroidController : MonoBehaviour
{
    public float rotationSpeed = 10;

    public Transform starTransform;

    public GameObject explosionAnim;

    public Sprite[] sprites;

    // Start is called before the first frame update
    void Start()
    {
        var sr = GetComponent<SpriteRenderer>();
        var idx = Random.Range(0, sprites.Length);
        sr.sprite = this.sprites[idx];

        this.starTransform = GameObject.Find("Sun").transform;
    }


    // Update is called once per frame
    void FixedUpdate()
    {
        // Spin the object around the world origin at 20 degrees/second.
        transform.RotateAround(starTransform.position, Vector3.forward, rotationSpeed * Time.fixedDeltaTime);
    }

    private void my_destroy()
    {
        Destroy(this.gameObject);

        // Prehraj animaciu vybuchu a znic animovany objekt
        var gObject = Instantiate(explosionAnim, transform.position, Quaternion.identity);
        gObject.name = "Asteroid";
        Destroy(gObject, 0.75f);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Star")
        {
            my_destroy();
        }
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
       if (collider.gameObject.tag == "Projectile")
        {
            my_destroy();
        }
    }

    // Pri konci existencie objektu
    private void OnDestroy()
    {
        var generator = GetComponentInParent<AsteroidGenerator>();
        if (generator != null)
            generator.asteroids.Remove(this.gameObject);
    }
}
