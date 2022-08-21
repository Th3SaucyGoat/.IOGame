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
    public Action<bool> SpawnMenuOpen;
    public Action StartRespawn;
    public Action EndRespawn;
    public Action<String> UnitTakenControl;
    public Action FiredasShooter;
    public Action<bool> InRangeToFeed;
    public Action<bool> RespawnTutorialComplete;
    public Action FirstUnitSpawned;
    public Action MatchInProgress;
    public Action StartRedirect;
    public Action StartSpectating;

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
