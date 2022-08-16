using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using KS.Reactor.Client.Unity;
using KS.Reactor.Client;
using KS.Reactor;
using Cinemachine;

public class Unit : ksEntityScript
{
    [HideInInspector]
    public UsernameLabel usernameLabel;

    private SpriteRenderer sprite;
    public Color primaryColor;
    public Color secondaryColor;


    private void Start()
    {
        Entity.OnPropertyChange[Prop.HEALTH] += OnHealthChanged;
        sprite = GetComponent<SpriteRenderer>();
        print("Sprite Found " + sprite);
        IdentifyTeam(Entity.Properties[Prop.TEAMID].UInt);
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
        // Figure out if this pId is the local player's
        if (Room.LocalPlayerId == pId)
        {
            // Hook up camera
            GameEvents.current.ChangeCamera(transform);
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
        print(Entity.GameObject.name);
        if (Entity.GameObject.name == "Collector")
        {
            sprite.color = secondaryColor;
        }
    }
    private void OnHealthChanged(ksMultiType oldV, ksMultiType newV)
    {
        sprite.material.color = Color.red;
        Invoke(nameof(EndFlash), 0.2f);
    }

    private void EndFlash()
    {
        sprite.material.color = primaryColor;
    }

}