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
        if (Input.GetKeyDown("1"))
        {
            if (spawnMenuActive == true)
            {
                spawnRequester("Collector");
            }
            else
            {
                // Issue command to follow +1
                
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
                // Isee command to dismiss
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
            spawnMenuActive = true ? false : true; 

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

    private ksEntity FindClosestAlly()
    {
        var validAllies = new List<ksEntity> { };
        foreach (ksEntity entity in Room.Entities)
        {
            // Check if it is a Unit, that is on the same team, that is not controlled, that is not already following a player.
            if (entity.GameObject.TryGetComponent(out Unit unit))
            {
                if (entity.Properties[Prop.TEAMID] == Player.Properties[Prop.TEAMID])
                {
                    if (entity.Properties[Prop.CONTROLLEDPLAYERID] == "")
                    {
                        if (entity.Properties[Prop.PLAYERFOLLOWINGID] == "")
                        {
                            validAllies.Add(entity);
                        }
                    }
                }
            }
        }
        print(validAllies.Count);
        float closestDistance = 999999f;
        ksEntity closestTarget = null;
        foreach (ksEntity ally in validAllies)
        {
            if (ally == null)
            {
                print("Ally threw null reference during issuing command");
                continue;
            }
            ksEntity currentControlledEntity = Room.GetEntity(Player.Properties[Prop.CONTROLLEDENTITYID]);
            Vector2 diff = currentControlledEntity.Position2D - ally.Position2D;
            float distance = diff.magnitude;
            if (distance < closestDistance)
            {
                closestTarget = ally;
                closestDistance = distance;
            }
        }
        if (closestDistance < 9999f)
        {
            return closestTarget;
        }
        else
        {
            return null;
        }
    }
}

public class ClosestAlly : ksPlayerScript
{


}
