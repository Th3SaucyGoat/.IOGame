using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class HUD : MonoBehaviour
{
    private TextMeshProUGUI foodText;
    private void Start()
    {
        foodText = transform.Find("FoodLabel").GetComponent<TextMeshProUGUI>();
        GameEvents.current.FoodChanged += OnFoodChanged;
    }

    private void OnFoodChanged(int value)
    {
        foodText.text = $"Food = {value}";
    }
}
