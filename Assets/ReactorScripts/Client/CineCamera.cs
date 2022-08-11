using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CineCamera : MonoBehaviour
{
    private void Start()
    {
        GameEvents.current.ChangeCamera += OnCameraChanged;
    }

    private void OnCameraChanged(Transform T)
    {
        var cine = GetComponent<CinemachineVirtualCamera>();
        cine.LookAt = T;
        cine.Follow = T;
    }
}
