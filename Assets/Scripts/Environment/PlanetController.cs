using UnityEngine;
using System.Collections;
using TMPro;

public class PlanetController : MonoBehaviour
{
    public GameObject dialogBox;

    // Referencia na textBox
    private TextMeshProUGUI textMesh;

    // Vlastnik planety (lod, ktora na planete pristala)
    public GameObject OwnerPlanet { get; private set; }
    private float landTimer;

    // Use this for initialization
    void Start()
    {
        textMesh = dialogBox.GetComponentInChildren<TextMeshProUGUI>();

        dialogBox.transform.position = this.transform.position;
    }

    // Update is called once per frame
    void Update() 
    {

    }

    // Na zaiatku kolizie
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            dialogBox.SetActive(true);
            landTimer = 1f;

            textMesh.SetText(
                $"Planet: {this.name}\n\n" +
                $"Owner: {(OwnerPlanet != null ? OwnerPlanet.name : null)}");
        }
    }

    // Pocas kolizie
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            if (landTimer < 0f)
            {
                OwnerPlanet = collision.gameObject;

                textMesh.SetText(
                    $"Planet: {this.name}\n\n" +
                    $"Owner: {(OwnerPlanet != null ? OwnerPlanet.name : null)}");

                landTimer = 0f;
            }
            else if (landTimer > 0f)
                landTimer -= Time.deltaTime;
        }
    }

    // Na konci kolizie
    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            dialogBox.SetActive(false);
        }
    }
}
