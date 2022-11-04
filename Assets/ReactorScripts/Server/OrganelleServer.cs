using System;
using System.Collections.Generic;
using System.Collections;
using KS.Reactor.Server;
using KS.Reactor;
using Example;

public class OrganelleServer : ksServerEntityScript
{

    private Timer delayTimer;
    private uint playerToBeId;
    private Timer rofTimer;
    private bool canShoot = true;

    // Called when the script is attached.
    public override void Initialize()
    {
        Room.OnUpdate[0] += Update;
        Entity.OnOverlapStart += OnOverlap;
        delayTimer = new Timer(.8f, DelayTimerTimeout, false);
        rofTimer = new Timer(0.5f, ROFTimerTimeout, false);
        //Entity.OnCollision += OnCollision;
        //ksLog.Info("Attempt at getting collider: "+ Entity.Scripts.Get<ksSphereCollider>().ToString());
    }

    // Called when the script is detached.
    public override void Detached()
    {
        Room.OnUpdate[0] -= Update;
        Entity.OnOverlapStart -= OnOverlap;

    }

    // Called during the update cycle
    private void Update()
    {
        delayTimer.Tick(Time.Delta);
        rofTimer.Tick(Time.Delta);
    }

    private void OnOverlap(ksCollider ours, ksCollider other)
    {
        //other.Entity;
        if (other.Entity.Properties[Prop.CONTROLLEDPLAYERID] != "" && Entity.Properties[Prop.ORGANELLEOWNER] != other.Entity.Properties[Prop.CONTROLLEDPLAYERID])
        {
            ksLog.Info("Organelle Contacted: " + other.Entity.Properties[Prop.CONTROLLEDPLAYERID].ToString());
            playerToBeId = other.Entity.Properties[Prop.CONTROLLEDPLAYERID];
            Entity.Scripts.Get<ksSphereCollider>().IsEnabled = false;
            other.Entity.Scripts.Get<HivemindServer>().organellesControlling.Add(this);
            ksVector2 direction = other.Transform2D.Position - Entity.Transform2D.Position;
            //Entity.Scripts.Get<ksRigidBody2DView>().AddForce(direction.Normalized() * 50f);
            delayTimer.Start();
        }


        //}
        //if (other.Entity.PlayerController != null)
        //{

        //}

    }


    [ksRPC(RPC.ORGANELLEPOSITION)]
    private void UpdateOrganellePosition(ksIServerPlayer player, ksVector3 pos)
    {
        if (delayTimer.RemainingSeconds != 0)
            return;
        //ksLog.Info(pos.ToString());
        Entity.Transform2D.Position = new ksVector2(pos.X, pos.Y);
    }

    //private void OnCollision(ksCollision collison)
    //{
    //    collision.
    //}

    private void DelayTimerTimeout()
    {
        Entity.Properties[Prop.ORGANELLEOWNER] = playerToBeId;
        
        ksLog.Info("DelayTimerTimout ");
    }

    public void FireOrganelle(ksVector2 point, int teamId)
    {
        if (!canShoot) return;
        canShoot = false;
        rofTimer.Start();
        //ksVector2 EOGPos = Entity.Position2D + (direction.Normalized() * EOGLength);
        Entity.Transform2D.LookAt(point);
        ksLog.Info(point.ToString());
        ksIServerEntity bullet = Room.SpawnEntity("Projectile", Entity.Position2D, Entity.Rotation2D);
        bullet.Properties[Prop.TEAMID] = teamId;
    }

    private void ROFTimerTimeout()
    {
        canShoot = true;
    }
}