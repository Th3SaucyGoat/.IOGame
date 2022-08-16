using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using KS.Reactor.Client.Unity;
using KS.Reactor.Client;
using KS.Reactor;
using Cinemachine;

public class CollectorClient : ksEntityScript
{
    // This likely needs to be changed in the future, use a new property or rpc to send 
    // info about stats of the unit.
    private int foodCapacity = 5;

    //private Color startingColor = new Color(0, 1, 1);
    //private Color fullColor = new Color(0, 0.3f, 1);

    private SpriteRenderer sprite;
    private Unit unitScript;

    private ksEntity hivemind;
    private bool feeding = false;

    public override void Initialize()
    {
        Entity.OnPropertyChange[Prop.FOOD] += OnFoodChanged;
        sprite = GetComponent<SpriteRenderer>();
        unitScript = GetComponent<Unit>();
        hivemind = Room.GetEntity(Entity.Properties[Prop.HIVEMINDID]);
    }

    // Called when the script is detached.
    public override void Detached()
    {
        
    }

    // Called every frame.
    private void Update()
    {
        if (Entity.PlayerController == null)
        {
            return;
        }
        if (ClientUtils.DistanceTo(hivemind, Entity) < 1.5f)
        {
            if (!HUD.feedLabelShown && Entity.Properties[Prop.FOOD] > 0 && !feeding)
            {
                GameEvents.current.InRangeToFeed(true);
            }
        }
        else if (HUD.feedLabelShown)
        {
            GameEvents.current.InRangeToFeed(false);
        }
        if (Input.GetKeyDown(KeyCode.Space) && HUD.feedLabelShown)
        {
            Entity.CallRPC(RPC.RETURN);
            GameEvents.current.InRangeToFeed(false);
            feeding = true;
         }
    }

    private void OnFoodChanged(ksMultiType oldV, ksMultiType newV)
    {
        if (newV == foodCapacity)
        {
            sprite.color = unitScript.primaryColor;
        }
        else if (newV < foodCapacity)
        {
            sprite.color = unitScript.secondaryColor;
        }
        if (newV ==0)
        {
            feeding = false;
        }
    }
}