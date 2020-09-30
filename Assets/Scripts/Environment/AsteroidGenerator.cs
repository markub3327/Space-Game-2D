using System.Collections.Generic;
using UnityEngine;

public class AsteroidGenerator : MonoBehaviour
{
    // Maximalny pocet municie v hre
    public static int MaxAsteroidObjects { get; set; } = 6;

    // Zoznam volnych pozicii pre municiu
    public List<GameObject> asteroids;

    // Prefab Ammo object
    public GameObject AsteroidPrefab;

    // Update is called once per frame
    private void Update()
    {
        if (asteroids.Count < MaxAsteroidObjects)
        {
            var obj = Instantiate(AsteroidPrefab, new Vector2(20f, 1.5f), Quaternion.identity, this.transform);
            obj.name = "Asteroid";

            // pridaj novy asteroid
            asteroids.Add(obj);
        }
    }
}
