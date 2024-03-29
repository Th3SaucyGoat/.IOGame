using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.EventSystems;
using KS.Reactor.Client.Unity;
using KS.Reactor.Client;
using KS.Reactor;
using UnityEngine;

public class ClientRoom : ksRoomScript
{

    [SerializeField]
    private Transform playerReadyLabelContainer;

    [SerializeField]
    private Transform playerReadyLabel;

    // Called after properties are initialized.
    public override void Initialize()
    {
        GameEvents.current.onReady += SetReadyStatus;
        print("Subbed");
        Room.OnPlayerJoin += PlayerJoin;
        Room.OnPlayerLeave += PlayerLeave;
    }

    // Called when the script is detached.
    public override void Detached()
    {
        Room.OnPlayerJoin -= PlayerJoin;
        Room.OnPlayerLeave -= PlayerLeave;
        print("Detached");
        GameEvents.current.onReady -= SetReadyStatus;

    }

    // Called when a player connects.
    private void PlayerJoin(ksPlayer player)
    {
        //players.Add(player);
        //print(player.Properties[Prop.NAME]);
        print("Joined");
        Transform label =  Instantiate(playerReadyLabel);
        label.SetParent(playerReadyLabelContainer);
        PlayerReadyLabel readyLabel = label.GetComponent<PlayerReadyLabel>();
        readyLabel.username = player.Properties[Prop.NAME];
        readyLabel.Id = player.Id;
        readyLabel.Ready = player.Properties[Prop.READY];
        // Instantiate username labels. Set Inactive
        UsernameLabel userlabel = GetComponent<UsernameLabels>().CreateUserLabel(player.Id, player.Properties[Prop.NAME]);
        userlabel.gameObject.SetActive(false);
        if (player.IsLocal)
        {
        }
        else
        {

        }
    }

    // Called when a player disconnects.
    private void PlayerLeave(ksPlayer player)
    {
        //players.Remove(player);
        PlayerReadyLabel[] playerLabels = playerReadyLabelContainer.GetComponentsInChildren<PlayerReadyLabel>();

        foreach (PlayerReadyLabel label in playerLabels)
        {
            if (label.Id == player.Id)
            {
                Destroy(label.gameObject);
            }
        }
    }
    public void SetReadyStatus()
    {
        bool readyStatus = Room.LocalPlayer.Properties[Prop.READY] ? false : true; 
        Room.CallRPC(RPC.SETREADYSTATUS, new ksMultiType[] { readyStatus });
    }

    [ksRPC(RPC.READYSTATUS)]
    public void SetPlayerReadyStatus(uint playerId, bool readyStatus)
    {
        PlayerReadyLabel[] playerLabels = playerReadyLabelContainer.GetComponentsInChildren<PlayerReadyLabel>();
        foreach (PlayerReadyLabel label in playerLabels)
        {
            if (label.Id == playerId)
            {
                label.Ready = readyStatus;
            }
        }

    }

    [ksRPC(RPC.STARTMATCH)]
    private void StartMatch()
    {
        // Signal start match event
        GameEvents.current.StartMatchTimer?.Invoke();
    }

    [ksRPC(RPC.PLAYERCONTROLLEDENTITYDESTROYED)]
    private void PlayerControlledEntityDestroyed(uint pId)
    {
        ksEntity entity = null;
        // See if the current entity the player is controlling is still valid.
        entity = Room.GetEntity(Room.LocalPlayer.Properties[Prop.CONTROLLEDENTITYID]);
        // Get rid of username tag
        UsernameLabels.currentLabels[pId].gameObject.SetActive(false);
        if (ClientUtils.IsEntityValid(entity))
        {
            // Someone elses
        }
        else
        {
            // Check if has hivemind. Else, show game over screen.
            if (Room.LocalPlayer.Properties[Prop.HIVEMINDID] == "")
            {
                // Show Game over screen
                GameEvents.current.GameOver(false);
            }
            // Mine. Initiate Respawn Sequence
            else
            {
                // Emit event for UI. 
                //Room.GameObject.GetComponent<RespawnHandler>().StartRespawn();
                GameEvents.current.StartRespawn();
            }
        }
        // Reset entity reference for this username label
        UsernameLabels.entityReference[pId] = null;
        
    }

    private void changeScore(ksMultiType oldValue, ksMultiType newValue)
    {

    }

    private int frameNum = 0;

    void Update()
    {
        if (frameNum == 200)
        {
            //print(Room);
            frameNum = 0;

        }
        frameNum++;
    }

    [ksRPC(RPC.ENDGAME)]
    private void InitiateGameOver(bool isVictory)
    {
        //// If they are controlling a unit, allow them to keep doing so
        //if (Room.LocalPlayer.Properties[Prop.CONTROLLEDENTITYID] != "" )
        //{

        //}
        //// Show the Game Over screen which absorbs inputs
        //else
        //{
        //    GameEvents.current.GameOver(isVictory);
        //}
        GameEvents.current.GameOver(isVictory);
    }

    [ksRPC(RPC.REDIRECT)]
    private void KickAfterTimer()
    {
        // Hook up a UI element to this function timer.
        GameEvents.current.StartRedirect();
    }


}