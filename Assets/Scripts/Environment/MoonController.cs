using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MoonController : MonoBehaviour
{
    public float rotationSpeed = 20;

    public Transform planetTransform;

    public GameObject dialogBox;

    // Referencia na textBox
    private TextMeshProUGUI textMesh;

    // Start is called before the first frame update
    void Start()
    {
        textMesh = dialogBox.GetComponentInChildren<TextMeshProUGUI>();

        dialogBox.SetActive(false);
            
        textMesh.SetText(
            $"Moon {this.name}\n\n" +
            $"Planet: {planetTransform.name}");
    }

    // Update is called once per frame
    void Update()
    {
        // Spin the object around the world origin at 20 degrees/second.
		transform.RotateAround(planetTransform.position, Vector3.forward, rotationSpeed * Time.deltaTime);
	}

    // Na zaiatku kolizie
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            dialogBox.transform.position = this.transform.position;
            dialogBox.SetActive(true);
        }
    }

    // Pocas kolizie (sleduj poziciu mesiaca)
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Player")
            dialogBox.transform.position = this.transform.position;
    }

    // Na konci kolizie
    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Player")
            dialogBox.SetActive(false);
    }
}
