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
            if (this.OwnerPlanet == null)
            {
                this.OwnerPlanet = ship;
                ship.myPlanets.Add(this);
                //ship.hasNewPlanet = true;

                var strPlanets = string.Empty;
                foreach (var p in ship.myPlanets)
                {
                    strPlanets += p.name + ", ";
                }
                Debug.Log($"MyPlanets[{ship.name}]: {strPlanets}");
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
