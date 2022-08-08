using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UsernameLabel : MonoBehaviour
{

    private static UsernameLabel _instance;

    public static UsernameLabel Instance
    {
        get
        {

            if (_instance == null)
            {
                _instance = FindObjectOfType<UsernameLabel>();

            }
            return _instance;
        }
        set { _instance = value;
            TMP = value.GetComponent<TextMeshPro>(); 
        }
    }

    private static TextMeshPro TMP;
    public static string Text
    {
        set
        {
            TMP.text = value;
        }
            
            get { return TMP.text; }
    }

    public Vector2 Offset { set; private get; }
    private Vector2 _position;
    public Vector2 Position
    {
        set
        {
            _position = value;
            transform.position = value + Offset;
        }
        get { return _position; }
    }
private void Awake()
    {
        
    }
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        //if (startingPosition != null || startingPosition != Vector2.zero)
        //{
        //    transform.position = startingPosition;

        //}
        transform.rotation = new Quaternion(0f,0f,0f, 0f);
    }
}
