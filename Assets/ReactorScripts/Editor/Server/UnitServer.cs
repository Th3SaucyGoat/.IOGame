using System;
using System.Collections.Generic;
using System.Collections;
using KS.Reactor.Server;
using KS.Reactor;

public class UnitServer : ksServerEntityScript
{
    [ksRPC(RPC.FOLLOW)]
    private void ChangeEntityFollowing(ksIServerPlayer player)
    {
        // Get entity to follow
        uint id = player.Properties[Prop.CONTROLLEDENTITYID];
        ksIServerEntity entity = Room.GetEntity(id);
        // Access AI behavior script, and adjust
        ICommandable behave = Entity.Scripts.Get<ICommandable>();
        behave.EntityToFollow = entity;
        // Set the Entity's player following property, so that it can be tracked
        Entity.Properties[Prop.PLAYERFOLLOWINGID] = player.Id;
        // Maybe call a function to make a smooth transition in behavior 

    }

    [ksRPC(RPC.DISMISS)]
    private void DismissEntity(ksIServerPlayer player)
    {
        // Access AI behavior script, reset EntityToFollow
        ICommandable behavior = Entity.Scripts.Get<ICommandable>();
        behavior.EntityToFollow = behavior.Hivemind;
        // Reset Properties
        Entity.Properties[Prop.PLAYERFOLLOWINGID] = "";
    }
}