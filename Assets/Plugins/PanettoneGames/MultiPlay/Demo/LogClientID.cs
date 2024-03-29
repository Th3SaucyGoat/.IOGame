using UnityEngine;

public class LogClientID : MonoBehaviour
{
#if UNITY_EDITOR
    private void Start()
    {
        int clientIndex = MultiPlay.Utils.GetCurrentClientIndex();

        if (clientIndex == 0) Debug.Log("MultiPlay is running on: Main Project/Server");
        else Debug.Log($"MultiPlay is running on Client: {clientIndex}");
    }
#endif
}
