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
    public Transform softBody;
    [SerializeField]
    private Transform organelle;
    private Transform softBodyInstanced;
    public Dictionary<string, ksMultiType> player;

    private int frameNum;
    private bool isFiring;

    // Called after properties are initialized.

    private void Awake()
    {

    }
    public override void Initialize()
    {
        player = new Dictionary<string, ksMultiType>();
        // UsernameLabel label = Room.GameObject.GetComponent<UsernameLabels>().CreateUserLabel(a[1].UInt, a[0].ToString());
        
        softBodyInstanced = Instantiate(softBody, transform.position, Quaternion.identity);


        //Instantiate(organelle, transform.position, Quaternion.identity);
    }


    private void Start()
    {
        Invoke("LateStart", 0.5f);
    }

    private void LateStart()
    {
        foreach (Transform t in softBodyInstanced)
        {
            var here = t.GetComponents<SpringJoint2D>();
            foreach (SpringJoint2D sping in here)
            {
                sping.autoConfigureDistance = false;
            }
        }
    }

    // Called when the script is detached.
    public override void Detached()
    {
        Entity.OnPropertyChange[Prop.FOOD] -= FoodChanged;
    }

    // Called every frame.
    private void Update()
    {
        if (Entity.PlayerController != null)
        {
            if (Input.GetMouseButtonDown(0) && isFiring == false)
            {
                Entity.CallRPC(RPC.FIRING, true);
                isFiring = true;
            }
            else if (Input.GetMouseButtonUp(0) && isFiring == true)
            {
                Entity.CallRPC(RPC.FIRING, false);
                isFiring = false;
            }
            if (frameNum == 10)
            {
                frameNum = 0;
                ksMultiType pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                Entity.CallRPC(RPC.RELAYMOUSEINFO, pos);
            }
            frameNum++;
        }

    }

    [ksRPC(RPC.SENDINFO)]
    public void RegisterInfo( ksMultiType[] a)
    {
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
        FoodChanged((ksMultiType) 0, (ksMultiType) 0);
    }

    private void FoodChanged(ksMultiType old, ksMultiType newV)
    {
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
                        GameEvents.current.FirstUnitSpawned?.Invoke();
                    }
                }
                break;
            case "Shooter":
                if (Entity.Properties[Prop.FOOD] >= Stats.ShooterCost)
                {
                    Entity.CallRPC(RPC.SPAWNUNIT, type);
                    if (!HUD.firstUnitSpawned)
                    {
                        GameEvents.current.FirstUnitSpawned?.Invoke();
                    }
                }
                break;
        }
    }

}