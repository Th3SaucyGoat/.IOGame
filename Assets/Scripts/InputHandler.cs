using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class InputHandler : MonoBehaviour
{
    private bool switchtoAlly = false;

    private CinemachineVirtualCamera cam;

    [SerializeField]
    private int layerMask;

    // Start is called before the first frame update
    void Start()
    {
        //Get a reference to the Hivemind and her food count and spawning function.
    }

    // Update is called once per frame
    void Update()
    {

        if (Input.GetKey("4"))
        {
            switchtoAlly = true;
        }
        if (Input.GetKey("1"))
        {
            if (food >= 20)
            {
                food -= 20;
                spawning.SpawnAlly("Collector");
            }
        }
        if (Input.GetKey("4"))
        {
            switchtoAlly = true;
        }
        if (switchtoAlly && Input.GetMouseButtonDown(0))
        {
            // Check if selected an ally
            Collider2D result = Physics2D.OverlapPoint(Camera.main.ScreenToWorldPoint(Input.mousePosition), layerMask);

            if (result != null)
            {
                GameObject Object = result.gameObject;
                print(Object);
                NewAlly(Object);

            }
        }

        if (switchtoAlly && Input.GetMouseButtonDown(0))
        {
            // Check if selected an ally
            Collider2D result = Physics2D.OverlapPoint(Camera.main.ScreenToWorldPoint(Input.mousePosition), layerMask);
            if (result != null)
            {
                GameObject Object = result.gameObject;
                NewAlly(Object);
                print(Object);

            }
        }


}

private void NewAlly(GameObject ally)
    {
        ally.GetComponent<Collector>().playerControlled = true;
        cam.LookAt = ally.transform;
        cam.Follow = ally.transform;


    }
}
