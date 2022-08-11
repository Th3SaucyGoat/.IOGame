using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UI : MonoBehaviour
{
    private TextMeshProUGUI foodText;
    private void Start()
    {
        GameEvents.current.StartMatch += OnStartMatch;
        foodText = transform.Find("FoodLabel").GetComponent<TextMeshProUGUI>();
        gameObject.SetActive(false);
        
    }

    private void OnStartMatch()
    {
        gameObject.SetActive(true);
        GameEvents.current.FoodChanged += OnFoodChanged;
    }

    private void OnFoodChanged(int value)
    {
        foodText.text = $"Food = {value}";
    }
}
