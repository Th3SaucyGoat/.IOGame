using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using KS.Reactor.Client.Unity;
using KS.Reactor.Client;
using KS.Reactor;
using Cinemachine;

public class Unit : ksEntityScript
{
    public UsernameLabel usernameLabel;

    private void Update()
    {
        if (usernameLabel != null)
        {
            usernameLabel.Position = transform.position;
        }
    }

    [ksRPC(RPC.TAKECONTROL)]
    private void TakeControl(uint pId)
    {
        print("here");
        // Figure out if this pId is the local player's
        if (Room.LocalPlayerId == pId)
        {
            // Hook up camera
            CinemachineVirtualCamera cine = GameObject.FindGameObjectWithTag("CineMachine").GetComponent<CinemachineVirtualCamera>();
            cine.LookAt = transform;
            cine.Follow = transform;
        }
        // Regardless, display the username of the player underneath this entity
        UsernameLabel labelForThisPlayer = UsernameLabels.currentLabels[pId];
        UsernameLabels.SetEntity(labelForThisPlayer, transform);
    }


}