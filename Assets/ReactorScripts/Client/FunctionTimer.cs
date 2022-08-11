/* 
    ------------------- Code Monkey -------------------

    Thank you for downloading this package
    I hope you find it useful in your projects
    If you have any questions let me know
    Cheers!

               unitycodemonkey.com
    --------------------------------------------------
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FunctionTimer {


    private static List<FunctionTimer> activeTimerList;
    private static GameObject initGameObject;

    private static void InitIfNeeded() {
        if (initGameObject == null) {
            initGameObject = new GameObject("FunctionTimer_InitGameObject");
            activeTimerList = new List<FunctionTimer>();
        }
    }

    public static FunctionTimer Create(Action action, float timer, bool DestroyOnComplete = true, string timerName = null) {
        InitIfNeeded();
        GameObject gameObject = new GameObject("FunctionTimer", typeof(MonoBehaviourHook));

        FunctionTimer functionTimer = new FunctionTimer(action, timer, timerName, DestroyOnComplete, gameObject);

        gameObject.GetComponent<MonoBehaviourHook>().onUpdate = functionTimer.Update;

        activeTimerList.Add(functionTimer);

        return functionTimer;
    }

    private static void RemoveTimer(FunctionTimer functionTimer) {
        InitIfNeeded();
        activeTimerList.Remove(functionTimer);
    }

    public static void StopTimer(string timerName) {
        for (int i = 0; i < activeTimerList.Count; i++) {
            if (activeTimerList[i].timerName == timerName) {
                // Stop this timer
                activeTimerList[i].DestroySelf();
                i--;
            }
        }
    }



    // Dummy class to have access to MonoBehaviour functions
    private class MonoBehaviourHook : MonoBehaviour {
        public Action onUpdate;
        private void Update() {
            if (onUpdate != null) onUpdate();
        }
    }

    private Action action;
    private float timer;
    private float time_value;
    private string timerName;
    public GameObject gameObject;
    private bool oneShot;

    private FunctionTimer(Action action, float timer, string timerName, bool destroyOnComplete, GameObject gameObject) {
        this.action = action;
        this.time_value = timer;
        this.timerName = timerName;
        this.gameObject = gameObject;
        this.oneShot = destroyOnComplete;
    }

    public void Update() {
        if (timer != 0)
        {
            timer -= Time.deltaTime;
            if (timer < 0)
            {
                // Trigger the action
                action();
                timer = 0f;
                if (oneShot)
                {
                    DestroySelf();
                }
            }
        }
            
    }

    private void DestroySelf() {
        UnityEngine.Object.Destroy(gameObject);
        RemoveTimer(this);
    }

    public void Start(float time)
    {
        timer = time;
    }

    public void Start()
    {
        timer = time_value;
    }
}
