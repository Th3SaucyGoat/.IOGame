using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapBrain : MonoBehaviour
{

    [SerializeField] private Transform topBounds;
    [SerializeField] private Transform bottomBounds;

    [SerializeField] private GameObject food;
    [SerializeField] private GameObject enemy; 

    private float[] x_range;
    private float[] y_range;

    private bool doneSpawning = false;

    // Start is called before the first frame update
    
    void Start()
    {

        x_range = new float[2] {topBounds.position.x, bottomBounds.position.x};
        y_range = new float[2] {topBounds.position.y, bottomBounds.position.y};
        int i = 0;
        while (i < 50){
            SpawnFood();
            i++;
        }
        doneSpawning = true;
        Invoke("SpawnFood", 1f);
        SpawnEnemy();

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void SpawnFood()
    {
        Vector2 position = new Vector2(Random.Range(x_range[0], x_range[1]), Random.Range(y_range[0], y_range[1]));
        Instantiate(food, new Vector3(position.x,position.y, 0), Quaternion.identity);
        if (doneSpawning) { Invoke("SpawnFood", 1f);}
    }

    void SpawnEnemy()
    {
        Vector2 position = new Vector2(Random.Range(x_range[0], x_range[1]), Random.Range(y_range[0], y_range[1]));
        GameObject e = Instantiate(enemy, (Vector3)position, Quaternion.identity);
        e.GetComponent<Enemy>().x_range = x_range;
        e.GetComponent<Enemy>().y_range = y_range;
        Invoke("SpawnEnemy", 10f);
    }
}
