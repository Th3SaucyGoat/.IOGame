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
        if (Input.GetKeyDown("1"))
        {
            spawnRequester("Collector");
        }
        if (Input.GetKeyDown("2"))
        {
            spawnRequester("Shooter");
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
    }

    private bool HandleSwitchControl(Collider result)
    {
        // Convert to ksIEntity
        GameObject new_ally = result.gameObject;
        ksIEntity ally = new_ally.GetComponent<ksEntityScript>().Entity;
        uint allyId = ally.Id;
         //Verify the switch can be valid
        if (Player.Properties[Prop.TEAMID].Int == ally.Properties[Prop.TEAMID] && ally.Properties[Prop.CONTROLLEDPLAYERID] == "")
        {
            print(allyId);
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
}