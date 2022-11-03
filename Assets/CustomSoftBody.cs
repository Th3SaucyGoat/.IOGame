using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class CustomSoftBody : MonoBehaviour
{
    // Start is called before the first frame update
    void Awake()
    {
        GameObject organelleBoundary = transform.Find("OrganelleBoundary").gameObject;
        organelleBoundary.transform.localScale = transform.localScale;
        organelleBoundary.transform.localPosition = transform.localPosition;
        GetComponent<AdjustEdgePoints>().edgeColliderObject = organelleBoundary;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
