using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Hivemind : MonoBehaviour, IFoodPickup
{

    public TextMeshProUGUI foodText;

    private int _food;
    public int foodCapacity { get; set; } = 200;

    public int food
    {
        get { return _food; }
        set
        {
            _food = value;
            if (_food > foodCapacity)
            {
                _food = foodCapacity;
            }
            if (foodText != null) { foodText.text = "Food: " + _food.ToString(); }
        }
    }
    void Start()
    {
        food = 20;
        ProxyTrigger foodHitbox = transform.Find("FoodHitbox").GetComponent<ProxyTrigger>();
        foodHitbox.OnTriggerEnter2D_Action += foodHitbox_OnTriggerEnter2D;
    }

    void Update()
    {

    }


    private void foodHitbox_OnTriggerEnter2D(Collider2D collision)
    {
        Food the_food = collision.gameObject.GetComponent<Food>();
        if (food < foodCapacity)
        {
            food += the_food.food;
            Destroy(collision.gameObject);
        }
    }
}
