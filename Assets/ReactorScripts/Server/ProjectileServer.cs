using System;
using System.Collections.Generic;
using System.Collections;
using KS.Reactor.Server;
using KS.Reactor;
using Example;

public class ProjectileServer : ksServerEntityScript
{
    private int damage = 1;
    private float speed = 12f;
    public uint TeamId;

    private Timer LiftetimeTimer;
    // Called when the script is attached.
    public override void Initialize()
    {
        Room.OnUpdate[0] += Update;
        Entity.OnOverlapStart += OnOverlapStart;
        LiftetimeTimer = new Timer(1.0f, Timeout, false);
        LiftetimeTimer.Start();
    }

    // Called when the script is detached.
    public override void Detached()
    {
        Room.OnUpdate[0] -= Update;
        Entity.OnOverlapStart -= OnOverlapStart;
    }

    // Called during the update cycle
    private void Update()
    {
        LiftetimeTimer.Tick(Time.Delta);
        Entity.Transform2D.Move(speed * Transform2D.Forward() * Time.Delta);
    }

    private void OnOverlapStart(ksCollider ours, ksCollider other)
    {
        // Check if is Obstacle
        if ((other.Entity.CollisionFilter.AssetName == "Obstacles"))
        {
            Entity.Destroy();
        }
        // Check if is Enemy. If not, don't destroy.
        else if (!ServerUtils.CheckTeam(Entity, other.Entity))
        {
            IDamagable hitbox = other.Entity.Scripts.Get<IDamagable>();
            if (hitbox != null)
            {
                ksLog.Info("applying damage");
                hitbox.Health -= damage;
            }
            Entity.Destroy();
        }
    }

    private void Timeout()
    {
        Entity.Destroy();
    }
}