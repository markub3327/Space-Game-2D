using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerControlledTurret : MonoBehaviour
{
	public GameObject projectilePrefab;

    public AudioClip throwClip;

    public float shot_speed = 300.0f;

    public float turret_rotation_speed = 3.0f;

    ShipController controller;

    // Start is called before the first frame update
    void Start()
    {
        controller = GetComponentInParent<ShipController>();
    }

    // Update is called once per frame
    void Update()
    {
		//This makes the turret aim at the mouse position (Controlled by CustomPointer, but you can replace CustomPointer.pointerPosition with Input.MousePosition and it should work)
		Vector3 turretPosition = Camera.main.WorldToScreenPoint(transform.position);
		Vector3 direction = Input.mousePosition - turretPosition;
		transform.rotation = Quaternion.Euler(0, 0, Mathf.LerpAngle(transform.rotation.eulerAngles.z, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg, turret_rotation_speed * Time.deltaTime));


        if (Input.GetButtonDown("Fire1") && controller.Ammo > 0)
        {
            //GameObject bullet = (GameObject)Instantiate(weapon_prefab, barrel_hardpoints[barrel_index].transform.position, transform.rotation);
            GameObject projectileObject = Instantiate(projectilePrefab, transform.position, Quaternion.Euler(transform.rotation.eulerAngles - (new Vector3(0, 0, 90.0f))));

            Projectile projectile = projectileObject.GetComponent<Projectile>();
            projectile.firingShip = transform.parent.gameObject;
            projectile.Launch(projectileObject.transform.up * shot_speed);

            // Prehra zvuk vystrelu
            controller.PlaySound(throwClip);

            //barrel_index++; //This will cycle sequentially through the barrels in the barrel_hardpoints array

            //if (barrel_index >= barrel_hardpoints.Length)
            //	barrel_index = 0;

            // Uber z municie hraca jeden naboj
            controller.ChangeAmmo(-1);
        }
    }
}
