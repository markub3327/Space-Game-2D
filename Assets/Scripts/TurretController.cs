using UnityEngine;
using System.Collections;

public class TurretController : MonoBehaviour
{
    public GameObject projectilePrefab;

    public AudioClip throwClip;

    public float shot_speed = 350.0f;

    public float turret_rotation_speed = 3.0f;

    private ShipController controller;

    public ParticleSystem shootEffect;

    // Start is called before the first frame update
    void Start()
    {
        controller = GetComponentInParent<ShipController>();
    }

    protected void SetPosition(Vector3 position)
    {
        //This makes the turret aim at the mouse position (Controlled by CustomPointer, but you can replace CustomPointer.pointerPosition with Input.MousePosition and it should work)
        Vector3 direction = position - transform.position;
        transform.rotation = Quaternion.Euler(0, 0, Mathf.LerpAngle(transform.rotation.eulerAngles.z, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90.0f, turret_rotation_speed * Time.deltaTime));
    }

    protected void Fire()
    {
        if (controller.Ammo > 0)
        {
            GameObject projectileObject = Instantiate(projectilePrefab, transform.position, transform.rotation);

            Projectile projectile = projectileObject.GetComponent<Projectile>();
            projectile.firingShip = transform.parent.gameObject;
            projectile.Launch(projectileObject.transform.up * shot_speed);

            // Prehraj animaciu vystrelu
            shootEffect.Play();

            // Prehra zvuk vystrelu
            controller.PlaySound(throwClip);

            // Uber z municie hraca jeden naboj
            controller.ChangeAmmo(-1);
        }
    }
}
