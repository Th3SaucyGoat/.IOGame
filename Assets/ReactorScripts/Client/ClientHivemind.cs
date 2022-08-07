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
            CinemachineVirtualCamera cine = GameObject.FindGameObjectWithTag("CineMachine").GetComponent<CinemachineVirtualCamera>();
            cine.Follow = transform;
            cine.LookAt = transform;
            GameEvents.current.StartMatch?.Invoke();
            Entity.OnPropertyChange[Prop.FOOD] += FoodChanged;
            // Make username label a child of this entity
            //Entity.OnPropertyChange[]

        }
        else
        {
            
        }
    }

    private void Start()
    {

    }

    // Called when the script is detached.
    public override void Detached()
    {
        
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
        UsernameLabel label = Room.GameObject.GetComponent<UsernameLabels>().CreateUserLabel(a[1].UInt, a[0].ToString());
        UsernameLabels.SetEntity(label, transform);
        //label.transform.SetParent(Room.GameObject.transform);
        print(label.gameObject.GetComponent<TextMeshPro>().text);

        if (Room.LocalPlayer.Properties[Prop.TEAMID].Int == Entity.Properties[Prop.TEAMID].Int)
        {
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