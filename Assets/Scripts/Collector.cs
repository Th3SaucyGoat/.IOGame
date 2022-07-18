using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Collector : MonoBehaviour , IFoodPickup ,  IPlayerMovement
{

    public int food {get {return _food;} 
   
    set {
        _food = value;
        if (_food >= foodCapacity)
        {
            _food = foodCapacity;
            Feed();
        }
        if (_food < 0)
        {
            _food = 0;
        }         
    }
    }
    private int _food;
    public GameObject player;

    public int foodCapacity {get;} = 20;
 

    public float distanceFromPlayer = 1;

    public GameObject target;

    // private Hivemind hivemind;
    private List<GameObject> foodInRange =  new List<GameObject>{};

    private Rigidbody2D rb;

    private float speed =2.5f;

    private Camera mainCamera;

    private bool attached =  false;

    public bool PlayerControlled { get; set; }

    private Vector2 direction;
    private int feedNum = 0;

    string Name;

    private Vector2 attachedOffset;

    enum BEHAVIOUR {Idle, Collect, Return}
    BEHAVIOUR behaviour = BEHAVIOUR.Idle;
    private Vector2 input_vector;

    private Vector2 point;

    void Start()
    {

        rb = GetComponent<Rigidbody2D>();
        mainCamera = Camera.main;
        SubscribetoEvents();
        
    }

    private void SubscribetoEvents()
    {
        ProxyTrigger foodLocator = transform.Find("ProxyTrigger").GetComponent<ProxyTrigger>();
        ProxyTrigger foodHitbox = transform.Find("FoodHitbox").GetComponent<ProxyTrigger>();

        foodLocator.OnTriggerEnter2D_Action += foodLocator_OnTriggerEnter2D;
        foodLocator.OnTriggerExit2D_Action += foodLocator_OnTriggerExit2D;
        foodHitbox.OnTriggerEnter2D_Action += foodHitbox_OnTriggerEnter2D;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (PlayerControlled && behaviour != BEHAVIOUR.Return)
        {
            input_vector.x = Input.GetAxisRaw("Horizontal");
            input_vector.y = Input.GetAxisRaw("Vertical");

            rb.velocity = input_vector.normalized * speed;

            if (Input.GetKeyDown(KeyCode.Space))
            {
                Vector3 position = gameObject.transform.position - player.transform.position;
                float distance = position.magnitude;
                if (distance < 2.0)
                {
                    Return();
                }
            }
        }
            if (behaviour == BEHAVIOUR.Return)
        {
            if (attached)
            {
                feedNum += 1;
                if (feedNum == 5){
                food -= 1;
                player.gameObject.GetComponent<IFoodPickup>().food += 1;
                feedNum = 0;
                }
                rb.MovePosition((Vector2) player.transform.position - attachedOffset);
                if (food == 0)
                {
                    Detach();

                }
            return;
            }
        }

        if (behaviour == BEHAVIOUR.Idle)
        {
            if (point != null)
            {
            }
            Vector3 position = gameObject.transform.position - player.transform.position;
            float distance = position.magnitude;
            if (distance < 2.0)
            {
                target = null;
            }
        }
        if (target == null && food != foodCapacity)
        {
            target = findClosestFood();
        } 
        Move();
    }

    private void Detach()
    {
        print("Detached");
        behaviour = BEHAVIOUR.Idle;
        attached = false;
        attachedOffset = Vector2.zero;
        target = null;
        rb.mass = 1;
        
    }

    private void Move()
    {
        if (PlayerControlled && behaviour != BEHAVIOUR.Return)
        {
            input_vector.x = Input.GetAxisRaw("Horizontal");
            input_vector.y = Input.GetAxisRaw("Vertical");
            rb.velocity = input_vector.normalized * speed;

            return;
        }
        // direction = (mainCamera.ScreenToWorldPoint(Input.mousePosition) - gameObject.transform.position);
        Vector2 pos = gameObject.transform.position - player.transform.position;  
        float distance = pos.magnitude;
        if (target != null){
        direction = (target.transform.position - gameObject.transform.position);

        rb.velocity = direction.normalized*speed;
        }
        else if (distance > 1)
        {
            direction = pos/distance;
            rb.velocity = -direction.normalized*speed;
 
        }
        else{
            rb.velocity = Vector2.zero;
        }
    }

    private void foodLocator_OnTriggerEnter2D(Collider2D other)
    {
        foodInRange.Add(other.gameObject);
    }

    private void foodLocator_OnTriggerExit2D(Collider2D other)
    {
        foodInRange.Remove(other.gameObject);
    }

    private void foodHitbox_OnTriggerEnter2D(Collider2D other)
    {
        Food the_food = other.GetComponent<Food>();
        if (food < foodCapacity){
        food += the_food.food;
        Destroy(other.gameObject);
        Name = other.name;
        // Find distance from player and determine next action
        Vector3 position = gameObject.transform.position - player.transform.position;
        float distance = position.magnitude;
        if (distance >3.5){
        target = player;
        behaviour = BEHAVIOUR.Idle;
        }
        }
    }
    private void OnCollisionEnter2D(Collision2D other) {
        if (behaviour == BEHAVIOUR.Return && other.gameObject == player)
        {
            if (!attached)
            {
                attachedOffset = (Vector2) player.transform.position - (Vector2) gameObject.transform.position; 
                attached = true;
                rb.mass = 0.1f;
            }
        }
    }

    private GameObject findClosestFood()
    {
        float closestDistance = 999999f;
        GameObject closestTarget =  null;
        foreach (GameObject f in foodInRange)
        {
            if (f == null)
            {
                continue;
            }
            Vector3 position = transform.position - f.transform.position;
            float distance = position.magnitude;
            if (distance < closestDistance)
            {
                closestTarget = f;
                closestDistance = distance;
            }
        
        }
        return closestTarget;
    }

    public void Return(){
        if (food > 0)
        {
            Feed();
        }
    }

    private void Feed()
    {
    behaviour = BEHAVIOUR.Return;
    target = player;
    } 
}


