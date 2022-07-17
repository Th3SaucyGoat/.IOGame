using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Cinemachine;

public class PlayerMovement : MonoBehaviour , IPlayerMovement
{
    private Rigidbody2D rb;

    private Vector2 input_vector;

  

    private Spawning spawning;


    public bool PlayerControlled { get; set; } = true;



    public int foodCapacity { get;} = 200;

    [SerializeField]
    private LayerMask layerMask;

    [SerializeField] float speed;


     //Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spawning = GetComponent<Spawning>();
    }

    // Update is called once per frame
    void Update()
    {
        if (PlayerControlled)
        {
            input_vector.x = Input.GetAxisRaw("Horizontal");
            input_vector.y = Input.GetAxisRaw("Vertical");

                // if (result != null)
                // {
                //     print(result);
                // }
                // If not, return
            }

        
            

    }

    private void FixedUpdate() {
        Move();
    }

    private void Move()
    {
        rb.velocity = input_vector.normalized*speed;
    }


}
