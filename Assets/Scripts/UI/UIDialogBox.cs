using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UIDialogBox : MonoBehaviour
{
    // referencia na textBox
    public TextMeshProUGUI textBox;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Write(string str)
    {
        textBox.SetText(textBox.text + str);
    }

    public void WriteLine(string str)
    {
        textBox.SetText(textBox.text + str + System.Environment.NewLine);
    }

    public void Clear()
    {
        textBox.SetText(string.Empty);
    }
}
