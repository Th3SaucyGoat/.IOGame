using System;
using System.Collections.Generic;
using System.Collections;
using KS.Reactor.Server;
using KS.Reactor;

public class HivemindServer : ksServerEntityScript , IFoodPickup , IDamagable
{
    private int _food;

    public int food {
        set
        {
            ksRange range = new ksRange(0, foodCapacity);
            _food = (int) range.Clamp(value);
            Entity.Properties[Prop.FOOD] = _food;
        }
        get { return _food; }
    }

    public int MaxHealth { get; }
    private int health;
    public int Health
    {
        set
        {
            health = value;
            if (health <= 0)
            {
                Entity.Destroy();
            }
        }
        get { return health; }
    }

    public int foodCapacity { set; get; } = 200;
    // Called when the script is attached.
    public override void Initialize()
    {
        Room.OnUpdate[0] += Update;
        food = 0;
        Health = 20;
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

    [ksRPC(RPC.SPAWNUNIT)]
    public void SpawnCollector(ksIServerPlayer player, string type)
    {
        ksLog.Info("server received request to spawn = " + Entity.Properties[Prop.FOOD]);
        if (Entity.Properties[Prop.FOOD] < 0)
        {
            return;
        }
        switch (type)
        {
            case "Collector":
                ksIServerEntity collector = Room.SpawnEntity("Collector", Entity.Position2D);
                collector.Properties[Prop.TEAMID] = Entity.Properties[Prop.TEAMID];
                var colServe = collector.Scripts.Get<CollectorServer>();
                colServe.Hivemind = Entity;
                colServe.DelayedStart();
                break;
            case "Shooter":
                ksIServerEntity shooter = Room.SpawnEntity("Shooter", Entity.Position2D);
                shooter.Properties[Prop.TEAMID] = Entity.Properties[Prop.TEAMID];
                var shoot = shooter.Scripts.Get<ShooterServer>();
                shoot.Hivemind = Entity;
                shoot.DelayedStart();
                break;
        }
        Entity.Properties[Prop.FOOD] -= 1;
        // Find the player's hivemind. Retrieve the position of the hivemind.

        // The collector needs to know it is of that unique team/player.

        //ksLog.Info("IS ASSIGNED? = "+ hive.Scripts.Get<CollectorServer>().Hivemind.ToString());
    }

}