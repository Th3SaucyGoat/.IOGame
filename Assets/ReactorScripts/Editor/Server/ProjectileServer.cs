using System;
using System.Collections.Generic;
using System.Collections;
using KS.Reactor.Server;
using KS.Reactor;

public class ProjectileServer : ksServerEntityScript
{
    private int damage = 1;
    private float speed = 5f;
    public uint TeamId;


    // Called when the script is attached.
    public override void Initialize()
    {
        Room.OnUpdate[0] += Update;
        Entity.OnOverlapStart += OnOverlapStart;
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
        else if (!Checks.CheckTeam(Entity, other.Entity))
        {
            IDamagable hitbox = other.Entity.Scripts.Get<IDamagable>();
            if (hitbox != null)
            {
                hitbox.Health -= damage;
            }
            Entity.Destroy();
        }
    }
}