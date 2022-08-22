using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;


public class UnitUILabel : MonoBehaviour
{
    private TextMeshProUGUI textMesh;

    private void Start()
    {
        int num = (int) Stats.current.GetType().GetField(gameObject.name +"Cost").GetValue(Stats.current);
        string button = (string) Stats.current.GetType().GetField(gameObject.name + "Button").GetValue(Stats.current); // Use a better way of getting the binding
        textMesh = GetComponent<TextMeshProUGUI>();
        textMesh.text = gameObject.name + $" ({button}) " + $" cost {num}";
    }
}
