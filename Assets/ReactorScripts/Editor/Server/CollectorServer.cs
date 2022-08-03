using System;
using System.Collections.Generic;
using System.Collections;
using KS.Reactor.Server;
using KS.Reactor;

public class CollectorServer : ksServerEntityScript , IFoodPickup
{
    public int foodCapacity { get; } = 10;

    private float ACCELERATION;

    private ksRigidBody2DView rb;

    private float speed;

    private int _food;
    public int food
    {
        set
        {
            _food = value;
            Entity.Properties[Prop.FOOD] = value;
        }
        get { return _food; }
    }

    public ksIServerEntity EntityToFollow;

    public ksIServerEntity Hivemind;

    private List<ksIServerEntity> foodInRange = new List<ksIServerEntity> { };

    private bool attached = false;

    enum BEHAVIOUR { Idle, Collect, Return }
    BEHAVIOUR behaviour = BEHAVIOUR.Idle;
    private ksIServerEntity target;
    private ksVector2 attachedOffset;
    private int feedNum;

    public override void Initialize()
    {
        Room.OnUpdate[0] += Update;
        food = 0;
        // Get rigidBody. Will it be 2D or 3D?
        rb = Entity.Scripts.Get<ksRigidBody2DView>();

        Entity.OnOverlapStart += OnOverlap;



    }

    public override void Detached()
    {
        Room.OnUpdate[0] -= Update;
    }

    // Called during the update cycle
    private void Update()
    {
        if (behaviour == BEHAVIOUR.Collect)
        {
            if (target == null)
            {
                target = FindClosestFood();
            }
            else
            {
                moveTowardsPoint(target.Position2D);
            }
        }

        else if (behaviour == BEHAVIOUR.Return)
        {
            if (attached)
            {
                feedNum++;
                if (feedNum == 5)
                {
                    food -= 1;
                    // Give food to hivemind

                    feedNum = 0;

                }
                if (food == 0)
                {
                    Detach();
                }
                // For now I will set its position. Make it so it moves along with at the same speed
                Entity.Transform2D.Teleport(attachedOffset - Hivemind.Position2D);
            }
        }

        //No food recognized nearby and can't feed. Stay at a certain distance away from entity following
        else if (behaviour == BEHAVIOUR.Idle)
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
                behaviour = BEHAVIOUR.Collect;
                target = FindClosestFood();
            }
            //Seek food if detects food
        }
    }

    private void Detach()
    {
        behaviour = BEHAVIOUR.Idle;
        attached = false;
        attachedOffset = ksVector2.Zero;
        //rb.mass = 1;
    }

    private ksIServerEntity FindClosestFood()
    {
        float closestDistance = 999999f;
        ksIServerEntity closestTarget = null;
        foreach (ksIServerEntity f in foodInRange)
        {
            if (f == null)
            {
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
        return closestTarget;
    }

    private void OnOverlap(ksCollider ours, ksCollider other)
    {
        // Check to see if ours is the locator collider.
        if (ours.IsTrigger)
        {
            ksLog.Info("Found Food");
            foodInRange.Add(other.Entity);
        }


    }

    private void moveTowardsPoint(ksVector2 point)
    {
        ksVector2 direction = point - Entity.Position2D;
        rb.Velocity = ksVector2.MoveTowards(rb.Velocity, direction.Normalized() * speed, Time.Delta * ACCELERATION);
    }
}