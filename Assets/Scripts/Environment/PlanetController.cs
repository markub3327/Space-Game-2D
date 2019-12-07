using System.Collections.Generic;
using UnityEngine;

public class PlanetController : MonoBehaviour
{
    // UIDialogBox
    public UIDialogBox dialogBox;

    public ShipController OwnerPlanet { get; set; }

    // Zoznam mesiacov patriacich planete
    public List<MoonController> Moons;
    
    // Kolizia
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            var ship = collision.gameObject.GetComponent<ShipController>();
            if (this.OwnerPlanet != ship)
            {
                if (this.OwnerPlanet != null)   // ak planeta mala vlastnika uvolni ho zo zoznamu na lodi
                {
                    this.OwnerPlanet.myPlanets.Remove(this);
                }
                this.OwnerPlanet = ship;
                ship.myPlanets.Add(this);
                ship.myPlanets.Sort((a, b) => (a.name.CompareTo(b.name)));  // usporiadaj zoznam planet aby bolo poradie planet na kazdej lodi rovnaky
            }

            // Vycisti dialogove okno
            dialogBox.Clear();

            // Vypis do dialogoveho okna
            dialogBox.WriteLine($"Planet: {this.name}");
            dialogBox.WriteLine($"Owner: {(this.OwnerPlanet != null ? this.OwnerPlanet.name : string.Empty)}");
            dialogBox.gameObject.SetActive(true);
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
