using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthsGenerator : MonoBehaviour
{
    // Maximalny pocet srdiecok v hre
    public static int MaxHealthObjects { get; set; } = 3;

    // Zoznam objektov srdiecok
    public List<GameObject> healths = new List<GameObject>(MaxHealthObjects);

    // Prefab Health object
    public GameObject HealthPrefab;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    private void FixedUpdate()
    {
        // Pocet municie v hre
        if (healths.Count < MaxHealthObjects)
        {
            var obj = Instantiate(HealthPrefab, new Vector3(-12f, -4f, 0f), Quaternion.identity, this.transform);
            healths.Add(obj);
        }
    }
}
