using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using KS.Reactor.Client.Unity;
using KS.Reactor.Client;
using KS.Reactor;
using TMPro;

public class UsernameLabels : ksRoomScript
{
    public UsernameLabel prefab;

    public static Dictionary<uint, UsernameLabel> currentLabels;

    private void Start()
    {
        currentLabels = new Dictionary<uint, UsernameLabel>();
    }

    public static void SetEntity(UsernameLabel label, Transform entityTransform)
    {

        entityTransform.GetComponent<Unit>().usernameLabel = label;

        SetLabelOffset(label.transform, entityTransform);
        //labelTransform.SetParent(entityTransform);
    }

    public static void SetLabelOffset(Transform labelTransform, Transform entityTransform)
    {
        labelTransform.gameObject.GetComponent<UsernameLabel>().Offset = new Vector3(0f, -1.0f, 0f);
    }

    public UsernameLabel CreateUserLabel(uint Id, string Username)
    {
        UsernameLabel label = (UsernameLabel) Instantiate(prefab);
        currentLabels.Add(Id, label);
        label.gameObject.GetComponent<TextMeshPro>().text = Username;
        return label;
    }
}