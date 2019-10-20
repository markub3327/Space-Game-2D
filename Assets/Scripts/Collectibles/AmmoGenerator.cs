using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AmmoGenerator : MonoBehaviour
{
    // Maximalny pocet municie v hre
    public static int MaxHealthObjects { get; set; } = 5;

    // Zoznam objektov srdiecok
    public List<GameObject> ammo = new List<GameObject>(MaxHealthObjects);

    // Prefab Ammo object
    public GameObject AmmoPrefab;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    private void FixedUpdate()
    {
        // Pocet municie v hre
        if (ammo.Count < MaxHealthObjects)
        {
            var obj = Instantiate(AmmoPrefab, new Vector3(-12f, -4f, 0f), Quaternion.identity, this.transform);
            ammo.Add(obj);            
        }
    }
}
