using UnityEngine;

public class MoonController : MonoBehaviour
{
    // Rychlost obiehania mesiaca okolo planety
    public float rotationSpeed = 10;

    // Suradnice planety
    public Transform planetTransform;

    // UIDialogBox
    public UIDialogBox dialogBox;


    // Start is called before the first frame update
    void Start()
    {
        // Vypis do dialogoveho okna
        dialogBox.WriteLine($"Moon: {this.name}");
        dialogBox.WriteLine($"Planet: {planetTransform.name}");
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        // Spin the object around the world origin at 20 degrees/second.
		transform.RotateAround(planetTransform.position, Vector3.forward, rotationSpeed * Time.fixedDeltaTime);
    }

    // Kolizia
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            // Aktivuj dialogove okno
            dialogBox.gameObject.SetActive(true);
        }
    }

    // Na konci kolizie
    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            // Deaktivuj dialogove okno
            dialogBox.gameObject.SetActive(false);
        }
    }
}
