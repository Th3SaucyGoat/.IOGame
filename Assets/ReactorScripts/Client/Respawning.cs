using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Respawning : MonoBehaviour
{
    private TextMeshProUGUI tmp;
    private FunctionTimer updateTimer;

    public static bool respawnTutorialComplete = false;

    private Animator labelAnimator;

    private void Start()
    {
        updateTimer = FunctionTimer.Create(OnUpdateTimerTimeout, 0.4f, false, "RespawnUITimer");
        updateTimer.gameObject.transform.SetParent(transform);
        tmp = transform.Find("Title").GetComponent<TextMeshProUGUI>();
        GameEvents.current.RespawnTutorialComplete += RespawnTutorialComplete;
    }
    void Update()
    {
        if (gameObject.activeInHierarchy == true)
        {
            if (updateTimer.IsStopped())
            {
                updateTimer.Start();
            }
        }
    }

    private void OnUpdateTimerTimeout()
    {
        print(tmp.text);
        if (tmp.text.EndsWith("..."))
        {
            tmp.text = tmp.text.Substring(0, tmp.text.Length - 3);
        }
        else
        {
            tmp.text = tmp.text + ".";
        }

        if (gameObject.activeInHierarchy == true)
        {
            updateTimer.Start();
        }
    }

    private void OnEnable()
    {
        labelAnimator = transform.Find("ToggleLabel").gameObject.GetComponent<Animator>();
        if (!respawnTutorialComplete)
        {
            labelAnimator.SetTrigger("FadeIn");
        }
    }

    private void RespawnTutorialComplete(bool isComplete)
    {
        respawnTutorialComplete = isComplete;
        if (isComplete)
        {
            labelAnimator.SetTrigger("FadeOut");
        }
    }
}
