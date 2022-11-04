using System;
using System.Collections.Generic;
using System.Collections;
using KS.Reactor.Server;
using KS.Reactor;

public class ServerFood : ksServerEntityScript
{
    private int foodValue = 1;
    // Called when the script is attached.
    public override void Initialize()
    {
        Room.OnUpdate[0] += Update;
        Entity.OnOverlapStart += OnOverlap;
        Entity.OnCollision += OnCollision;

    }

    // Called when the script is detached.
    public override void Detached()
    {
        Room.OnUpdate[0] -= Update;
        Entity.OnOverlapStart -= OnOverlap;
        Entity.OnCollision -= OnCollision;

    }

    // Called during the update cycle
    private void Update()
    {
        
    }

    private void OnOverlap(ksCollider ours, ksCollider other)
    {
        IFoodPickup otherEntityFood = other.Entity.Scripts.Get<IFoodPickup>();

        if (otherEntityFood.food < otherEntityFood.foodCapacity)
        {
            otherEntityFood.food += foodValue;
            Entity.Destroy();
            ksLog.Info("OnOverlap");
        }

    }

    private void OnCollision(ksContact other)
    {
        ksLog.Info("Contacted");
    }
}