using System;
using System.Collections.Generic;
using System.Collections;
using KS.Reactor.Server;
using KS.Reactor;

public class UnitServer : ksServerEntityScript
{
    [ksRPC(RPC.FOLLOW)]
    private void ChangeFollowEntity(ksIServerPlayer player)
    {
        // Get entity to follow
        uint id = player.Properties[Prop.CONTROLLEDENTITYID];
        ksIServerEntity entity = Room.GetEntity(id);
        // Access AI behavior script, and adjust
        IBehavior behave = Entity.Scripts.Get<IBehavior>();
        behave.EntityToFollow = entity;

        // Maybe call a function to make a smooth transition in behavior 

    }
}