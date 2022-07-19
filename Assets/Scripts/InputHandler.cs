using Cinemachine;
using UnityEngine;

public class InputHandler : MonoBehaviour
{


    //private

    [SerializeField]
    private LayerMask layerMask;

    [SerializeField]
    private CinemachineVirtualCamera cam;

    private bool switchtoAlly = false;

    private Spawning spawning;
    private IFoodPickup foodPickup;

    private GameObject currentControlledEntity;

    private GameObject hive;


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

        if (Input.GetKeyDown("4"))
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
        if (switchtoAlly && Input.GetMouseButtonDown(0))
        {
            // Check if selected an ally
            Collider2D result = Physics2D.OverlapPoint(Camera.main.ScreenToWorldPoint(Input.mousePosition), layerMask);
            switchtoAlly = false;
            if (result != null)
            {
                GameObject new_ally = result.gameObject;
                
                NewAlly(new_ally);

            }
        }
        if (Input.GetButtonDown("Fire2"))
        {
            spawning.Return();
        }
        if (Input.GetKeyDown("2"))
        {
            //find closest collector/ally. Set entity to follow to the current player object if at minimum distance away.
            GameObject closestAlly = findClosestAlly();

        }
    }

    private void NewAlly(GameObject ally)
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
            if (ally == null)
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
        return closestTarget;
    }
    }

