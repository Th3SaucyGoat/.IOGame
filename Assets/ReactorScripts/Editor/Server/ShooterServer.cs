using System;
using System.Collections.Generic;
using System.Collections;
using KS.Reactor.Server;
using KS.Reactor;
using Example;

public class ShooterServer : ksServerEntityScript , IDamagable , IMovement , ICommandable 
{
    public float Speed { set; get; } = 2f;
    public float Acceleration { set; get; } = 4f;

    private ksRigidBody2DView rb;

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

    public ksIServerEntity Hivemind { set; get; }

    private ksIServerEntity _EntityToFollow;
    public ksIServerEntity EntityToFollow {
        set
        {
            _EntityToFollow = value;
            ksLog.Info("Went through Setter");
        }
        get { return _EntityToFollow; }
    }

    private ksIServerEntity target;

    private Timer ROFTimer;

    private bool playerFiring = false;

    List<ksIServerEntity> enemiesInRange = new List<ksIServerEntity> { };
    private ksVector2 direction;

    private float EOGLength;

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
        Health = 10;
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
            
            ksVector2 pos = Entity.Position2D - EntityToFollow.Position2D;
            float distance = pos.Magnitude();
           
            if (num == 50)
            {
                ksLog.Info(EntityToFollow.Type + " and " + distance.ToString());
                num = 0;
            }
            // Head back if too far away
            if (distance > 2.0f)
            {
                MoveTowardsPoint(EntityToFollow.Position2D);
            }
            // Ok to pursue a little bit if not too far away and there's an enemy
            else if (Checks.IsTargetValid(target))
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
            if (!Checks.IsTargetValid(target))
            {
                //ksLog.Info("Not Valid");
                //Start Timer. At the end of timeout, put back into protect mode. Or can have him idle/pace back and forth in place
                if (enemiesInRange.Count == 0)
                {

                }
                else
                {
                    target = FindClosesetEnemy();
                }
            }
            // Head towards target if far away. But don't get too close.
            else
            {
                //ksLog.Info("Valid");
                //ksLog.Info(target.Position2D);
                ksVector2 pos = Entity.Position2D - target.Position2D;
                float distance = pos.Magnitude();
                if (distance > 2.0)
                {
                    MoveTowardsPoint(target.Position2D);
                }
                else
                {

                }
            }
        }
        if (!Checks.IsTargetValid(target))
        {
            target = FindClosesetEnemy();
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


    private ksIServerEntity FindClosesetEnemy()
    {
        float closestDistance = 999999f;
        ksIServerEntity closestTarget = null;
        var enemiesToRemove = new List<ksIServerEntity> { };

        foreach (ksIServerEntity enemy in enemiesInRange)
        {
            if (!Checks.IsTargetValid(enemy))
            {
                enemiesToRemove.Add(enemy);
                //ksLog.Info("Enemy IN RANGE of Shooter Isn't Valid");
                continue;
            }
            ksVector2 position = Entity.Position2D - enemy.Position2D;
            float distance = position.Magnitude();
            if (distance < closestDistance)
            {
                closestTarget = enemy;
                closestDistance = distance;
            }
        }
        foreach (ksIServerEntity e in enemiesToRemove)
        {
            enemiesInRange.Remove(e);
        }
        //ksLog.Info("Target found = " + closestTarget.ToString()) ;
        return closestTarget;
    }

    private void OnOverlapStart(ksCollider ours, ksCollider other)
    {
        //ksLog.Info("Here ");

        // Check if locator. Add to enemies in range. Need to figure out way to isolate enemies from allies though.
        if (ours.IsTrigger)
        {
            if (Entity.Properties[Prop.TEAMID].Int != other.Entity.Properties[Prop.TEAMID].Int)
            {
                enemiesInRange.Add(other.Entity);
                if (enemiesInRange.Count == 1)
                {
                    target = other.Entity;
                }
            }
            //ksLog.Info("Triggered");
        }
    }
    private void OnOverlapEnd(ksCollider ours, ksCollider other)
    {
        //var distance = (other.Entity.Position2D - Entity.Position2D).Magnitude();
        //if (other.Entity == target && distance < 5.0)
        //{
        //    ksLog.Info("The Collider OnOverlapEnd picked up destroying something!");

        //}
        //else
        //{
        //}
        if (ours.IsTrigger)
        {
            if (!Checks.CheckTeam(Entity, other.Entity))
            {
                enemiesInRange.Remove(other.Entity);
                target = FindClosesetEnemy();
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

    public void ChangeFollow()
    {
        ksLog.Info("Here Follow" + EntityToFollow.Type);
        
    }
}