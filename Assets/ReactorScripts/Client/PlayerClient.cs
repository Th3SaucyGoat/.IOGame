using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using KS.Reactor.Client.Unity;
using KS.Reactor.Client;
using KS.Reactor;
using KS;

public class PlayerClient : ksPlayerScript
{
    GameObject myHivemind;
    private delegate void SpawnRequester(string s);
    private SpawnRequester spawnRequester;
    private uint _hiveID;
    private bool switchtoAlly;
    [SerializeField]
    private LayerMask layerMask;
    private bool spawnMenuActive;

 

    public uint HiveId
    {
        set
        {
            _hiveID = value;

            HivemindIDChanged(value);

        }
        get { return _hiveID; }
    }
    public override void Initialize()
    {

    }

    public override void Detached()
    {
    }

    // Called every frame.
    private void Update()
    {
        // Need to verify that the player is controlling an entity or spawned in first
        if (Player.Properties[Prop.CONTROLLEDENTITYID] == "")
        {
            return;
        }
        if (Input.GetKeyDown("1"))
        {
            if (spawnMenuActive == true)
            {
                spawnRequester("Collector");
            }
            else
            {
                // Issue command to follow +1
                AllyFollow();
            }
        }
        if (Input.GetKeyDown("2"))
        {
            if (spawnMenuActive == true)
            {
                spawnRequester("Shooter");
            }
            else
            {
                // I see command to dismiss
                DismissFollowingAllies();
            }
        }
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            switchtoAlly = true;
            // Give feedback that selection is ready.
        }
        if (switchtoAlly && Input.GetMouseButtonDown(0))
        {
            // Check if selected an ally
            //Camera cam = GameObject.FindGameObjectWithTag("MainCamera");
            Vector3 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3 new_pos = new Vector3( pos.x, pos.y, 0f );
            Collider[] result = Physics.OverlapSphere( new_pos, 0.2f, layerMask);
            switchtoAlly = false;
            bool success = false;
            //print(result.Length);
            if (result.Length > 0)
            {
                success = HandleSwitchControl(result[0]);
            }
            if (success == false)
            {
                // Give player feedback selection failed

            }
        }
        if (Input.GetKeyDown(KeyCode.CapsLock))
        {
            // pull up the spawn menu
            spawnMenuActive = spawnMenuActive ? false : true;
            print("Spawn Menu Active? " + spawnMenuActive);

        }
    }

    private bool HandleSwitchControl(Collider result)
    {
        // Convert to ksIEntity
        GameObject new_ally = result.gameObject;
        ksEntity ally = new_ally.GetComponent<ksEntityScript>().Entity;
        uint allyId = ally.Id;
         //Verify the switch can be valid
        if (ClientUtils.CheckTeam(Player, ally) && ally.Properties[Prop.CONTROLLEDPLAYERID] == "")
        {
            Room.CallRPC(RPC.REQUESTCONTROL, allyId);
            
            return true;
        }
        return false;
    }

    private void HivemindIDChanged(uint newV)
    {
        myHivemind = Room.GetEntity(newV).GameObject;
        spawnRequester = myHivemind.GetComponent<ClientHivemind>().HandleSpawnRequestFromPlayer;
    }
    private void DismissFollowingAllies()
    {
        // Retrieve all following allies. Could Loop through all the allies and check their player following property, or store an array.
        var alliesFollowing = new List<ksEntity> { };
        foreach (ksEntity entity in Room.Entities)
        {
            if (entity.Properties[Prop.PLAYERFOLLOWINGID].Int == Player.Id)
            {
                alliesFollowing.Add(entity);
            }
        }
        // Call Rpc
        foreach (ksEntity entity in alliesFollowing)
        {
            entity.CallRPC(RPC.DISMISS);
        }
    }

    private void AllyFollow()
    {
        //Retrieve closest valid ally.
        var validAllies = ClientUtils.FindCommandableAllies(Player, Room.Entities);

        ksEntity entity = ClientUtils.FindClosestEntity(Room.GetEntity(Player.Properties[Prop.CONTROLLEDENTITYID]), validAllies);

        if (ClientUtils.IsEntityValid(entity))
        {
            print("Here" + entity.GameObject.name);

            entity.CallRPC(RPC.FOLLOW);
        }
        else
        {
            print("ally is null");
        }

    }
}
