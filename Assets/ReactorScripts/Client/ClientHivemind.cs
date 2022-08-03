using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using KS.Reactor.Client.Unity;
using KS.Reactor.Client;
using KS.Reactor;
using Cinemachine;

public class ClientHivemind : ksEntityScript
{
    public UsernameLabel prefab;
    public Dictionary<string, ksMultiType> player;
    // Called after properties are initialized.

    private void Awake()
    {

    }
    public override void Initialize()
    {

        player = new Dictionary<string, ksMultiType>();
        if (Entity.PlayerController != null)
        {
            CinemachineVirtualCamera cine = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<CinemachineVirtualCamera>();
            cine.Follow = transform;
            cine.LookAt = transform;
            GameEvents.current.StartMatch?.Invoke();
            Entity.OnPropertyChange[Prop.FOOD] += FoodChanged;

            // Make username label a child of this entity
            //Entity.OnPropertyChange[]

        }
        else
        {
            
        }
    }

    private void Start()
    {

    }

    // Called when the script is detached.
    public override void Detached()
    {
        
    }

    // Called every frame.
    private void Update()
    {


        if (Input.GetKeyDown("1"))
        {
            if (Entity.Properties[Prop.FOOD] >= 1)
            {
                Entity.CallRPC(RPC.SPAWNCOLLECTOR);
            }
        }
        

    }

    [ksRPC(RPC.SENDINFO)]
    public void RegisterInfo( ksMultiType[] a)
    {
        player["Username"] = a[0];
        player["Id"] = a[1];
        CreateUserLabel();
    }

    private void CreateUserLabel()
    {
        print("Received RPC, creating label");
        UsernameLabel.Instance = Instantiate(prefab);
        UsernameLabel.Text = player["Username"];
        UsernameLabel.Instance.transform.SetParent(transform);
        UsernameLabel.Instance.transform.position = new Vector3(transform.position.x, transform.position.y - 2, 0f);
    }

    private void FoodChanged(ksMultiType old, ksMultiType newV)
    {
        GameEvents.current.FoodChanged(newV);
    }

}