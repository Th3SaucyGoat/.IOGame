using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerReadyLabel : MonoBehaviour
{
    public string username;

    public uint Id;

    private TextMeshProUGUI userLabel;
    private TextMeshProUGUI readyLabel;

    private bool _ready;
    public bool Ready { set
        {
            _ready = value;
            if (_ready)
            {
                readyLabel.text = "Ready";
            }
            else
            {
                readyLabel.text = "Not Ready";

            }
        }
        get { return _ready; }
    }

    private void Awake()
    {
        userLabel = transform.Find("PlayerUsername").GetComponent<TextMeshProUGUI>();
        readyLabel = transform.Find("ReadyStatus").GetComponent<TextMeshProUGUI>();
        Ready = false;
    }
    private void Start()
    {


        userLabel.text = username;

    }
}
