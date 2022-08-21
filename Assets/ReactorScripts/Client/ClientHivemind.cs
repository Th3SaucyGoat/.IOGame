using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using KS.Reactor.Client.Unity;
using KS.Reactor.Client;
using KS.Reactor;
using Cinemachine;
using TMPro;
using Example;

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
        print("Register Info Called");
        //player["Username"] = a[0];
        //player["Id"] = a[1];
        // UsernameLabel label = Room.GameObject.GetComponent<UsernameLabels>().CreateUserLabel(a[1].UInt, a[0].ToString());


        if (ClientUtils.CheckTeam(Room.LocalPlayer, Entity))
            {
            // Player needs to know which hivemind spawn requests go to.
            Room.LocalPlayer.GameObject.GetComponent<PlayerClient>().HiveId = Entity.Id;
            MatchStart();

        }
    }
    private void MatchStart()
    {
        //GameEvents.current.StartMatch?.Invoke(); // For changing UI.
        Entity.OnPropertyChange[Prop.FOOD] += FoodChanged;
        print("Value Changed");
        FoodChanged((ksMultiType) 0, (ksMultiType) 0);
    }

    private void FoodChanged(ksMultiType old, ksMultiType newV)
    {
        print("here");
        GameEvents.current.FoodChanged?.Invoke(newV);
    }

    public void HandleSpawnRequestFromPlayer(string type)
    {
        switch (type)
        {
            case "Collector":
                if (Entity.Properties[Prop.FOOD] >= Stats.CollectorCost)
                {
                    Entity.CallRPC(RPC.SPAWNUNIT, type);
                    if (!HUD.firstUnitSpawned)
                    {
                        GameEvents.current.FirstUnitSpawned();
                    }
                }
                break;
            case "Shooter":
                if (Entity.Properties[Prop.FOOD] >= Stats.ShooterCost)
                {
                    Entity.CallRPC(RPC.SPAWNUNIT, type);
                    if (!HUD.firstUnitSpawned)
                    {
                        GameEvents.current.FirstUnitSpawned();
                    }
                }
                break;
        }
    }

}