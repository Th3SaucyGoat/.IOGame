using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shooter : PlayerMovement , IDamagable
{

    public int MaxHealth { get; }

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

    public GameObject hivemind;

    public GameObject EntityToFollow;

    private GameObject target;

    private List<GameObject> enemiesInRange = new List<GameObject>{};

    private Vector2 direction;

    private int frameNum = 0;

    [SerializeField]
    private Transform projectile;

    public float bulletForce = 5f;

    enum BEHAVIOUR { Protect, Chase }
    BEHAVIOUR behaviour = BEHAVIOUR.Chase;

    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();
        EntityToFollow = hivemind;
        speed = 2;
        subscribeToEvents();

    }

    private void subscribeToEvents()
    {
        ProxyTrigger entityLocator = transform.Find("ProxyTrigger").GetComponent<ProxyTrigger>();

        entityLocator.OnTriggerEnter2D_Action += entityLocator_OnTriggerEnter2D;
        entityLocator.OnTriggerExit2D_Action += entityLocator_OnTriggerExit2D;
    }

    // Update is called once per frame
    protected override void Update()
    {
        if (PlayerControlled)
        {
            base.Update();

            return;
        }
        //Chase State where chases after enemies
        if (behaviour == BEHAVIOUR.Chase)
        {
            if ( target == null)
            {
                //Start Timer. At the end of timeout, put back into protect mode. Or can have him idle/pace back and forth in place
                if (enemiesInRange.Count ==0)
                {

                }
                else
                {
                    target = findClosestEnemy();
                }
            }
            else
            {
                Vector2 position = gameObject.transform.position - target.transform.position;
                float distance = position.magnitude;
                if (distance > 2.0)
                {
                    MovetoPoint((Vector2)target.transform.position);
                }
                else
                {
                    rb.velocity = Vector2.zero;
                }
            }
        }
        //Protect state where stays within range of EntityToFollow, only slightly chases
        else if (behaviour == BEHAVIOUR.Protect)
        {
            Vector3 position = gameObject.transform.position - EntityToFollow.transform.position;
            float distance = position.magnitude;
            // Head back if too far away
            if (distance > 3.0f)
            {
                MovetoPoint((Vector2) EntityToFollow.transform.position);
            }
            // Ok to pursue a little bit if not too far away and there's an enemy
            else if (target != null)
            {
                MovetoPoint((Vector2) target.transform.position);
            }
            // Stay around at a distance from following entity
            else if (distance > 1.5f)
            {
                MovetoPoint((Vector2) EntityToFollow.transform.position);
            }
            else
            {
                rb.velocity = Vector2.zero;
            }

        }
        if (frameNum >= 100)
        {
            if (target != null)
            {
                shoot();
                frameNum = 0;
            }
        }
        frameNum += 1;

    }

    private void entityLocator_OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            enemiesInRange.Add(other.gameObject);
            print(enemiesInRange.Count);
        }
            
    }

    private void entityLocator_OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            enemiesInRange.Remove(other.gameObject);
        }
    }

    private void MovetoPoint(Vector2 point)
    {
        Vector2 direction = point - (Vector2) gameObject.transform.position;
        rb.velocity = direction.normalized * speed;
        Vector2 lookDir = point - (Vector2) transform.position;
        float angle = Mathf.Atan2( lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;
        transform.eulerAngles = angle;
    }

    private GameObject findClosestEnemy()
    {
        float closestDistance = 999999f;
        GameObject closestTarget = null;
        foreach (GameObject enemy in enemiesInRange)
        {
            if (enemy == null)
            {
                continue;
            }
            Vector3 position = transform.position - enemy.transform.position;
            float distance = position.magnitude;
            if (distance < closestDistance)
            {
                closestTarget = enemy;
                closestDistance = distance;
            }

        }
        return closestTarget;
    }

    private void shoot()
    {
        Transform bullet = Instantiate(projectile, transform.position, rb.rotation);
        bullet.GetComponent<Rigidbody2D>().AddForce(-transform.up * bulletForce, ForceMode2D.Impulse);

    }
}
