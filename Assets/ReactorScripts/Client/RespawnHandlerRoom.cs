using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using KS.Reactor.Client.Unity;
using KS.Reactor.Client;
using KS.Reactor;

public class RespawnHandlerRoom : ksRoomScript
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
    private List<ksEntity> respawnEntityList;
    private ksEntity respawnEntity;

    public override void Initialize()

    {
        // Room not getting disabled on the initial room object - throwing errors because.
        // Need to return if it is on that initial object. 
        // ^ This happened because I was using Start function not Initialize. Now it should be fine.
        if (Room == null)
        {
            // Disable this script
            //gameObject.GetComponent<RespawnHandler>().enabled = false;
            return;
        }
        respawnTimer = FunctionTimer.Create(InitiateRespawn, 5f, false);
        refreshRespawnList = FunctionTimer.Create(RefreshRespawnList, 0.5f, false);
        respawnTimer.gameObject.transform.SetParent(gameObject.transform);
        refreshRespawnList.gameObject.transform.SetParent(gameObject.transform);
        respawnEntityList = new List<ksEntity> { };
        GameEvents.current.GameOver += OnGameOver;
        GameEvents.current.StartRespawn += StartRespawn;
    }

    public override void Detached()
    {
        GameEvents.current.GameOver -= OnGameOver;
        GameEvents.current.StartRespawn -= StartRespawn;
    }
    // Update is called once per frame
    void Update()
    {
        //print(Room.GameObject);
        if (respawning)
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
                respawnIndex -= 1;

            }
            if (Input.GetKeyDown(KeyCode.D))
            {
                respawnIndex += 1;

            }
            return;
        }
    }

    public void StartRespawn()
    {
        respawnTimer.Start();
        respawning = true;

        respawnEntityList = ClientUtils.RetrieveControllableAllies(Room.LocalPlayer, Room.Entities);
        respawnIndex = 0;
        refreshRespawnList.Start();
    }

    private void RefreshRespawnList()
    {
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

