using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class HUD : MonoBehaviour
{
    private TextMeshProUGUI foodText;

    private GameObject spawnMenu;
    private GameObject commandMenu;

    [SerializeField]
    private GameObject EnabledTutorialContainer;

    [SerializeField]
    private GameObject DisabledTutorialContainer;

    [SerializeField]
    private GameObject respawningUI;
    [SerializeField]
    private GameObject shiftandClicklabel;
    [SerializeField]
    private GameObject clickTofireLabel;
    [SerializeField]
    private GameObject spaceTofeedLabel;

    [SerializeField]
    private GameObject aliveUIContainer;
    

    public static bool shooterTutorialDone = false;
    public static bool firstUnitSpawned = false;

    public static bool feedLabelShown = false;


    private void Start()
    {
        foodText = aliveUIContainer.transform.Find("FoodLabel").GetComponent<TextMeshProUGUI>();
        spawnMenu = aliveUIContainer.transform.Find("SpawnMenu").gameObject;
        commandMenu = aliveUIContainer.transform.Find("CommandMenu").gameObject;
        SubscribeToEvents();
    }

    private void SubscribeToEvents()
    {
        GameEvents.current.FoodChanged += OnFoodChanged;
        GameEvents.current.SpawnMenuOpen += SpawnMenuOpen;
        GameEvents.current.StartRespawn += StartRespawn;
        GameEvents.current.EndRespawn += EndRespawn;
        GameEvents.current.UnitTakenControl += UnitTakenControl;
        GameEvents.current.FiredasShooter += FiredAsShooter;
        GameEvents.current.InRangeToFeed += InRangeToFeed;
        GameEvents.current.FirstUnitSpawned += FirstUnitSpawned;
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
        EnableTutorial(respawningUI.transform);
        aliveUIContainer.SetActive(false);
    }

    private void EndRespawn()
    {
        aliveUIContainer.SetActive(true);
    }
    // For removing shift "tutorial" text
    private void UnitTakenControl(string UnitName)
    {
        // Always need to enable a tutorial, in order to avoid errors.
        // Tutorials that are already done won't show anyways.
        switch (UnitName)
        {
            case "Collector":
                EnableTutorial(spaceTofeedLabel.transform);
                break;
            case "Shooter":
                EnableTutorial(clickTofireLabel.transform);
                if (!shooterTutorialDone)
                {
                    clickTofireLabel.GetComponent<Animator>().SetTrigger("FadeIn");
                }
                break;
            case "Hivemind":
                EnableTutorial(shiftandClicklabel.transform);
                break;
        }
    }

    public void FiredAsShooter()
    {
        clickTofireLabel.GetComponent<Animator>().SetTrigger("FadeOut");
        shooterTutorialDone = true;
    }

    private void InRangeToFeed(bool inRange)
    {
        Animator animator = spaceTofeedLabel.GetComponent<Animator>();

        if (inRange)
        {
            print("FadingIn");
            animator.SetTrigger("FadeIn");
            feedLabelShown = true;
        }
        else
        {
            print("FadingOut");
            animator.SetTrigger("FadeOut");
            feedLabelShown = false;
        }
    }

    private void EnableTutorial(Transform newLabel)
    {
        if (EnabledTutorialContainer.transform.childCount == 1)
        {
            Transform previousLabel = EnabledTutorialContainer.transform.GetChild(0);
            previousLabel.SetParent(DisabledTutorialContainer.transform);
        }
        newLabel.SetParent(EnabledTutorialContainer.transform);
    }

    private void FirstUnitSpawned()
    {
        firstUnitSpawned = true;
        shiftandClicklabel.GetComponent<Animator>().SetTrigger("FadeIn");
    }
}
