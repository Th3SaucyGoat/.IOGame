using System;
using System.Collections.Generic;
using System.Collections;
using KS.Reactor.Server;
using KS.Reactor;
using Example;

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
        }
        get { return Entity.Properties[Prop.FOOD]; }
    }


    public int foodCapacity { set; get; } = 200;

    enum BEHAVIOR { Collect, Follow }
    BEHAVIOR behavior = BEHAVIOR.Collect;

    private ksIServerEntity foodTarget;
    private List<ksIServerEntity> foodInRange = new List<ksIServerEntity> { };
    private int foodSearchDelay;

    private Timer debugTimer;

    private ksRigidBody2DView rb;

    // Called when the script is attached.
    public override void Initialize()
    {
        Room.OnUpdate[0] += Update;
        Scripts.Get<UnitServer>().Health = Stats.HivemindMaxHealth;
        rb = Entity.Scripts.Get<ksRigidBody2DView>();
        Entity.OnOverlapStart += OnOverlap;
        Entity.OnOverlapEnd += OnOverlapExit;
        Entity.OnDestroy += OnDestroy;
        food = 0;
        debugTimer = new Timer(2.0f, OnDebugTimeout, false);
        //debugTimer.Start();
    }

    public override void Detached()
    {
        Room.OnUpdate[0] -= Update;
        Entity.OnOverlapStart -= OnOverlap;
        Entity.OnOverlapEnd -= OnOverlapExit;

    }

    private void Update()
    {
        debugTimer.Tick(Time.Delta);
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
        //if (Entity.Properties[Prop.FOOD] < Stats.GetType().GetField(type))
        //{
        //    return;
        //}
        switch (type)
        {
            case "Collector":
                if (Entity.Properties[Prop.FOOD] < Stats.CollectorCost)
                {
                    return;
                }
                ksIServerEntity collector = Room.SpawnEntity("Collector", Entity.Position2D);
                InitializeEntity(collector);
                Entity.Properties[Prop.FOOD] -= Stats.CollectorCost;
                break;
            case "Shooter":
                if (Entity.Properties[Prop.FOOD] < Stats.ShooterCost)
                {
                    return;
                }
                ksIServerEntity shooter = Room.SpawnEntity("Shooter", Entity.Position2D);
                InitializeEntity(shooter);
                Entity.Properties[Prop.FOOD] -= Stats.ShooterCost;
                break;
        }
        // Find the player's hivemind. Retrieve the position of the hivemind.

        // The collector needs to know it is of that unique team/player.

        //ksLog.Info("IS ASSIGNED? = "+ hive.Scripts.Get<CollectorServer>().Hivemind.ToString());
    }

    private void InitializeEntity(ksIServerEntity entity)
    {
        entity.Properties[Prop.TEAMID]= Entity.Properties[Prop.TEAMID];
        entity.Properties[Prop.HIVEMINDID] = Entity.Id;
        ICommandable command = entity.Scripts.Get<ICommandable>();
        command.Hivemind = Entity;
        ISpawnable delay = entity.Scripts.Get<ISpawnable>();
        delay.DelayedStart();
    }

    private void OnOverlap(ksCollider ours, ksCollider other)
    {
        if (ours.IsTrigger)
        {
            foodInRange.Add(other.Entity);
        }
    }
    private void OnDebugTimeout()
    {
        Scripts.Get<UnitServer>().Health = 0;
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