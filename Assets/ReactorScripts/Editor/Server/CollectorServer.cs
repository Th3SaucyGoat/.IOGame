using System;
using System.Collections.Generic;
using System.Collections;
using KS.Reactor.Server;
using KS.Reactor;

public class CollectorServer : ksServerEntityScript , IFoodPickup , IDamagable , IMovement , ICommandable
{
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

    public int MaxHealth { get; }
    public int foodCapacity { get; } = 5;

    public float Speed { set; get; } = 3;
    public float Acceleration { set; get; } = 10f;

    private int _food;
    public int food
    {
        set
        {
            ksRange range = new ksRange(0, foodCapacity);
            _food = (int) range.Clamp(value);

            if (food == foodCapacity)
            {
                if (Entity.PlayerController == null && EntityToFollow == Hivemind)
                {
                    Feed();
                }
                else
                {
                    behavior = BEHAVIOR.Idle;
                }
            }
            Entity.Properties[Prop.FOOD] = _food;

        }
        get { return _food; }
    }

    private ksRigidBody2DView rb;

    public ksIServerEntity EntityToFollow { set; get; }

    public ksIServerEntity Hivemind { set; get; }

    private IFoodPickup hivemindFood;

    private List<ksIServerEntity> foodInRange = new List<ksIServerEntity> { };

    private bool attached = false;

    enum BEHAVIOR { Idle, Collect, Return }
    BEHAVIOR behavior;
    private ksIServerEntity foodTarget;
    private ksVector2 attachedOffset;
    private int feedNum;

    public override void Initialize()
    {
        Health = 5;
    }

    public void DelayedStart()
    {
        food = 0;

        behavior = BEHAVIOR.Idle;
        EntityToFollow = Hivemind;
        hivemindFood = Hivemind.Scripts.Get<IFoodPickup>();
        // Get rigidBody. Will it be 2D or 3D?
        rb = Entity.Scripts.Get<ksRigidBody2DView>();

        SubscribeToEvents();
    }

    private void SubscribeToEvents()
    {
        Room.OnUpdate[0] += Update;

        Entity.OnOverlapStart += OnOverlap;
        Entity.OnOverlapEnd += OnOverlapExit;
        Entity.OnCollision += OnCollisionStart;
    }

    public override void Detached()
    {
        Room.OnUpdate[0] -= Update;
        Entity.OnOverlapStart -= OnOverlap;
        Entity.OnOverlapEnd -= OnOverlapExit;
        Entity.OnCollision -= OnCollisionStart;


    }

    // Called during the update cycle
    private void Update()
    {
        if (Entity.PlayerController != null)
        {
            return;
        }

            //ksLog.Info(behavior.ToString());
            if (behavior == BEHAVIOR.Collect)
        {
            //ksLog.Info(foodInRange.Count.ToString());
            if (!Checks.IsTargetValid(foodTarget))
            {
                if (foodInRange.Count == 0)
                {
                    behavior = BEHAVIOR.Idle;
                    return;
                }
                foodTarget = FindClosestFood();
            }
            else
            {
                moveTowardsPoint(foodTarget.Position2D);
            }
        }

        else if (behavior == BEHAVIOR.Return)
        {
            if (attached)
            {
                feedNum++;
                if (feedNum == 5)
                {
                    food -= 1;
                    // Give food to hivemind
                    hivemindFood.food += 1;
                    feedNum = 0;
                }
                // Move exactly alongside Hivemind.
                Entity.Transform2D.Teleport(Hivemind.Position2D - attachedOffset);

                if (food == 0)
                {
                    Detach();
                }
            }
            else
            {
                moveTowardsPoint(Hivemind.Position2D);
            }
        }

        //No food recognized nearby and can't feed. Stay at a certain distance away from entity following
        else if (behavior == BEHAVIOR.Idle)
        {
            ksVector2 pos = Entity.Position2D - EntityToFollow.Position2D;
            float distance = pos.Magnitude();
            // Stay at a distance from player
            if (distance > 1.5f)
            {
                moveTowardsPoint(EntityToFollow.Position2D);
            }
            else if (foodInRange.Count >= 1 && food < foodCapacity)
            {
                behavior = BEHAVIOR.Collect;
                foodTarget = FindClosestFood();
            }
            //Seek food if detects food
        }
    }

    private void Detach()
    {
        behavior = BEHAVIOR.Idle;
        attached = false;
        attachedOffset = ksVector2.Zero;
        //rb.mass = 1;
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

    private void OnOverlap(ksCollider ours, ksCollider other)
    {
        //ksLog.Info("Here ");
        // Check to see if ours is the locator collider.
        if (ours.IsTrigger)
        {
            foodInRange.Add(other.Entity);
            //ksLog.Info("Yes ");
        }
        // Just collided with the food. Determine next AI logic.
        else
        {
            foodInRange.Remove(other.Entity);

            // Find distance from entity to follow
            ksVector2 pos = Entity.Position2D - EntityToFollow.Position2D;
            var distance = pos.Magnitude();
            // Too far away, return to entity to follow
            if (behavior != BEHAVIOR.Return)
            {
                if (distance> 3.5 || foodInRange.Count == 0)
                {
                    behavior = BEHAVIOR.Idle;
                }
            }
        }
        //ksLog.Info("Found Food = " + foodInRange.Count.ToString());
    }

    // This function is used by the locator 
    private void OnOverlapExit(ksCollider ours, ksCollider other)
    {
        // Locator has a food exit.
        if (ours.IsTrigger)
        {

            foodInRange.Remove(other.Entity);
  
            if (foodInRange.Count == 0 && behavior != BEHAVIOR.Return)
            {
                behavior = BEHAVIOR.Idle;
            }
        }
    }

    private void Feed()
    {
        behavior = BEHAVIOR.Return;
    }

    private void OnCollisionStart(ksContact contact)
    {
        //ksLog.Info("Collision Started");
        if (contact.Collider1.Entity != Hivemind)
        {
            //ksLog.Info("Collector registered a collision event that was not the Hivemind");
        }
        // Handles attach logic when returning.
        if (behavior == BEHAVIOR.Return && contact.Collider1.Entity == Hivemind)
            
        {

            if (!attached)
            {
                attachedOffset = Hivemind.Position2D - Entity.Position2D;
                attached = true;
                //rb.mass = 0.1f;

            }
        }
    }

    private void moveTowardsPoint(ksVector2 point)
    {
        ksVector2 direction = point - Entity.Position2D;
        rb.Velocity = ksVector2.MoveTowards(rb.Velocity, direction.Normalized() * Speed, Time.Delta * Acceleration);
    }
    public void ChangeFollow()
    {
        ksLog.Info("Here Follow");
    }
}