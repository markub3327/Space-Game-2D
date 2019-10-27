using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class TurretController : MonoBehaviour
{
    public GameObject projectilePrefab;

    public AudioClip throwClip;

    public float shot_speed = 75.0f;

    //public float turret_rotation_speed = 3.0f;

    private ShipController controller;

    public ParticleSystem shootEffect;

    // Start is called before the first frame update
    public virtual void Start()
    {
        controller = GetComponentInParent<ShipController>();
    }
    
    protected void SetPosition(Vector3 position)
    {
        Vector3 direction = position - transform.position;

        // Linear
        //transform.rotation = Quaternion.Euler(0, 0, Mathf.LerpAngle(transform.rotation.eulerAngles.z, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90.0f, turret_rotation_speed * Time.deltaTime));

        // Hned :D
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90.0f);
    }

    // Funkcie strelby zbrane
    protected void Fire()
    {
        if (controller.Ammo > 0)
        {
            GameObject projectileObject = Instantiate(projectilePrefab, transform.position, transform.rotation);

            Projectile projectile = projectileObject.GetComponent<Projectile>();
            projectile.firingShip = controller;
            projectile.Launch(projectileObject.transform.up * shot_speed);

            // Prehraj animaciu vystrelu
            shootEffect.Play();

            // Prehra zvuk vystrelu
            controller.PlaySound(throwClip);

            // Uber z municie hraca jeden naboj
            controller.ChangeAmmo(-1);
        }
    }

    // Funkcia automatickeho zameriavania na najblizsi ciel (lod)
    protected void AutoAim()
    {
        Sensors.Radar.Scan(transform.position, controller.LookDirection);

        // Najde najblizsi videny objekt
        var obj = Sensors.Radar.GetNearestObject(controller.gameObject);

        // Nastav mierenie zbarni na najblizsi objekt ak ide o lod
        if (obj != null)
        {
            SetPosition(obj.Value.point);

            Debug.Log($"{this.name}'s nearest object: name = {obj.Value.transform.name}, distance = {obj.Value.distance}");
        }
    }
}
