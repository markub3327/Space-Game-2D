using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthsGenerator : MonoBehaviour
{
    // Maximalny pocet srdiecok v hre
    public static int MaxHealthObjects { get; set; } = 3;

    // Zoznam volnych pozicii pre srdiecka
    public List<Vector2> freePoints = new List<Vector2>(MaxHealthObjects);

    // Prefab Health object
    public GameObject HealthPrefab;

    // Casovac pridavania srdiecok
    private const float maxTime = 3f;
    private float Timer = maxTime;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    private void FixedUpdate()
    {
        // Pocet srdiecok v hre (musia chybat 2 zivoty)
        if (freePoints.Count > 0)
        {
            Timer -= Time.deltaTime;
            if (Timer < 0f)
            {
                var genR = new System.Random();
                var idx = genR.Next(0, freePoints.Count);
                Instantiate(HealthPrefab, freePoints[idx], Quaternion.identity, this.transform);

                // Vymaz pouzity bod z volnych (obsadeny)
                freePoints.Remove(freePoints[idx]);

                // Resetuj casovac
                Timer = maxTime;
            }
        }
    }
}
