using System;
using System.Collections.Generic;
using System.Collections;
using KS.Reactor.Server;
using KS.Reactor;
using Example;

public class ShooterServer : ksServerEntityScript , IMovement , ICommandable 
{
    public float Speed { set; get; } = 2f;
    public float Acceleration { set; get; } = 4f;

    private ksRigidBody2DView rb;

    public int MaxHealth { get; } = 1;



    public ksIServerEntity Hivemind { set; get; }

    private ksIServerEntity _EntityToFollow;
    public ksIServerEntity EntityToFollow {
        set
        {
            _EntityToFollow = value;
        }
        get { return _EntityToFollow; }
    }

    private ksIServerEntity target;

    private Timer ROFTimer;

    private bool playerFiring = false;

    List<ksIServerEntity> enemiesInRange = new List<ksIServerEntity> { };
    private ksVector2 direction;

    private float EOGLength;

    private Timer DebugTimer;

    public enum BEHAVIOUR { Protect, Chase }
    public BEHAVIOUR behaviour = BEHAVIOUR.Protect;

    private bool canShoot = true;
    private int num;

    // Called when the script is attached.
    public override void Initialize()
    {
        Room.OnUpdate[0] += Update;
        Entity.OnOverlapStart += OnOverlapStart;
        Entity.OnOverlapEnd += OnOverlapEnd;
        ROFTimer = new Timer(0.8f, OnROFTimerTimeout, false);
        rb = Entity.Scripts.Get<ksRigidBody2DView>();
        Scripts.Get<UnitServer>().Health = MaxHealth;
        DebugTimer = new Timer(2f, DebugDestroy, false);
        DebugTimer.Start();
    }

    public void DelayedStart()
    {

        EntityToFollow = Hivemind;
    }

    // Called when the script is detached.
    public override void Detached()
    {
        Room.OnUpdate[0] -= Update;
        Entity.OnOverlapStart -= OnOverlapStart;
        Entity.OnOverlapEnd -= OnOverlapEnd;
    }

    // Called during the update cycle
    private void Update()
    {
        DebugTimer.Tick(Time.Delta);
        ROFTimer.Tick(Time.Delta);
        if (Entity.PlayerController != null)
        {
            if (canShoot && playerFiring == true)
            {
                Shoot();
            }

            return;
        }
        //ksLog.Info(target.ToString() + "  " + target.IsDestroyed.ToString() + "  " + (target == null).ToString());
        //ksLog.Info(behaviour.ToString());
        if (behaviour == BEHAVIOUR.Protect)
        {
            if (num == 50)
            {
               // ksLog.Info(EntityToFollow.Type + " and " + distance.ToString());
                num = 0;
            }
            // Head back if too far away
            if (ServerUtils.DistanceTo(Entity, EntityToFollow) > 2.0f)
            {
                MoveTowardsPoint(EntityToFollow.Position2D);
            }
            // Ok to pursue a little bit if not too far away and there's an enemy
            else if (ServerUtils.IsTargetValid(target))
            {
                MoveTowardsPoint(target.Position2D);
            }
            else
            {

            }
        }
        //Chase State where chases after enemies
        else if (behaviour == BEHAVIOUR.Chase)
        {
            if (!ServerUtils.IsTargetValid(target))
            {
                //ksLog.Info("Not Valid");
                //Start Timer. At the end of timeout, put back into protect mode. Or can have him idle/pace back and forth in place
                if (enemiesInRange.Count == 0)
                {

                }
                else
                {
                    target = FindClosestEnemy();
                }
            }
            // Head towards target if far away. But don't get too close.
            else
            {
                //ksLog.Info("Valid");
                //ksLog.Info(target.Position2D);
                if (ServerUtils.DistanceTo(Entity, EntityToFollow) > 2.0)
                {
                    MoveTowardsPoint(target.Position2D);
                }
                else
                {

                }
            }
        }
        if (!ServerUtils.IsTargetValid(target))
        {
            target = FindClosestEnemy();
        }
        else
        {
            // Turn the Entity, or its turret, to face the target
            LookAtPoint(target.Position2D);
            if (canShoot)
            {
                Shoot();
            }
        }
        num++;
    }

    private void MoveTowardsPoint(ksVector2 point)
    {
        ksVector2 direction = point - Entity.Position2D;
        rb.Velocity = ksVector2.MoveTowards(rb.Velocity, direction.Normalized() * Speed, Time.Delta * Acceleration);
    }
    private void LookAtPoint(ksVector2 point)
    {
        Entity.Transform2D.LookAt(point); 
    }

    private ksIServerEntity FindClosestEnemy()
    {
        enemiesInRange = ServerUtils.UpdateEntityList(enemiesInRange);
        return ServerUtils.FindClosestEntity(Entity, enemiesInRange);
    }

    private void OnOverlapStart(ksCollider ours, ksCollider other)
    {
        // Check if locator. Add to enemies in range. Need to figure out way to isolate enemies from allies though.
        if (ours.IsTrigger)
        {
            if (!ServerUtils.CheckTeam(ours.Entity, other.Entity))
            {
                enemiesInRange.Add(other.Entity);
                if (enemiesInRange.Count == 1)
                {
                    target = other.Entity;
                }
                Room.Scripts.Get<ServerRoom>();
            }
        }
    }
    private void OnOverlapEnd(ksCollider ours, ksCollider other)
    {
        if (ours.IsTrigger)
        {
            if (!ServerUtils.CheckTeam(Entity, other.Entity))
            {
                enemiesInRange.Remove(other.Entity);
                target = FindClosestEnemy();
            }
        }
    }

    private void Shoot()
    {
        canShoot = false;
        ROFTimer.Start();
        ksVector2 EOGPos = Entity.Position2D + (direction.Normalized() * EOGLength);
        ksIServerEntity bullet = Room.SpawnEntity("Projectile", EOGPos, Entity.Rotation2D);
        bullet.Properties[Prop.TEAMID] = Entity.Properties[Prop.TEAMID];
    }

    private void OnROFTimerTimeout() { 
        canShoot = true;
    }
    [ksRPC(RPC.FIRING)]
    private void ChangeFiringStatus(ksIServerPlayer player, bool isFiring)
    {
        ksLog.Info("Received " + num.ToString());
        playerFiring = isFiring;
        num++;
    }

    [ksRPC(RPC.RELAYMOUSEINFO)]
    private void Rotate(ksIServerPlayer player, ksMultiType point)
    {
        LookAtPoint(point);
    }

    private void DebugDestroy()
    {
        ksLog.Info("Debug Destroyed");
        Scripts.Get<IDamagable>().Health -= 10;
    }

    public void DetermineState()
    {
        target = FindClosestEnemy();
    }
}