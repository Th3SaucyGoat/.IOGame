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
    }

    private void NewAlly(GameObject ally)
    {
        currentControlledEntity.GetComponent<IPlayerMovement>().PlayerControlled = false;
        //ally.AddComponent<PlayerMovement>();
        cam.LookAt = ally.transform;
        cam.Follow = ally.transform;
        //Destroy(result);
        ally.GetComponent<IPlayerMovement>().PlayerControlled = true;
        currentControlledEntity = ally;
        //print(ally.name);
        

    }
}
