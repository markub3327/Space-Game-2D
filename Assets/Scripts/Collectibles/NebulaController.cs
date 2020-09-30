using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NebulaController : MonoBehaviour
{
    public void Delete()
    {
        var generator = GetComponentInParent<FuelGenerator>();
        if (generator != null)
            generator.freeFuels.Add(this);
        this.gameObject.SetActive(false);
    }
}
