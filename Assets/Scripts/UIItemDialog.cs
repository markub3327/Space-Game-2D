using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UIItemDialog : MonoBehaviour
{
    public GameObject dialogBox;

    // Start is called before the first frame update
    void Start()
    {
        dialogBox.SetActive(false);

        var textMesh = dialogBox.GetComponentInChildren<TextMeshProUGUI>();
        textMesh.SetText($"Planet: {this.name}");
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Player")
            dialogBox.SetActive(true);
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Player")
            dialogBox.SetActive(false);
    }
}
