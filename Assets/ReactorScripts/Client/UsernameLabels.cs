using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using KS.Reactor.Client.Unity;
using KS.Reactor.Client;
using KS.Reactor;
using TMPro;
using Unity;

public class UsernameLabels : ksRoomScript
{
    public UsernameLabel prefab;

    public static Dictionary<uint, UsernameLabel> currentLabels;  // Holds a reference to the labels active (one per player)
    public static Dictionary<uint, ksEntity> entityReference;       // Holds a reference to the entities that the labels are attached to

    private void Start()
    {
        currentLabels = new Dictionary<uint, UsernameLabel>();
        entityReference = new Dictionary<uint, ksEntity>();
    }

    //  public static void SetEntity(UsernameLabel label, Transform entityTransform)
    //  {
    //      entityTransform.GetComponent<Unit>().usernameLabel = label;

    //     SetLabelOffset(label.transform, entityTransform);
    //labelTransform.SetParent(entityTransform);
    //  }

    public static void SetEntity(uint pId, ksEntity entity)
    {

        UsernameLabel labelForThisPlayer = UsernameLabels.currentLabels[pId];
        // Remove the label reference from the previous entity, if there is one.
        if (entityReference.ContainsKey(pId))
        {
            entityReference[pId].GameObject.GetComponent<Unit>().usernameLabel = null;
        }
        // Add reference to new entity
        entity.GameObject.GetComponent<Unit>().usernameLabel = labelForThisPlayer;
        entityReference[pId] = entity;
    }

    public UsernameLabel CreateUserLabel(uint Id, string Username)
    {
        UsernameLabel label = (UsernameLabel) Instantiate(prefab);
        currentLabels.Add(Id, label);
        label.gameObject.GetComponent<TextMeshPro>().text = Username;
        label.gameObject.GetComponent<UsernameLabel>().Offset = new Vector3(0f, -1.0f, 0f);
        return label;
    }
}