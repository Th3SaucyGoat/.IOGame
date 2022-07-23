using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


// If using Add force, you can use linear drag as the friction. And mass is taken into account
// If just changing velocity with move towards, it will act as friction

public class PlayerMovement : MonoBehaviour
{
    protected Rigidbody2D rb;

    private Vector2 input_vector;

    private Vector2 velocity;

    
    public float speed;

    public bool PlayerControlled { get; set; }

    [SerializeField]
    private LayerMask layerMask;


     //Start is called before the first frame update
    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        //spawning = GetComponent<Spawning>();
    }

    // Update is called once per frame
    protected virtual void Update()
    {
            input_vector.x = Input.GetAxisRaw("Horizontal");
            input_vector.y = Input.GetAxisRaw("Vertical");
        // Velocity Changing
        velocity = Vector2.MoveTowards(velocity, input_vector.normalized * speed, Time.deltaTime*speed*1.5f);
        // Using Addforce
        //velocity = input_vector.normalized * speed;

            

        
            

    }

    protected void FixedUpdate()
    {
        if (PlayerControlled)
        {
            // velocity changing
            rb.velocity = velocity;
            // Using Addforce
            //rb.AddForce(velocity);
            //rb.velocity = Vector2.ClampMagnitude(rb.velocity, 1);

        }
    }
}
