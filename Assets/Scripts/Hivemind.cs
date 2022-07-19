using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


public class Hivemind : PlayerMovement, IFoodPickup
{

    public TextMeshProUGUI foodText;

    public int foodCapacity { get; set; } = 200;


    [SerializeField]
    private Transform topBounds;
    [SerializeField]
    private Transform bottomBounds;

    private float[] x_range;
    private float[] y_range;

    private Vector2 point;
    private Vector2 direction;




    private int _food;
    public int food
    { get { return _food;}
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

    protected override void Start()
    {
        base.Start();
        PlayerControlled = true;
        food = 100;
        ProxyTrigger foodHitbox = transform.Find("FoodHitbox").GetComponent<ProxyTrigger>();
        foodHitbox.OnTriggerEnter2D_Action += foodHitbox_OnTriggerEnter2D;

        x_range = new float[2] { topBounds.position.x, bottomBounds.position.x };
        y_range = new float[2] { topBounds.position.y, bottomBounds.position.y };
        point = findNewPoint();
}

    public override void Update()
    {
        if (PlayerControlled)
        {
            base.Update();
        }
        else if (point != null)
        {
            direction = (point - (Vector2) gameObject.transform.position);
            rb.velocity = direction.normalized * speed;

            Vector2 pos = point - (Vector2) gameObject.transform.position;
            float distance = pos.magnitude;
            if (distance < 1.0)
            {
                point = findNewPoint();
            }
        }
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

    private Vector2 findNewPoint()
    {
        return new Vector2(Random.Range(x_range[0], x_range[1]), Random.Range(y_range[0], y_range[1]));
    }
}
