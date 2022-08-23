using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class MainMenuUI : MonoBehaviour
{
    private GameObject playerReadyLabelContainer;
    private GameObject readyButton;
    private GameObject input;
    private GameObject redMenuText;
    private FunctionTimer matchingStartingTimer;


    private void OnEnable()
    {
        GameObject.FindGameObjectWithTag("CineMachine").transform.position = Vector3.zero;
    }

    // Start is called before the first frame update
    void Start()
    {
        GameEvents.current.RoomFound += OnRoomFound;
        GameEvents.current.Disconnected += BackToMainMenu;
        GameEvents.current.MatchInProgress += MatchInProgress;
        GameEvents.current.StartMatchTimer += StartMatchTimer;

        foreach (Transform child in transform)
        {
            foreach (Transform grandchild in child.transform)
            {
                if (grandchild.name.Contains("Container"))
                {
                    playerReadyLabelContainer = grandchild.gameObject;
                }
            }
            if (child.name.Contains("ReadyButton"))
            {
                readyButton = child.gameObject;
            }
            if (child.name.Contains("Input"))
            {
                input = child.gameObject;
            }
            if (child.name.Contains("redMenuText"))
            {
                redMenuText = child.gameObject;
            }
        }
        
    }

    

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnRoomFound()
    {
        input.SetActive(false);
        readyButton.SetActive(true);
        playerReadyLabelContainer.SetActive(true);
    }

    private void BackToMainMenu()
    {
        input.SetActive(true);
        readyButton.SetActive(false);
        playerReadyLabelContainer.SetActive(false);
    }

    private void MatchInProgress()
    {
        redMenuText.GetComponent<MatchInProgress>().ActivateWarning();
    }

    private void StartMatchTimer()
    {
        matchingStartingTimer = FunctionTimer.Create(StartMatch, 5.0f);
        matchingStartingTimer.Start();
        redMenuText.GetComponent<MatchInProgress>().DisplayMatchStartTimeout(matchingStartingTimer);
    }

    private void StartMatch()
    {
        GameEvents.current.StartMatch();

        foreach (Transform trans in playerReadyLabelContainer.transform)
        {
            Destroy(trans.gameObject);
        }
    }    
}
