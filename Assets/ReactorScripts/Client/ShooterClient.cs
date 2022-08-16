using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using KS.Reactor.Client.Unity;
using KS.Reactor.Client;
using KS.Reactor;

public class ShooterClient : ksEntityScript
{
    private int frameNum = 0;

    private bool isFiring = false;


    public override void Initialize()
    {
    }

    public override void Detached()
    {
        
    }

    private void Update()
    {
        if (Entity.PlayerController != null)
        {
            if (Input.GetMouseButtonDown(0) && isFiring == false)
            {
                Entity.CallRPC(RPC.FIRING, true);
                isFiring = true;
                if (!HUD.shooterTutorialDone)
                {
                    GameEvents.current.FiredasShooter();
                }
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
}