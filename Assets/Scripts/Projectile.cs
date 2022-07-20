using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField]
    private int damage = 5;

    private float speed = 5f;

    public Vector2 direction;
    private void Start()
    {
        print(transform.localRotation.ToString() + transform.rotation.ToString());
        
    }
    private void Update()
    {
        transform.Translate(Vector3.right * Time.deltaTime * speed);
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Ally"))
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
