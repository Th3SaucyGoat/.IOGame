using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


public class PlayerMovement : MonoBehaviour
{
    protected Rigidbody2D rb;

    private Vector2 input_vector;

    
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
    public virtual void Update()
    {
            input_vector.x = Input.GetAxisRaw("Horizontal");
            input_vector.y = Input.GetAxisRaw("Vertical");

            // if (result != null)
            // {
            //     print(result);
            // }
            // If not, return
            Move();
            

        
            

    }



    protected void Move()
    {

        rb.velocity = input_vector.normalized*speed;
    }


}
