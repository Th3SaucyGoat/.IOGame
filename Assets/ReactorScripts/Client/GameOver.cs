using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameOver : MonoBehaviour
{
    private FunctionTimer redirectTimer;
    private TextMeshProUGUI redirectCountdown;
    private TextMeshProUGUI title;
    private GameObject toggleLabel;
    private GameObject spectateButton;

    private void Awake()
    {
        GameEvents.current.StartRedirect += StartRedirect;
        GameEvents.current.StartSpectating += StartSpectating;
        redirectCountdown = transform.Find("Redirect").gameObject.GetComponent<TextMeshProUGUI>();
        toggleLabel = transform.Find("ToggleLabel").gameObject;
        spectateButton = transform.Find("Spectate").gameObject;
        title = transform.Find("Title").GetComponent<TextMeshProUGUI>();
        reset();

    }

    private void reset()
    {
        title.text = "Game Over";
        toggleLabel.SetActive(false);
        spectateButton.SetActive(true);
    }

    private void Update()
    {
        if (redirectCountdown.gameObject.activeSelf == true && redirectTimer != null)
        {
            redirectCountdown.text = $"You will be redirected in {(int) redirectTimer.TimeRemaining()} seconds";
        }
    }

    private void StartRedirect()
    {
        redirectTimer = FunctionTimer.Create(Redirect, 10f);
        redirectTimer.Start();
        redirectCountdown.gameObject.SetActive(true);
    }

    private void Redirect()
    {
        redirectCountdown.gameObject.SetActive(false);
        GameEvents.current.Disconnected();
    }

    private void StartSpectating()
    {
        title.text = "Spectating";
        toggleLabel.SetActive(true);
        spectateButton.SetActive(false);
    }

    public void InitiateSpectating()
    {
        GameEvents.current.StartSpectating?.Invoke(); // Other components listen to this signal.
    }    
}
