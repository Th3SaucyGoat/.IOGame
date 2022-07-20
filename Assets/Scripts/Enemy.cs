using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour, IDamagable
{
    public int MaxHealth { get; } = 20; 

    private int _health;
    public int health
    {
        set
        {
            _health = value;
            if (_health <= 0)
            {
                Destroy(gameObject);
            }
        }
        get
        {
            return _health;
        }
    }

    private Vector2 point;

    [SerializeField]
    private Transform topBounds;
    [SerializeField]
    private Transform bottomBounds;

    public float[] x_range;
    public float[] y_range;

    private Vector2 direction;
    private Rigidbody2D rb;
    private int speed = 1;


    // Start is called before the first frame update
    void Start()
    {
        health = MaxHealth;
        rb = GetComponent<Rigidbody2D>();
        point = findNewPoint();

    }

    // Update is called once per frame
    void Update()
    {
        //Find distance to point, if close to it, find new point. Else move towards point
        direction = point - (Vector2) gameObject.transform.position;
        float distance = direction.magnitude;
        if (distance < .05f)
        {
            point = findNewPoint();
        }
        else
        {
            rb.velocity = direction.normalized * speed;
        }
    }

    private Vector2 findNewPoint()
    {
        return new Vector2(Random.Range(x_range[0], x_range[1]), Random.Range(y_range[0], y_range[1]));
    }
}
