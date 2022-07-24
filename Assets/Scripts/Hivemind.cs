using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


public class Hivemind : PlayerMovement, IFoodPickup , IDamagable
{

    public TextMeshProUGUI foodText;

    public int foodCapacity { get; set; } = 200;

    public int MaxHealth { get; } = 100;

    public float factor;

    private int _health;
    public int health
    {
        set
        {
            _health = value;
            if (_health <= 0)
            {
                print(gameObject.name.ToString() + " Died");
            }
        }
        get { return _health; }
    }



    [SerializeField]
    private Transform topBounds;
    [SerializeField]
    private Transform bottomBounds;

    private float[] x_range;
    private float[] y_range;

    private Vector2 point;
    private Vector2 direction;

    private Vector2 velocity;


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
        health = MaxHealth;
        PlayerControlled = true;
        food = 100;
        ProxyTrigger foodHitbox = transform.Find("FoodHitbox").GetComponent<ProxyTrigger>();
        foodHitbox.OnTriggerEnter2D_Action += foodHitbox_OnTriggerEnter2D;

        x_range = new float[2] { topBounds.position.x, bottomBounds.position.x };
        y_range = new float[2] { topBounds.position.y, bottomBounds.position.y };
        point = findNewPoint();
}

    protected override void Update()
    {
        if (PlayerControlled)
        {
            base.Update();
            return;
        }
        else if (point != null)
        {
            moveTowardsPoint(point);

            Vector2 pos = point - (Vector2) gameObject.transform.position;
            float distance = pos.magnitude;
            if (distance < 1.0)
            {
                point = findNewPoint();
            }
        }
    }

    private void moveTowardsPoint( Vector2 point)
    {
        direction = (point - (Vector2)gameObject.transform.position);
        rb.velocity = Vector2.MoveTowards(rb.velocity, direction.normalized * speed, Time.deltaTime);
        

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
