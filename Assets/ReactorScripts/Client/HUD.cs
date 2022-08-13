using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class HUD : MonoBehaviour
{
    private TextMeshProUGUI foodText;

    private GameObject spawnMenu;
    private GameObject commandMenu;
    private GameObject respawningUI;
    private void Start()
    {
        foodText = transform.Find("FoodLabel").GetComponent<TextMeshProUGUI>();
        spawnMenu = transform.Find("SpawnMenu").gameObject;
        commandMenu = transform.Find("CommandMenu").gameObject;
        respawningUI = transform.Find("Respawning").gameObject;
        GameEvents.current.FoodChanged += OnFoodChanged;
        GameEvents.current.SpawnMenuOpen += SpawnMenuOpen;
        GameEvents.current.StartRespawn += () => respawningUI.SetActive(true);
        GameEvents.current.EndRespawn += () => respawningUI.SetActive(false);
    }

    private void OnFoodChanged(int value)
    {
        foodText.text = $"Food = {value}";
    }

    private void SpawnMenuOpen(bool isOpen)
    {
        if (isOpen)
        {
            spawnMenu.SetActive(true);
            commandMenu.SetActive(false);
        }
        else
        {
            spawnMenu.SetActive(false);
            commandMenu.SetActive(true);
        }
    }
}
