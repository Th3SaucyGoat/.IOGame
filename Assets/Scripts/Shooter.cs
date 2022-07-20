using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shooter : PlayerMovement , IDamagable , ICommandable
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

    public GameObject EntityToFollow { set; get; }

    private GameObject target;

    private FunctionTimer ROFTimer;

    private List<GameObject> enemiesInRange = new List<GameObject>{};

    private Vector2 direction;


    [SerializeField]
    private GameObject projectile;
    public Transform EOG;

    public float bulletForce = 5f;

    public Camera cam;

    public enum BEHAVIOUR { Protect, Chase }
    public BEHAVIOUR behaviour = BEHAVIOUR.Chase;

    private bool canShoot = true;

    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();
        ROFTimer = FunctionTimer.Create(OnShootTimerCompleted, 0.8f, false);
        ROFTimer.gameObject.transform.parent = transform;
        rb.freezeRotation = true;
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

            Vector2 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
            LookAtPoint(mousePos);
            if (Input.GetMouseButtonDown(0))
            {
                if (canShoot)
                {
                    shoot();
                }
            }
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
                    rb.velocity = Vector2.zero;
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

        //Constant looking at the target if there is one. Shoot if able
        if (target == null && enemiesInRange.Count > 0)
        {
            target = findClosestEnemy();
        }
        else if (target != null)
        {
            LookAtPoint((Vector2)target.transform.position);
            if (canShoot)
            {
                shoot();
            }
        }
    }

    private void LookAtPoint( Vector2 point)
    {
        Vector2 lookDir = point - (Vector2)gameObject.transform.position;
        float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg;
        rb.rotation = angle;
    }
    private void entityLocator_OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            enemiesInRange.Add(other.gameObject);
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
        ROFTimer.Start();
        canShoot = false;
        GameObject bullet = Instantiate(projectile, EOG.position, EOG.rotation);
        bullet.GetComponent<Projectile>().direction = ((Vector2) EOG.position - (Vector2) gameObject.transform.position).normalized;
        //bullet.GetComponent<Rigidbody2D>().AddForce(EOG.right * bulletForce, ForceMode2D.Impulse);
    }

    public void determineFollowState()
    {
        behaviour = BEHAVIOUR.Protect;
    }
    public void determineDismissState()
    {
        behaviour = BEHAVIOUR.Chase;
    }

    public void OnShootTimerCompleted()
    {
        canShoot = true;
    }
}
