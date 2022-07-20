using Cinemachine;
using UnityEngine;
using System.Collections.Generic;


public class InputHandler : MonoBehaviour
{



    [SerializeField]
    private LayerMask layerMask;

    [SerializeField]
    private CinemachineVirtualCamera cam;

    private bool switchtoAlly = false;

    private Spawning spawning;
    private IFoodPickup foodPickup;

    private GameObject currentControlledEntity;

    private GameObject hive;

    private List<GameObject> alliesFollowing = new List<GameObject>();

    // Start is called before the first frame update
    void Start()
    {
        //Get a reference to the Hivemind and her food count and spawning function.
        currentControlledEntity = GameObject.Find("Hivemind");
        hive = currentControlledEntity;
        spawning = hive.GetComponent<Spawning>();
        foodPickup = hive.GetComponent<IFoodPickup>();



    }

    // Update is called once per frame
    void Update()
    {

        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            switchtoAlly = true;
        }
        if (Input.GetKeyDown("1"))
        {
            if (foodPickup.food >= 20)
            {
                foodPickup.food -= 20;
                spawning.SpawnAlly("Collector");
            }
        }

        if (Input.GetKeyDown("2"))
        {
            if (foodPickup.food >= 50)
            {
                foodPickup.food -= 50;
                spawning.SpawnAlly("Shooter");
            }
        }
        if (switchtoAlly && Input.GetMouseButtonDown(0))
        {
            // Check if selected an ally
            Collider2D result = Physics2D.OverlapPoint(Camera.main.ScreenToWorldPoint(Input.mousePosition), layerMask);
            switchtoAlly = false;
            if (result != null)
            {
                GameObject new_ally = result.gameObject;
                
                SwitchControl(new_ally);

            }
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            spawning.Return();
        }
        if (Input.GetKeyDown(KeyCode.F))
        {
            //find closest collector/ally. Set entity to follow to the current player object if at minimum distance away.
            GameObject closestAlly = findClosestAlly();
            if (closestAlly != null)
            {
                closestAlly.GetComponent<Collector>().EntityToFollow = currentControlledEntity;
                closestAlly.GetComponent<Collector>().determineFollowState();

                foreach (GameObject ally in alliesFollowing)
                {
                    if (closestAlly == ally)
                    {
                        print("RETURNED AN ALREADY FOLLOWING ALLY");
                    }
                }
                alliesFollowing.Add(closestAlly);
                print(alliesFollowing.Count);

            }
        }

        //dismiss following allies
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (alliesFollowing.Count > 0)
            {
                foreach (GameObject ally in alliesFollowing)
                {
                    ally.GetComponent<Collector>().EntityToFollow = hive;
                    ally.GetComponent<Collector>().determineDismissState();
                    
                }
                alliesFollowing.Clear();
            }
        }
    }

    private void SwitchControl(GameObject ally)
    {
        currentControlledEntity.GetComponent<PlayerMovement>().PlayerControlled = false;
        //ally.AddComponent<PlayerMovement>();
        cam.LookAt = ally.transform;
        cam.Follow = ally.transform;
        //Destroy(result);
        ally.GetComponent<PlayerMovement>().PlayerControlled = true;
        currentControlledEntity = ally;
        //print(ally.name);
    }

    private GameObject findClosestAlly()
    {
        GameObject[] allAllies = GameObject.FindGameObjectsWithTag("Collector");
        float closestDistance = 999999f;
        GameObject closestTarget = null;
        foreach (GameObject ally in allAllies)
        {
            if (ally == null | alliesFollowing.Contains(ally) | ally == currentControlledEntity)
            {
                continue;
            }
            Vector3 position = transform.position - ally.transform.position;
            float distance = position.magnitude;
            if (distance < closestDistance)
            {
                closestTarget = ally;
                closestDistance = distance;
            }

        }
        if (closestDistance < 9999f)
        {
            return closestTarget;
        }
        else
        {
            return null;
        }

        
    }

    }

