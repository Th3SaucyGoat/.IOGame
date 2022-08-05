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

private void Awake()
    {
        
    }
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        transform.rotation = Quaternion.identity;
    }
}
