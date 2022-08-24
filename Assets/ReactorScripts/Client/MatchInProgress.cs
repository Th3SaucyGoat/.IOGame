using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Example;


public class MatchInProgress : MonoBehaviour
{
    private TextMeshProUGUI textMesh;
    private bool active;
    private FunctionTimer timer;
    private FunctionTimer matchStartTimer;

    private void Start()
    {
        textMesh = GetComponent<TextMeshProUGUI>();
        timer = FunctionTimer.Create(Timeout, 1.0f, false);
    }

    private void OnEnable()
    {
        if (textMesh != null)
        {
            textMesh.color = new Color(1, 0, 0, 0);
        }
    }

    private void Update()
    {
        if (matchStartTimer != null && matchStartTimer.gameObject != null)
        {
            textMesh.text = $"Match will start in {(int)matchStartTimer.TimeRemaining()} seconds";
        }
        else if (textMesh.color.a != 0f)
        {
            textMesh.color = new Color(1f, 0f, 0f, 0f);
        }
    }

    public void ActivateWarning()
    {
        textMesh.color = new Color(1, 0, 0, 1);
        active = true;
        timer.Start();
    }

    private void Timeout()
    {
        if (active)
        {
            active = false;
            timer.Start(0.05f);
        }
        else
        {
            float a = textMesh.color.a;
            a -= .02f;
            textMesh.color = new Color(textMesh.color.r, textMesh.color.g, textMesh.color.b, a);
            if (a > 0)
            {
                timer.Start(0.05f);
            }
        }
    }

    public void DisplayMatchStartTimeout(FunctionTimer theTimer)
    {
        matchStartTimer = theTimer;
        textMesh.color = new Color(1, 0, 0, 1);
    }
}
