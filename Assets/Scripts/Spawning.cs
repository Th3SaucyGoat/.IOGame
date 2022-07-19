using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawning : MonoBehaviour
{

    [SerializeField] private GameObject collector;
    
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
