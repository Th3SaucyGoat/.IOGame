using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using KS.Reactor.Client.Unity;
using KS.Reactor.Client;
using KS.Reactor;

public class RespawnHandler : ksRoomScript
{
    public FunctionTimer respawnTimer;
    private FunctionTimer refreshRespawnList;
    private bool respawning = false;
    private int _respawnIndex = 0;
    private int respawnIndex
    {
        set
        {
            _respawnIndex = value;
            if (respawnEntityList.Count == 0)
            {
                // Maybe give feedback to say there is no valid entities to spawn as
                _respawnIndex = 0;
            }
            else if (_respawnIndex < 0)
            {
                _respawnIndex = respawnEntityList.Count - 1;
            }
            else if (_respawnIndex == respawnEntityList.Count)
            {
                _respawnIndex = 0;
            }
            // Adjust cinemachine camera to that entity
            respawnEntity = respawnEntityList[respawnIndex];
            if (!ClientUtils.IsEntityValid(respawnEntity))
            {
                respawnEntityList.Remove(respawnEntity);
                respawnIndex = respawnIndex;
            }
            GameEvents.current.ChangeCamera(respawnEntity.GameObject.transform);
        }
        get { return _respawnIndex; }
    }
    private List<ksEntity> respawnEntityList = new List<ksEntity> { };
    private ksEntity respawnEntity;

    void Start()
    {
        respawnTimer = FunctionTimer.Create(InitiateRespawn, 5f, false);
        refreshRespawnList = FunctionTimer.Create(RefreshRespawnList, 0.5f, false);
        respawnTimer.gameObject.transform.SetParent(gameObject.transform);
        refreshRespawnList.gameObject.transform.SetParent(gameObject.transform);
        GameEvents.current.GameOver += OnGameOver;
        GameEvents.current.StartRespawn += StartRespawn;
    }

    // Update is called once per frame
    void Update()
    {
        if (respawning)
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                // Put viable allies index down one
                respawnIndex -= 1;
            }
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                // Put viable allies index up one
                respawnIndex += 1;
            }
            return;
        }
    }

    public void StartRespawn()
    {
        // Start countdown. Show UI item. Should be hooked up to the time remaining on the timer.
        respawnTimer.Start();
        respawning = true;

        // Retrieve List of all valid allies
        respawnEntityList = ClientUtils.RetrieveControllableAllies(Room.LocalPlayer, Room.Entities);
        respawnIndex = 0;
        print("Starting Respawn!");
    }



    private void RefreshRespawnList()
    {
        // Add only new entities to the respawn entity list
        var list = ClientUtils.RetrieveControllableAllies(Room.LocalPlayer, Room.Entities);
        foreach (ksEntity e1 in list)
        {
            bool isNew = true;
            foreach (ksEntity e2 in respawnEntityList)
            {
                if (e1 == e2)
                {
                    isNew = false;
                    break;
                }
            }
            if (isNew)
            {
                respawnEntityList.Add(e1);
            }
        }
        if (respawning)
        {
            refreshRespawnList.Start();
        }
    }

    public void InitiateRespawn()
    {
        // Check if game allows for respawning still
        if (respawning == true)
        {
            Room.CallRPC(RPC.REQUESTCONTROL, respawnEntity.Id);
            respawning = false;
            GameEvents.current.EndRespawn?.Invoke();
        }


        // If entity that you are currently on is null. Allow for viewing of other non-valid team entities.
        // Immediately respawn when able based off the refreshing of the list.
    }

    public void OnGameOver(bool isVictory)
    {
        // Make initiate respawn impossible
        respawning = false;
        // Maybe allow for viewing of other teams units, if there is any
    }
}

