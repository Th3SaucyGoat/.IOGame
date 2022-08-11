using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using KS.Reactor.Client.Unity;
using KS.Reactor.Client;
using KS.Reactor;
using Cinemachine;
using TMPro;

public class ClientHivemind : ksEntityScript
{
    public UsernameLabel prefab;
    public Dictionary<string, ksMultiType> player;
    // Called after properties are initialized.

    private void Awake()
    {

    }
    public override void Initialize()
    {
        player = new Dictionary<string, ksMultiType>();
        if (Entity.PlayerController != null)
        {
            GameEvents.current.ChangeCamera(transform);
            GameEvents.current.StartMatch?.Invoke();
            Entity.OnPropertyChange[Prop.FOOD] += FoodChanged;
            FoodChanged(0, 0);
            // Make username label a child of this entity
            //Entity.OnPropertyChange[]
            var entity = Room.GetEntity(99999);
            print(ClientUtils.IsEntityValid(entity));
        }
       // UsernameLabel label = Room.GameObject.GetComponent<UsernameLabels>().CreateUserLabel(a[1].UInt, a[0].ToString());
    }

    private void Start()
    {

    }

    // Called when the script is detached.
    public override void Detached()
    {
        Entity.OnPropertyChange[Prop.FOOD] -= FoodChanged;
    }

    // Called every frame.
    private void Update()
    {
    }

    [ksRPC(RPC.SENDINFO)]
    public void RegisterInfo( ksMultiType[] a)
    {
        player["Username"] = a[0];
        player["Id"] = a[1];
        // UsernameLabel label = Room.GameObject.GetComponent<UsernameLabels>().CreateUserLabel(a[1].UInt, a[0].ToString());
        //UsernameLabels.SetEntity(a[1], Entity);

        UsernameLabels.SetEntity(a[1], Entity);

        if (ClientUtils.CheckTeam(Room.LocalPlayer, Entity))
            {
            // Player needs to know which hivemind spawn requests go to.
            Room.LocalPlayer.GameObject.GetComponent<PlayerClient>().HiveId = Entity.Id;
        }
    }

    private void FoodChanged(ksMultiType old, ksMultiType newV)
    {
        GameEvents.current.FoodChanged(newV);
    }

    public void HandleSpawnRequestFromPlayer(string type)
    {
        switch (type)
        {
            case "Collector":
                if (Entity.Properties[Prop.FOOD] >= 0)
                {
                    Entity.CallRPC(RPC.SPAWNUNIT, type);
                }
                break;
            case "Shooter":
                if (Entity.Properties[Prop.FOOD] >= 0)
                {
                     //call shooter rpc
                    Entity.CallRPC(RPC.SPAWNUNIT, type);
                }
                break;
        }
    }

}