using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class PlanetController : MonoBehaviour
{
    // UIDialogBox
    public UIDialogBox dialogBox;

    // Vlastnik planety (lod, ktora na planete pristala)
    public GameObject OwnerPlanet { get; private set; }
    private float landTimer;

    // Zoznam mesiacov patriacich planete
    public List<MoonController> Moons;

    // Na zaiatku kolizie
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            // Aktivuj dialogove okno
            dialogBox.gameObject.SetActive(true);

            // Resetuj casovac pristatia lode na planete
            landTimer = 1f;

            // Vypis do dialogoveho okna
            dialogBox.WriteLine($"Planet: {this.name}");
            dialogBox.WriteLine($"Owner: {(OwnerPlanet != null ? OwnerPlanet.name : string.Empty)}");

            // Zapni dialogBox na vsetkych mesiacoch planety
            foreach (var moon in Moons)
            {
                moon.ShowDialog();
            }
        }
    }

    // Pocas kolizie
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            // ak casovac pristatia vyprsal
            if (landTimer < 0f)
            {
                // Zapis vlastnika planety
                OwnerPlanet = collision.gameObject;

                // Vycisti dialogove okno
                dialogBox.Clear();

                // Vypis do dialogoveho okna
                dialogBox.WriteLine($"Planet: {this.name}");
                dialogBox.WriteLine($"Owner: {(OwnerPlanet != null ? OwnerPlanet.name : string.Empty)}");

                // Reset casovaca
                landTimer = 0f;
            }
            // odpocitavaj cas
            else if (landTimer > 0f)
                landTimer -= Time.deltaTime;
        }
    }

    // Na konci kolizie
    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            // Vycisti a deaktivuj dialogove okno
            dialogBox.Clear();
            dialogBox.gameObject.SetActive(false);

            // Zatvor dialogBox na vsetkych mesiacoch planety
            foreach (var moon in Moons)
            {
                moon.CloseDialog();
            }
        }
    }
}
