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

    private Vector2 Velocity;

    
    public float speed;

    public bool PlayerControlled { get; set; }

    [SerializeField]
    private LayerMask layerMask;

    protected float ACCELERATION;


    //Start is called before the first frame update
    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        ACCELERATION = 10f;
        //spawning = GetComponent<Spawning>();
    }

    // Update is called once per frame
    protected virtual void Update()
    {
            input_vector.x = Input.GetAxisRaw("Horizontal");
            input_vector.y = Input.GetAxisRaw("Vertical");
        // Velocity Changing
        Velocity = Vector2.MoveTowards(Velocity, input_vector.normalized * speed, Time.deltaTime*ACCELERATION);
        // Using Addforce
        //velocity = input_vector.normalized * speed;
    }

    protected void FixedUpdate()
    {
        print(input_vector);
        if (PlayerControlled && input_vector != Vector2.zero)
        {
            // velocity changing
            rb.velocity = Velocity;
            //rb.velocity = Vector2.Lerp(rb.velocity, input_vector.normalized * speed, 1);
            // Using Addforce
            //rb.AddForce(velocity);
            //rb.velocity = Vector2.ClampMagnitude(rb.velocity, 1);

        }
    }
}
