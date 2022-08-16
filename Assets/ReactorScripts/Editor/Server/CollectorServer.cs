using System;
using System.Collections.Generic;
using System.Collections;
using KS.Reactor.Server;
using KS.Reactor;

public class CollectorServer : ksServerEntityScript , IFoodPickup , IMovement , ICommandable
{
    public int MaxHealth { get; } = 1;
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
    BEHAVIOR behavior = BEHAVIOR.Collect;
    private ksIServerEntity foodTarget;
    private ksVector2 attachedOffset;
    private int feedNum;

    public override void Initialize()
    {
        Scripts.Get<UnitServer>().Health = MaxHealth;
        food = 0;
        rb = Entity.Scripts.Get<ksRigidBody2DView>();
        SubscribeToEvents();
    }

    public void DelayedStart()
    {
        // This is things that need to be set after the Initialize function,
        // since the initialize function happens immediately on instantiation.
        EntityToFollow = Hivemind;
        hivemindFood = Hivemind.Scripts.Get<IFoodPickup>();
        Entity.CallRPC(RPC.SENDINFO, new ksMultiType[] { Entity.Properties[Prop.TEAMID] });
    }

    private void SubscribeToEvents()
    {
        Room.OnUpdate[0] += Update;

        Entity.OnOverlapStart += OnOverlap;
        Entity.OnOverlapEnd += OnOverlapExit;
        Entity.OnCollision += OnCollisionStart;
        Entity.OnContactUpdate += OnCollisionUpdate;
    }

    public override void Detached()
    {
        Room.OnUpdate[0] -= Update;
        Entity.OnOverlapStart -= OnOverlap;
        Entity.OnOverlapEnd -= OnOverlapExit;
        Entity.OnCollision -= OnCollisionStart;
        Entity.OnContactUpdate -= OnCollisionUpdate;



    }

    // Called during the update cycle
    private void Update()
    {
        //ksLog.Info(behavior.ToString() + " " + foodInRange.Count.ToString());
        if (Entity.PlayerController != null)
        {
            return;
        }

            //ksLog.Info(behavior.ToString());
            if (behavior == BEHAVIOR.Collect)
        {
            //ksLog.Info(foodInRange.Count.ToString());
            if (!ServerUtils.IsTargetValid(foodTarget))
            {
                //ksLog.Info("Here = " + foodInRange.Count.ToString() + !Checks.IsTargetValid(foodTarget));
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
                if (feedNum >= 5)
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
            if (!ServerUtils.IsTargetValid(EntityToFollow))
            {
                EntityToFollow = Hivemind;
                return;
            }
            // Stay at a distance from player
            if (ServerUtils.DistanceTo(Entity, EntityToFollow) > 1.5f)
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
        // Need unique logic for if a player is controlling.
        if (Entity.Properties[Prop.CONTROLLEDPLAYERID] != "")
        {

            Entity.SetController(
                new UnitController(Speed, Acceleration),
                Room.GetPlayer(Entity.Properties[Prop.CONTROLLEDPLAYERID].UInt)
                );
        }
    }

    private ksIServerEntity FindClosestFood()
    {
        foodInRange = ServerUtils.UpdateEntityList(foodInRange);

        return ServerUtils.FindClosestEntity(Entity, foodInRange);
    }

    private void OnOverlap(ksCollider ours, ksCollider other)
    {
        //ksLog.Info("Here ");
        // Check to see if ours is the locator collider.
        if (ours.IsTrigger)
        {
            foodInRange.Add(other.Entity);
            //ksLog.Info("adding = "+ foodInRange.Count.ToString());
            //ksLog.Info("Yes ");
        }
        // Just collided with the food. Determine next AI logic.
        else
        {
            foodInRange.Remove(other.Entity);

            // Find distance from entity to follow
            float distance = ServerUtils.DistanceTo(Entity, EntityToFollow);
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
            //ksLog.Info("removing = " + foodInRange.Count.ToString());
  
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
        
        ksLog.Info("Collision Started" + (contact.Collider1.Entity == Hivemind).ToString() + "   "+ attached.ToString());
        if (contact.Collider1.Entity != Hivemind)
        {
            ksLog.Info("Collector registered a collision event that was not the Hivemind");
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

    private void OnCollisionUpdate(ksContact contact)
    {
        ksLog.Info("Collision Updated");
        if (contact.Collider1.Entity != Hivemind)
        {
            ksLog.Info("Collector registered a collision event that was not the Hivemind");
        }
        // Handles attach logic when returning.
        if (!attached)
        {

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

    }

    private void moveTowardsPoint(ksVector2 point)
    {
        ksVector2 direction = point - Entity.Position2D;
        rb.Velocity = ksVector2.MoveTowards(rb.Velocity, direction.Normalized() * Speed, Time.Delta * Acceleration);
    }
    public void DetermineState()
    {
        if (food == foodCapacity)
        {
            behavior = BEHAVIOR.Return;
        }
        else if (ServerUtils.DistanceTo(Entity, EntityToFollow) > 2)
        {
            behavior = BEHAVIOR.Idle;
        }
        else if ( food < foodCapacity && foodInRange.Count >0)
        {
            behavior = BEHAVIOR.Collect;
        }
    }

    [ksRPC(RPC.RETURN)]
    private void PlayerInitiateReturn()
    {
        behavior = BEHAVIOR.Return;
        // Need to disable the player controller in order for AI behaviour to take over.
        ksLog.Info("Feed initiated");
        Entity.RemoveController();
    }
}