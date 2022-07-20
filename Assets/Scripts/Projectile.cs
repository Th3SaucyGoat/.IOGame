using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{

    public int damage = 5;




    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Ally"))
        {
            return;
        }

        //Find out whether or not this object is damageable
        if (other.TryGetComponent(out IDamagable hurtbox))
        {
            hurtbox.health -= damage;
        };
        //Regardless of the object's damageability, if the object isn't an ally, self-destruct the bullet.
        Destroy(gameObject);

    }

}      
