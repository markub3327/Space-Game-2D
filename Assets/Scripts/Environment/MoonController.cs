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
    void Update()
    {
        // Spin the object around the world origin at 20 degrees/second.
		transform.RotateAround(planetTransform.position, Vector3.forward, rotationSpeed * Time.deltaTime);

        // Vyrovnaj text aby bol citatelny
        dialogBox.transform.rotation = Quaternion.Euler(0f, 0f, -Mathf.Atan2(transform.position.y, transform.position.x) * Mathf.Rad2Deg - 40f);
    }

    public void ShowDialog()
    {
        dialogBox.gameObject.SetActive(true);
    }

    public void CloseDialog()
    {
        // Deaktivuj dialogove okno
        dialogBox.gameObject.SetActive(false);
    }
}
