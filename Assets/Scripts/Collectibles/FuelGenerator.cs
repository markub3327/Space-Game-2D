using System.Collections.Generic;
using UnityEngine;

public class FuelGenerator : MonoBehaviour
{
    // Maximalny pocet municie v hre
    public static int MaxFuelObjects { get; set; } = 3;

    // Zoznam pouzitych hmlovin
    public List<NebulaController> freeFuels = new List<NebulaController>(MaxFuelObjects);

    // Casovac pridavania hmlovin
    private const float maxTime = 30f;
    private float Timer = maxTime;

    // Update is called once per frame
    private void Update()
    {
        if (freeFuels.Count > 0)
        {
            Timer -= Time.deltaTime;
            if (Timer < 0f)
            {
                var idx = Random.Range(0, freeFuels.Count);
                freeFuels[idx].gameObject.SetActive(true);

                // Vymaz pouzity bod z volnych (obsadeny)
                freeFuels.RemoveAt(idx);

                // Resetuj casovac
                Timer = maxTime;
            }
        }
    }
}
