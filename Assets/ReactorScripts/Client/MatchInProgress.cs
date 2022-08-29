using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Example;


public class MatchInProgress : MonoBehaviour
{
    private TextMeshProUGUI textMesh;
    private bool active;
    private FunctionTimer matchWarningTimer;
    private FunctionTimer matchStartTimer;

    private void OnEnable()
    {
        textMesh = GetComponent<TextMeshProUGUI>();
        matchWarningTimer = FunctionTimer.Create(Timeout, 1.0f, false);
        textMesh.color = new Color(1, 0, 0, 0);

    }

    private void Update()
    {

        if (matchStartTimer != null && matchStartTimer.gameObject != null)
        {
            textMesh.text = $"Match will start in {(int)matchStartTimer.TimeRemaining()} seconds";
        }
    }

    public void ActivateWarning()
    {
        textMesh.color = new Color(1f, 0f, 0f, 1f);
        textMesh.text = "Please wait for current match to complete";
        active = true;
        matchWarningTimer.Start();
    }

    private void Timeout()
    {
        print("Timed out");

        if (active)
        {
            active = false;
            matchWarningTimer.Start(0.05f);
        }
        else
        {
            float a = textMesh.color.a;
            a -= .02f;
            textMesh.color = new Color(textMesh.color.r, textMesh.color.g, textMesh.color.b, a);
            if (a > 0)
            {
                matchWarningTimer.Start(0.05f);
            }
        }
    }

    public void DisplayMatchStartTimeout(FunctionTimer theTimer)
    {
        matchStartTimer = theTimer;
        textMesh.color = new Color(1f, 0f, 0f, 1f);
    }
}
