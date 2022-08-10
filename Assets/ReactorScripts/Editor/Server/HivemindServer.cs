using System;
using System.Collections.Generic;
using System.Collections;
using KS.Reactor.Server;
using KS.Reactor;

public class HivemindServer : ksServerEntityScript , IFoodPickup , IDamagable , IMovement , ICommandable
{
    public float Speed { set; get; } = 2f;
    public float Acceleration { set; get; } = 5f;

    public ksIServerEntity EntityToFollow { set; get; }
    public ksIServerEntity Hivemind { set; get; }

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

    enum BEHAVIOR { Collect, Follow }
    BEHAVIOR behavior = BEHAVIOR.Collect;

    private ksIServerEntity foodTarget;
    private List<ksIServerEntity> foodInRange;
    private int foodSearchDelay;

    private ksRigidBody2DView rb;

    // Called when the script is attached.
    public override void Initialize()
    {
        Room.OnUpdate[0] += Update;
        food = 0;
        Health = 20;
        rb = Entity.Scripts.Get<ksRigidBody2DView>();
        Entity.OnOverlapStart += OnOverlap;
        Entity.OnOverlapEnd += OnOverlapExit;
    }

    // Called when the script is detached.
    public override void Detached()
    {
        Room.OnUpdate[0] -= Update;
        Entity.OnOverlapStart -= OnOverlap;
        Entity.OnOverlapEnd -= OnOverlapExit;

    }

    // Called during the update cycle
    private void Update()
    {
        if (behavior == BEHAVIOR.Collect)
        {
            // if have foodTarget, and it is in range of hivemind, follow
            if (!Checks.IsTargetValid(foodTarget))
            {
                foodSearchDelay++;
                if (foodSearchDelay == 200)
                {
                    foodSearchDelay = 0;
                    foodTarget = FindClosestFood();
                }
            }
            else
            {
                moveTowardsPoint(foodTarget.Position2D);
            }
        }
    }

    private void moveTowardsPoint(ksVector2 point)
    {
        ksVector2 direction = point - Entity.Position2D;
        rb.Velocity = ksVector2.MoveTowards(rb.Velocity, direction.Normalized() * Speed, Time.Delta * Acceleration);
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
    private void OnOverlap(ksCollider ours, ksCollider other)
    {
        if (ours.IsTrigger)
        {
            foodInRange.Add(other.Entity);
        }
    }

    private void OnOverlapExit(ksCollider ours, ksCollider other)
    {
        if (ours.IsTrigger)
        {
            foodInRange.Remove(other.Entity);
        }
    }

    private ksIServerEntity FindClosestFood()
    {
        float closestDistance = 999999f;
        ksIServerEntity closestTarget = null;
        var foodToRemove = new List<ksIServerEntity> { };
        foreach (ksIServerEntity f in foodInRange)
        {
            if (!Checks.IsTargetValid(foodTarget))
            {
                //ksLog.Info("FOOD IN RANGE HAS A DESTROYED FOOD");
                foodToRemove.Add(f);
                continue;
            }
            ksVector2 position = Entity.Position2D - f.Position2D;
            float distance = position.Magnitude();
            if (distance < closestDistance)
            {
                closestTarget = f;
                closestDistance = distance;
            }
        }
        foreach (ksIServerEntity f in foodToRemove)
        {
            foodInRange.Remove(f);
        }
        return closestTarget;
    }

    public void ChangeFollow()
    {
        ksLog.Info("Here Follow");
    }

}