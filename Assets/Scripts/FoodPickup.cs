using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FoodPickup : MonoBehaviour , IFoodPickup
{
    public int food { set; get; } = 0;

    public int foodCapacity { set; get; } = 200;
    // Start is called before the first frame update
    void Start()
    {
        // GameObject proxy = transform.Find("ProxyTrigger").gameObject;
        // OnTriggerEnter2D -= proxy;
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    
    // private void OnCollisionEnter2D(Collision2D other) {
    //     GameObject entity = other.gameObject;
    //     print(entity);
    //     print(entity.CompareTag("Food"));
    //     if (entity.CompareTag("Food"))
    //     {
    //         food += entity.GetComponent<Food>().food;
    //         Destroy(entity);
    //     }
    // }
}
