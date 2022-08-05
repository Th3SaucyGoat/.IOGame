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
    public Transform prefab;

    public static Dictionary<uint, Transform> currentLabels;

    private void Start()
    {
        currentLabels = new Dictionary<uint, Transform>();
    }

    public static void SetEntity(Transform labelTransform, Transform entityTransform)
    {
        SetLabelPosition(labelTransform, entityTransform);
        labelTransform.SetParent(entityTransform);
    }

    public static void SetLabelPosition(Transform labelTransform, Transform entityTransform)
    {
        labelTransform.position = new Vector3(entityTransform.position.x, entityTransform.position.y-1.0f, 0f);
    }

    public Transform CreateUserLabel(uint Id, string Username)
    {
        Transform label = Instantiate(prefab);
        currentLabels.Add(Id, label);
        label.gameObject.GetComponent<TextMeshPro>().text = Username;
        return label;
    }
}