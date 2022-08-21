using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class MainMenuUI : MonoBehaviour
{
    private GameObject playerReadyLabelContainer;
    private GameObject readyButton;
    private GameObject input;
    private GameObject matchInProgressWarning;


    private void OnEnable()
    {
        GameObject.FindGameObjectWithTag("CineMachine").GetComponent<CinemachineVirtualCamera>().ForceCameraPosition(transform.position, Quaternion.identity);
    }

    // Start is called before the first frame update
    void Start()
    {
        GameEvents.current.RoomFound += OnRoomFound;
        GameEvents.current.Disconnected += BackToMainMenu;
        GameEvents.current.MatchInProgress += MatchInProgress;

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
            if (child.name.Contains("MatchInProgress"))
            {
                matchInProgressWarning = child.gameObject;
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
        matchInProgressWarning.GetComponent<MatchInProgress>().ActivateWarning();
    }    
}
