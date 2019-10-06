using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerControlledTurret : TurretController
{
    // Update is called once per frame
    void Update()
    {
        SetPosition(Camera.main.ScreenToWorldPoint(Input.mousePosition));

        if (Input.GetButtonDown("Fire1"))
        {
            Fire();
        }
    }
}
