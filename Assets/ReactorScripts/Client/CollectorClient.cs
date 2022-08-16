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

    public override void Initialize()
    {
        Entity.OnPropertyChange[Prop.FOOD] += OnFoodChanged;
        sprite = GetComponent<SpriteRenderer>();
        unitScript = GetComponent<Unit>();
        
    }

    // Called when the script is detached.
    public override void Detached()
    {
        
    }

    // Called every frame.
    private void Update()
    {
        
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
    }
}