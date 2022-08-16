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
    private GameObject shiftandClicklabel;
    private GameObject clickTofireLabel;
    private GameObject spaceTofeedLabel;

    public static bool shooterTutorialDone = false;

    public static bool feedLabelShown = false;


    private void Start()
    {
        foodText = transform.Find("FoodLabel").GetComponent<TextMeshProUGUI>();
        spawnMenu = transform.Find("SpawnMenu").gameObject;
        commandMenu = transform.Find("CommandMenu").gameObject;
        respawningUI = transform.Find("Respawning").gameObject;
        shiftandClicklabel = transform.Find("Shift&Click").gameObject;
        clickTofireLabel = transform.Find("ClicktoFire").gameObject;
        spaceTofeedLabel = transform.Find("SpacetoFeed").gameObject;
        GameEvents.current.FoodChanged += OnFoodChanged;
        GameEvents.current.SpawnMenuOpen += SpawnMenuOpen;
        GameEvents.current.StartRespawn += StartRespawn;
        GameEvents.current.EndRespawn += EndRespawn;
        GameEvents.current.UnitTakenControl += UnitTakenControl;
        GameEvents.current.FiredasShooter += FiredAsShooter;
        GameEvents.current.InRangeToFeed += InRangeToFeed;
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
    private void UnitTakenControl(string UnitName)
    {
        if (shiftandClicklabel.activeSelf == true)
        {
            shiftandClicklabel.SetActive(false);
        }
        
        switch (UnitName)
        {
            case "Collector":
                break;
            case "Shooter":
                if (!shooterTutorialDone)
                {
                    // Start animation
                    clickTofireLabel.GetComponent<Animator>().SetTrigger("FadeIn");
                }
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


}
