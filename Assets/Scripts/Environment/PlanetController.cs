using System.Collections.Generic;
using UnityEngine;

public class PlanetController : MonoBehaviour
{
    // UIDialogBox
    public UIDialogBox dialogBox;

    public GameObject OwnerPlanet { get; set; } = null;

    // Zoznam mesiacov patriacich planete
    public List<MoonController> Moons;
    
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
