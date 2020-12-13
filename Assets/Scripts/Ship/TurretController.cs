using UnityEngine;

public class TurretController : MonoBehaviour
{
    public ParticleSystem shootEffect;

    public GameObject projectilePrefab;

    private ShipController controller;

    private float speed = 100.0f;

    public void Start()
    {
        controller = GetComponentInParent<ShipController>();
    }

    public void Fire()
    {
        var projectileObject = Instantiate(projectilePrefab, transform.position, transform.rotation);
        var projectile = projectileObject.GetComponent<Projectile>();
        projectile.name = "Projectile";
        projectile.firingShip = controller;
        projectile.Launch(controller.LookDirection * speed);

        // Prehraj animaciu vystrelu
        shootEffect.Play();
    }
}
