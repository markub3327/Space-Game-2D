using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MoonController : MonoBehaviour
{
    public float speed = 2;

	public Tilemap tilemap;

    public Vector3Int point;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
		// Spin the object around the world origin at 20 degrees/second.
		transform.RotateAround(tilemap.GetCellCenterWorld(point), Vector3.forward, speed * Time.deltaTime);
	}
}
