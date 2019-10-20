using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIBarControl : MonoBehaviour
{
    // Obrazok linky, ktorou posuva
    private Image bar;

    void Start()
    {
        bar = GetComponent<Image>();
    }

    public void SetValue(float value)
    {
        if (bar != null)
            // Zmen sirku baneru podla hodnoty zadanej parametrom
            bar.fillAmount = value;
    }
}
