using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using KS.Reactor.Client.Unity;
using KS.Reactor.Client;
using KS.Reactor;

public class OrganelleClient : ksEntityScript
{
    [SerializeField]
    private Transform softBodyPrefab;
    private Transform softBody;
    private List<Transform> bones = new();
    private bool controlledByLocalPlayer;

    // Called after properties are initialized.
    public override void Initialize()
    {
        softBody = Instantiate(softBodyPrefab, transform.position, Quaternion.identity);
        foreach (Transform trans in softBody.transform)
        {
            print(trans.gameObject.name);
            if (trans.gameObject.name.Contains("bone"))
            {
                bones.Add(trans);
                
            }
        }
        //print("Bones: " + bones.Count + "Children: " + softBody.childCount);
        Entity.OnPropertyChange[Prop.ORGANELLEOWNER] += OrganelleCaptured;
        //print("Local Player: " + Room.LocalPlayerId);
    }

    // Called when the script is detached.
    public override void Detached()
    {
        Entity.OnPropertyChange[Prop.ORGANELLEOWNER] -= OrganelleCaptured;

    }

    // Called every frame.
    private void Update()
    {
        //return;
        if (controlledByLocalPlayer)
        {
            //print("Local: " + softBody.transform.localPosition + "Global: " + softBody.transform.position);
            Vector3 avgPosition = Vector3.zero;
            foreach (Transform t in bones)
            {
                avgPosition += t.position;
                if (!t.gameObject.name.Contains("bone"))
                    print(t.gameObject.name.Contains("bone"));
            }
            // Account for boundary
            avgPosition = Vector3.Scale(avgPosition, new Vector3((1f / bones.Count), (1f / bones.Count), 0f));
            //avgPosition.ToArray() / bones.Length;
            //print("Avg: " + avgPosition + "Previous: " + transform.position);
            Entity.CallRPC(RPC.ORGANELLEPOSITION, avgPosition);
            
        }
    }


    private void OrganelleCaptured(ksMultiType oldValue, ksMultiType newValue)
    {
        if (newValue == "")
        {
            // Organelle player set to nothing
            print("Organelle Layer Changed. Not Controlled");
            LayerMask organelleBoundaryLayer = LayerMask.NameToLayer("Organelle");
            //softBody.Find("OrganelleBoundary").gameObject.layer = organelleBoundaryLayer;

            softBody.gameObject.layer = organelleBoundaryLayer;
            foreach (Transform child in transform)
            {
                child.gameObject.layer = organelleBoundaryLayer;
            }

        }
        else
        {
            print("Organelle Layer Changed. Player Controlled");
            LayerMask organelleBoundaryLayer = LayerMask.NameToLayer("CapturedOrganelle");
            //softBody.Find("OrganelleBoundary").gameObject.layer = organelleBoundaryLayer;
            softBody.gameObject.layer = organelleBoundaryLayer;
            foreach (Transform bone in bones)
            {
                bone.gameObject.layer = organelleBoundaryLayer;
            }
            if (Room.LocalPlayerId == Entity.Properties[Prop.ORGANELLEOWNER].UInt)
            {
                controlledByLocalPlayer = true;
            }
        }
    }
}