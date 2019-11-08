using UnityEngine;

public class PlayerControlledTurret : TurretController
{
    private const float gunFPS = (1f / 15f);        // 10 shoots per second 
    private float gunFireTimer = gunFPS;            // casovac uchovavajuci cas do skoncenia nesmrtelnosti

    // Update is called once per frame
    public void Update()
    {
        // Tlacidlo mierenia na najblizsi ciel
        if (Input.GetButton("Fire2"))
        {
            AutoAim();
        }
        else
            SetPosition(Camera.main.ScreenToWorldPoint(Input.mousePosition));

        // Tlacidlo strelby
        if (Input.GetButton("Fire1"))
        {
            gunFireTimer -= Time.deltaTime;
            if (gunFireTimer < 0.0f)
            {
                Fire();
                gunFireTimer = gunFPS;
            }
        }
    }
}
