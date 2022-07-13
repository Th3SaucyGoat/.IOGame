using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerMovement : MonoBehaviour , IFoodPickup
{
    private Rigidbody2D rb;

    private Vector2 input_vector;

    public TextMeshProUGUI foodText;

    private Spawning spawning;

    private bool switchtoAlly = false;

    private bool playerControlled = true;

    public int food 
    { 
    get { return _food;}
    set { _food = value;
    if (_food > foodCapacity){
        _food = foodCapacity;
    }
    if (foodText != null) {foodText.text = "Food: " + _food.ToString();}
        }
    }
    private int _food;
    public int foodCapacity {get;} = 200;

    [SerializeField] float speed;
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spawning = GetComponent<Spawning>();
        _food = 20;
    }

    // Update is called once per frame
    void Update()
    {
        input_vector.x = Input.GetAxisRaw("Horizontal");
        input_vector.y = Input.GetAxisRaw("Vertical");

        if (Input.GetKey("1"))
        {
            if (food >= 20)
            {
            food -= 20;
            spawning.SpawnAlly("Collector");
            }
        }

        if (Input.GetKey("4"))
        {
            switchtoAlly = true;
        }

        if (switchtoAlly && Input.GetMouseButtonDown(0))
        {
            // Check if selected an ally
            Collider2D result = Physics2D.OverlapPoint(Camera.main.ScreenToWorldPoint(Input.mousePosition));
            if (result != null && result.gameObject.layer == 0)
            {
                result = result.gameObject;
                newAlly(result);

            }

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

    private void OnCollisionEnter2D(Collision2D other) {
        // Debug.Log("here");
    }

    private void OnTriggerEnter2D(Collider2D other) {
        // Debug.Log("here");
        
    }

    private void newAlly(GameObject ally)
    {
        playerControlled = false;
        
    }
}
