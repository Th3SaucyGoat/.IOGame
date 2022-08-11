using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class GameEvents : MonoBehaviour
{
    public Action onReady;
    public Action RoomFound;
    public Action Disconnected;
    public Action StartMatch;
    public Action<Transform> ChangeCamera;
    public Action<int> FoodChanged;
    public Action<bool> GameOver;
    public static GameEvents current;

    private void Awake()
    {
        current = this;
    }

    public void Ready()
    {
        onReady?.Invoke();
    }


}
