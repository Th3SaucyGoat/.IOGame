using System;
using System.Collections.Generic;
using System.Collections;
using KS.Reactor.Server;
using KS.Reactor;

public class HivemindServer : ksServerEntityScript , IFoodPickup , IMovement , ICommandable
{
    public float Speed { set; get; } = 2f;
    public float Acceleration { set; get; } = 5f;
    private ksIServerEntity _EntityToFollow;
    public ksIServerEntity EntityToFollow
    {
        set
        {
            _EntityToFollow = value;
            if (value == null)
            {
                behavior = BEHAVIOR.Collect;
            }
            else
            {
                behavior = BEHAVIOR.Follow;
            }
        }
        get
        {
            return _EntityToFollow;
        }
    }
    public ksIServerEntity Hivemind { set; get; } = null;

    public int food {
        set
        {
            ksRange range = new ksRange(0, foodCapacity);
            Entity.Properties[Prop.FOOD] = (int) range.Clamp(value);
            //if (Entity.Properties[Prop.FOOD] == 10)
            //{
            //    Scripts.Get<IDamagable>().Health -= 20;
            //}
        }
        get { return Entity.Properties[Prop.FOOD]; }
    }

    public int MaxHealth { get; } = 5;

    public int foodCapacity { set; get; } = 200;

    enum BEHAVIOR { Collect, Follow }
    BEHAVIOR behavior = BEHAVIOR.Collect;

    private ksIServerEntity foodTarget;
    private List<ksIServerEntity> foodInRange = new List<ksIServerEntity> { };
    private int foodSearchDelay;

    private ksRigidBody2DView rb;

    // Called when the script is attached.
    public override void Initialize()
    {
        Room.OnUpdate[0] += Update;
        Scripts.Get<UnitServer>().Health = MaxHealth;
        rb = Entity.Scripts.Get<ksRigidBody2DView>();
        Entity.OnOverlapStart += OnOverlap;
        Entity.OnOverlapEnd += OnOverlapExit;
        Entity.OnDestroy += OnDestroy;
        food = 0;
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
        //Room.CallRPC(new List<ksIServerPlayer> { Room.GetPlayer(Entity.Properties[Prop.CONTROLLEDPLAYERID].UInt) }, RPC.ENDGAME, false);
        //Entity.Destroy();
        if (EntityToFollow != null)
        {
            // Stay at a distance from player
            if (ServerUtils.DistanceTo(Entity, EntityToFollow) > 3f)
            {
                moveTowardsPoint(EntityToFollow.Position2D);
            }
            else if (foodInRange.Count >= 1 && food < foodCapacity)
            {
                foodTarget = FindClosestFood();
                behavior = BEHAVIOR.Collect;

            }
        }

        else if (behavior == BEHAVIOR.Collect)
        {
            // if have foodTarget, and it is in range of hivemind, follow
            if (!ServerUtils.IsTargetValid(foodTarget))
            {
                if (EntityToFollow != null)
                {
                    behavior = BEHAVIOR.Follow;
                }
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
        if (Entity.Properties[Prop.FOOD] < 1)
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
        foodInRange = ServerUtils.UpdateEntityList(foodInRange);
        return ServerUtils.FindClosestEntity(Entity, foodInRange);
    }

    public void ChangeFollow()
    {
        ksLog.Info("Here Follow");
    }

    public void DetermineState()
    {
        foodTarget = FindClosestFood();
    }

    // Notifies room when team's hivemind dies
    private void OnDestroy()
    {
        Room.Scripts.Get<ServerRoom>().OnEntityDestroyed(Entity);
    }
}