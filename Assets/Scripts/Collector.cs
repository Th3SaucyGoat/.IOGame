using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Collector : PlayerMovement , IFoodPickup
{

    public int food {get {return _food;} 
   
    set {
        _food = value;
        if (_food >= foodCapacity)
        {
            _food = foodCapacity;
                if (!PlayerControlled)
                {
                    Feed();
                }
        }
        if (_food < 0)
        {
            _food = 0;
        }         
    }
    }
    private int _food;

    public GameObject hivemind;

    private IFoodPickup hivemindFood;

    private GameObject entityToFollow;

    public int foodCapacity {get;} = 20;

    public GameObject foodCheck;
    public bool foodChek = false;

    public float distanceFromPlayer = 1;

    public GameObject target;

    // private Hivemind hivemind;
    private List<GameObject> foodInRange =  new List<GameObject>{};

    private bool attached =  false;


    private Vector2 direction;
    private int feedNum = 0;

    string Name;

    private Vector2 attachedOffset;

    enum BEHAVIOUR {Idle, Collect, Return}
       
    BEHAVIOUR behaviour = BEHAVIOUR.Idle;


    private Vector2 point;
 

    protected override void Start()
    {
        base.Start();
        speed = 3f;
        hivemindFood = hivemind.gameObject.GetComponent<IFoodPickup>() ;
        entityToFollow = hivemind;
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

    public override void Update()
    {
        //Checking if player inputs should be taken into account for this gameobject
        if (PlayerControlled && behaviour != BEHAVIOUR.Return)
        {
            //The parent update function deals with input handling and moving
            base.Update();

            if (Input.GetKeyDown(KeyCode.Space))
            {
                //Feed Hivemind if close enough
                Vector3 position = gameObject.transform.position - hivemind.transform.position;
                float distance = position.magnitude;
                if (distance < 2.0)
                {
                    Return();
                }
            }
            return;
        }
        //Needs to check if attached and handle attached logic
        if (behaviour == BEHAVIOUR.Return)
        {

            if (attached)
            {
                feedNum += 1;
                if (feedNum == 5)
                {
                    food -= 1;
                    hivemindFood.food += 1;

                    feedNum = 0;
                }
                rb.MovePosition((Vector2) hivemind.transform.position - attachedOffset);
                if (food == 0)
                {
                    Detach();

                }
            }
            //Move towards player
            else
            {
                MovetoPoint((Vector2) hivemind.transform.position);
            }

        }
        //No food recognized nearby and can't feed. Stay at a certain distance away from entity following
        else if (behaviour == BEHAVIOUR.Idle)
        {

            if (point != null)
            {
            }
            Vector3 position = gameObject.transform.position - entityToFollow.transform.position;
            float distance = position.magnitude;
            // Stay at a distance from player
            if (distance > 1.5f)
            {
                MovetoPoint((Vector2) entityToFollow.transform.position);
            }
            else if (foodInRange.Count >= 1)
            {
                behaviour = BEHAVIOUR.Collect;
                target = findClosestFood();
            }
            else
            {
                rb.velocity = Vector2.zero;

            }
            //Seek food if detects food (done with locator)
        }

        //Currently focused on collecting a food
        else if (behaviour == BEHAVIOUR.Collect)
        {
            if (target == null)
            {
                target = findClosestFood();
            }
            else
            {
                MovetoPoint((Vector2) target.transform.position);

            }

        }
    }

    private void Detach()
    {
        print("Detached");
        behaviour = BEHAVIOUR.Idle;
        attached = false;
        attachedOffset = Vector2.zero;
        rb.mass = 1;
        
    }

    private void MovetoPoint(Vector2 point)
    {
        Vector2 direction =  point - (Vector2) gameObject.transform.position  ;
        rb.velocity = direction.normalized*speed;

    }

    

    private void foodLocator_OnTriggerEnter2D(Collider2D other)
    {
        foodInRange.Add(other.gameObject);
        }

    private void foodLocator_OnTriggerExit2D(Collider2D other)
    {
        
        foodInRange.Remove(other.gameObject);
        if (foodInRange.Count == 0)
        {
            behaviour = BEHAVIOUR.Idle;
        }
    }

    private void foodHitbox_OnTriggerEnter2D(Collider2D other)
    {
        Food the_food = other.GetComponent<Food>();
        if (food < foodCapacity){
        food += the_food.food;
        Destroy(other.gameObject);
        Name = other.name;
        // Find distance from player and determine next action
        Vector3 position = gameObject.transform.position - entityToFollow.transform.position;
        float distance = position.magnitude;
        //Too far away, return to the player
        if (distance > 3.5){
        if (behaviour != BEHAVIOUR.Return)
        {
         behaviour = BEHAVIOUR.Idle;
        }
        }

        }
    }

    private void OnCollisionEnter2D(Collision2D other) {
        if (behaviour == BEHAVIOUR.Return && other.gameObject == hivemind)
        {
            if (!attached)
            {
                attachedOffset = (Vector2) hivemind.transform.position - (Vector2) gameObject.transform.position; 
                attached = true;
                rb.mass = 0.1f;
            }
        }
    }

    private GameObject findClosestFood()
    {  
        if (foodInRange.Count == 0)
        {
            if (behaviour == BEHAVIOUR.Collect)
                {
                    behaviour = BEHAVIOUR.Idle;
                }
            return null;
        }
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
    } 
}


