using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawning : MonoBehaviour
{

    [SerializeField] private GameObject collector;
    [SerializeField] private GameObject shooter;

    [SerializeField] public Transform allyContainer;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
 


    }

    public void SpawnAlly(string type)
    {
        switch (type)
        {
            case "Collector":
                GameObject ally = Instantiate(collector, gameObject.transform.position, Quaternion.identity);
                ally.GetComponent<Collector>().hivemind = gameObject;
                ally.transform.parent = allyContainer;
                break;
            case "Shooter":
                GameObject a = Instantiate(shooter, gameObject.transform.position, Quaternion.identity);
                a.GetComponent<Shooter>().hivemind = gameObject;
                a.GetComponent<Shooter>().cam = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
                a.transform.parent = allyContainer;

                break;
        }
    }

    public void Return()
    {
        GameObject[] collectors = GameObject.FindGameObjectsWithTag("Collector");
        foreach (GameObject collector in collectors)
        {
            Collector c = collector.GetComponent<Collector>();
            c.Return();
        }
    }
}
