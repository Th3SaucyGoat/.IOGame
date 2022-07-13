using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ProxyTrigger : MonoBehaviour
{

    public Action<Collider2D> OnTriggerEnter2D_Action;
    public Action<Collider2D> OnTriggerExit2D_Action;


    private void OnTriggerEnter2D(Collider2D other) {
        OnTriggerEnter2D_Action?.Invoke(other);
    }
    private void OnTriggerExit2D(Collider2D other) {
        OnTriggerExit2D_Action?.Invoke(other);
    }


}
