using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UI : MonoBehaviour
{
    GameObject mainMenu;
    GameObject gameUi;
    GameObject gameOver;
    void Start()
    {
        mainMenu = transform.Find("MainMenu").gameObject;
        gameUi = transform.Find("GameUI").gameObject;
        gameOver = transform.Find("GameOver").gameObject;
        reset();
        SubscribeToEvents();
    }

    private void reset()
    {
        mainMenu.SetActive(true);
        gameUi.SetActive(false);
        gameOver.SetActive(false);
    }

    private void SubscribeToEvents()
    {
        GameEvents.current.StartMatch += OnStartMatch;
        GameEvents.current.GameOver += OnGameOver;
        GameEvents.current.Disconnected += OnDisconnected;
    }

    private void OnStartMatch()
    {
        mainMenu.SetActive(false);
        gameUi.SetActive(true);
    }

    private void OnGameOver(bool isVictory)
    {
        gameUi.SetActive(false);
        gameOver.SetActive(true);
        if (isVictory)
        {
            gameOver.transform.Find("Title").gameObject.GetComponent<TextMeshProUGUI>().text = "Victory";
        }
    }

    private void OnDisconnected()
    {
        reset();
    }

    public void Disconnect()
    {
        GameEvents.current.Disconnected();
    }
}
