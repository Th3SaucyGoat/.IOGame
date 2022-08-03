using System;
using System.Collections.Generic;
using System.Collections;
using KS.Reactor.Server;
using KS.Reactor;

public class HivemindServer : ksServerEntityScript , IFoodPickup
{
    private int _food;

    public int food {
        set
        {
            _food = value;
            Entity.Properties[Prop.FOOD] = value;
        }

        get { return _food; }
    }

    public int foodCapacity { set; get; } = 200;
    // Called when the script is attached.
    public override void Initialize()
    {
        Room.OnUpdate[0] += Update;
        food = 0;
        Entity.OnOverlapStart += OnOverlap;

    }

    // Called when the script is detached.
    public override void Detached()
    {
        Room.OnUpdate[0] -= Update;

    }

    // Called during the update cycle
    private void Update()
    {

    }

    [ksRPC(RPC.SPAWNCOLLECTOR)]
    public void SpawnCollector(ksIServerPlayer player)
    {
        ksLog.Info("server received request to spawn = " + Entity.Properties[Prop.FOOD]);
        if (Entity.Properties[Prop.FOOD] < 1)
        {
            return;
        }
        Entity.Properties[Prop.FOOD] -= 1;
        // Find the player's hivemind. Retrieve the position of the hivemind.
        Room.SpawnEntity("Collector", Entity.Position2D);

        // The collector needs to know it is of that unique team/player.
    }

    private void OnOverlap(ksCollider ours, ksCollider other)
    {
        {
            // Check to see if ours is the locator collider.
            if (ours.IsTrigger )
            {
                ksLog.Info(ours.CollisionFilter.AssetName);
            }


        }
    }
}