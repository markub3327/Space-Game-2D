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
    private const float maxTime = 10f;
    private float Timer = maxTime;

    // Update is called once per frame
    private void Update()
    {
        if (freePoints.Count > 0)
        {
            Timer -= Time.deltaTime;
            if (Timer < 0f)
            {
                var idx = Random.Range(0, freePoints.Count);
                var obj = Instantiate(AmmoPrefab, freePoints[idx], Quaternion.identity, this.transform);
                obj.name = "Ammo";

                // Vymaz pouzity bod z volnych (obsadeny)
                freePoints.Remove(freePoints[idx]);

                // Resetuj casovac
                Timer = maxTime;
            }
        }
    }
}
