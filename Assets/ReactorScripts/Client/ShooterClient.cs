using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using KS.Reactor.Client.Unity;
using KS.Reactor.Client;
using KS.Reactor;

public class ShooterClient : ksEntityScript
{
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
                if (Entity.Properties[Prop.FIRING] == false)
                {
                    Entity.CallRPC(RPC.FIRING, true);


                }
            else if (Input.GetMouseButtonUp(0))
            {
                    Entity.CallRPC(RPC.FIRING, false);
            }
        }
    }
}