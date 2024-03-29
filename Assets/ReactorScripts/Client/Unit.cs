using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using KS.Reactor.Client.Unity;
using KS.Reactor.Client;
using KS.Reactor;
using Cinemachine;
using UnityEngine.Assertions;

public class Unit : ksEntityScript
{
    [HideInInspector]
    public UsernameLabel usernameLabel;

    private SpriteRenderer sprite;
    [HideInInspector]
    public Color primaryColor = new Color();
    [HideInInspector]
    public Color secondaryColor = new Color();
    [HideInInspector]
    public Color previousColor = new();


    private void Start()
    {
        Entity.OnPropertyChange[Prop.HEALTH] += OnHealthChanged;
        sprite = GetComponent<SpriteRenderer>();
        Assert.AreNotEqual("", Entity.Properties[Prop.TEAMID].String);
        //IdentifyTeam(Entity.Properties[Prop.TEAMID].UInt);

    }

    private void Update()
    {
        if (usernameLabel != null)
        {
            usernameLabel.Position = transform.position;
        }

    }

    [ksRPC(RPC.TAKECONTROL)]
    private void TakeControl(uint pId)
    {
        print(Entity.GameObject.name);
        // Figure out if this pId is the local player's
        if (Room.LocalPlayerId == pId)
        {
            // Hook up camera
            GameEvents.current.ChangeCamera(transform);

            // Emitted for UI changes.
            GameEvents.current.UnitTakenControl(Entity.GameObject.name);
        }
        // Display the username of the player underneath this entity
        UsernameLabels.SetEntity(pId, Entity);
    }

    private void IdentifyTeam(uint teamId)
    {
        // Because this function is called before the entity's start function. 
        if (sprite == null)
        {
            sprite = GetComponent<SpriteRenderer>();
        }

        Color[] colors = TeamColors.DetermineTeamColor(teamId);
        primaryColor = colors[0];
        secondaryColor = colors[1];
        sprite.color = primaryColor;
        if (Entity.GameObject.name == "Collector")
        {
            sprite.color = secondaryColor;
        }
    }
    private void OnHealthChanged(ksMultiType oldV, ksMultiType newV)
    {
        if (sprite.color == Color.white)
        {
            return;
        }
        previousColor = sprite.color;
        sprite.color = Color.white;
        Invoke(nameof(EndFlash), 0.12f);
    }

    private void EndFlash()
    {
        sprite.color = previousColor;
    }

}