using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class RespawnPoints : MonoBehaviour
{
    private List<ShipController> ships;

    // Start is called before the first frame update
    void Start()
    {
        ships = new List<ShipController>(GetComponentsInChildren<ShipController>());
    }

    // Update is called once per frame
    void Update()
    {
        if (ships.Where(ship => ship.IsRespawned == false).Count() == 0)
        {
            var tmp = ships[ships.Count-1].respawnPoint;
            for (int i = ships.Count-1; i >= 1; i--)
            {            
                ships[i].respawnPoint = ships[i-1].respawnPoint;                
                ships[i].IsRespawned = false;                
            }            
            ships[0].respawnPoint = tmp;                
            ships[0].IsRespawned = false;
        }
    }
}
