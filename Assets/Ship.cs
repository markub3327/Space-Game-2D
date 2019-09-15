using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ship : MonoBehaviour
{

    public GameObject shipPrefab;

    private GameObject shipObject;

    // Start is called before the first frame update
    void Start()
    {
        // Vytvor objekt lode na poziciu hraca (lod patri pod objekt hraca)
        shipObject = Instantiate(shipPrefab, transform.position, shipPrefab.transform.rotation, this.transform);
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log($"Number if contacts: {collision.contactCount}");
        Debug.Log($"Collision object's name: {collision.gameObject.name}");
        Debug.Log($"Collision object's tag: {collision.gameObject.tag}");
    }
}