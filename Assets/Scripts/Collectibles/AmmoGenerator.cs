using System.Collections.Generic;
using UnityEngine;

public class AmmoGenerator : MonoBehaviour
{
    // Maximalny pocet municie v hre
    public static int MaxAmmoObjects { get; set; } = 5;

    // Zoznam volnych pozicii pre municiu
    public List<Vector2> freePoints = new List<Vector2>(MaxAmmoObjects);

    // Prefab Ammo object
    public GameObject AmmoPrefab;

    // Casovac pridavania municie
    private const float maxTime = 5f;
    private float Timer = maxTime;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    private void FixedUpdate()
    {
        // Pocet srdiecok v hre (musia chybat 2 zivoty)
        if (freePoints.Count >= 3)
        {
            Timer -= Time.deltaTime;
            if (Timer < 0f)
            {
                var idx = Random.Range(0, freePoints.Count);
                Instantiate(AmmoPrefab, freePoints[idx], Quaternion.identity, this.transform);

                // Vymaz pouzity bod z volnych (obsadeny)
                freePoints.Remove(freePoints[idx]);

                // Resetuj casovac
                Timer = maxTime;
            }
        }
    }
}
