using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using KS.Reactor.Client.Unity;
using KS.Reactor.Client;
using KS.Reactor;

public class SpectatingHandler : ksRoomScript
{
    private FunctionTimer refreshEntityList;
    private int entityIndex = 0;
    public int EntityIndex
    {
        set
        {
            entityIndex = value;
            if (EntityList.Count == 0)
            {
                // Maybe give feedback to say there is no valid entities to spawn as
                entityIndex = 0;
                return;
            }
            else if (entityIndex < 0)
            {
                entityIndex = EntityList.Count - 1;
            }
            else if (entityIndex == EntityList.Count)
            {
                entityIndex = 0;
            }
            // Adjust cinemachine camera to that entity
            currentViewingEntity = EntityList[entityIndex];
            if (!ClientUtils.IsEntityValid(currentViewingEntity))
            {
                EntityList.Remove(currentViewingEntity);
                EntityIndex = entityIndex;
                return;
            }
            GameEvents.current.ChangeCamera(currentViewingEntity.GameObject.transform);
        }
        get { return entityIndex; }
    }


    private List<ksEntity> EntityList = new();

    private ksEntity currentViewingEntity;
    private bool spectating;

    // Called after properties are initialized.
    public override void Initialize()
    {
        GameEvents.current.StartSpectating += StartSpectating;
        GameEvents.current.Disconnected += OnDisconnectedFromRoom;
    }

    // Called when the script is detached.
    public override void Detached()
    {
        GameEvents.current.StartSpectating -= StartSpectating;
        GameEvents.current.Disconnected -= OnDisconnectedFromRoom;
    }

    // Called every frame.
    private void Update()
    {
        if (spectating)
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
                EntityIndex--;
            }
            if (Input.GetKeyDown(KeyCode.D))
            {
                EntityIndex++;
            }
        }
    }


    public void StartSpectating()
    {
        spectating = true;
        EntityList = ClientUtils.findAllEntities(Room.Entities);
        EntityIndex = 0;
        refreshEntityList = FunctionTimer.Create(refreshSpectatorList, 0.5f, false);
        refreshEntityList.Start();
    }

    private void refreshSpectatorList()
    {
        if (spectating)
        {
            refreshEntityList.Start();
            var list = ClientUtils.findAllEntities(Room.Entities);
            foreach (ksEntity e1 in list)
            {
                bool isNew = true;
                foreach (ksEntity e2 in EntityList)
                {
                    if (e1 == e2)
                    {
                        isNew = false;
                        break;
                    }
                }
                if (isNew)
                {
                    EntityList.Add(e1);
                }
            }
        }
    }

    private void OnDisconnectedFromRoom()
    {
        spectating = false;
    }
}