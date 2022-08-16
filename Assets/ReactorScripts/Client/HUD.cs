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
    private GameObject shiftandClick;
    private void Start()
    {
        foodText = transform.Find("FoodLabel").GetComponent<TextMeshProUGUI>();
        spawnMenu = transform.Find("SpawnMenu").gameObject;
        commandMenu = transform.Find("CommandMenu").gameObject;
        respawningUI = transform.Find("Respawning").gameObject;
        shiftandClick = transform.Find("Shift&Click").gameObject;
        GameEvents.current.FoodChanged += OnFoodChanged;
        GameEvents.current.SpawnMenuOpen += SpawnMenuOpen;
        GameEvents.current.StartRespawn += StartRespawn;
        GameEvents.current.EndRespawn += EndRespawn;
        GameEvents.current.UnitTakenControl += UnitTakenControl;
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

    private void StartRespawn()
    {
        respawningUI.SetActive(true);

    }

    private void EndRespawn()
    {
        respawningUI.SetActive(false);
    }
    // For removing shift "tutorial" text
    private void UnitTakenControl()
    {
        if (shiftandClick.activeSelf == true)
        {
            shiftandClick.SetActive(false);
        }
    }
}
