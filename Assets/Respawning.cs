using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Respawning : MonoBehaviour
{
    private TextMeshProUGUI tmp;
    private FunctionTimer updateTimer;
    private void Start()
    {
        updateTimer = FunctionTimer.Create(OnUpdateTimerTimeout, 0.4f, false, "RespawnUITimer");
        updateTimer.gameObject.transform.SetParent(transform);
        tmp = transform.Find("Title").GetComponent<TextMeshProUGUI>();
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
}
