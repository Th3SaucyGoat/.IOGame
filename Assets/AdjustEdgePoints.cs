using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdjustEdgePoints : MonoBehaviour
{
    public GameObject edgeColliderObject;
    private EdgeCollider2D boundary;
    private Transform[] softBodyVertices;
    private Vector2[] verticesPositions;
    private int boneCount = 0;

    // Start is called before the first frame update
    void Start()
    {
        if (edgeColliderObject == null)
        {
            Debug.LogError("Edge Collider not assigned!");
            Destroy(this);
            return;
        }
        boundary = edgeColliderObject.GetComponent<EdgeCollider2D>();
        foreach (Transform t in transform)
        {
            if (t.gameObject.name.Contains("bone"))
            {
                boneCount++;
            }

        }

        softBodyVertices = new Transform[boneCount];
        // Needed cause need to connect end point to start
        for (int i =0; i< boneCount; i++)
        {
            softBodyVertices[i] = transform.GetChild(i);
            if (transform.GetChild(i).gameObject.name.Contains("Boun"))
            {
                Debug.LogError("Boundary found instead of bones!");
            }
        }
        verticesPositions = new Vector2[softBodyVertices.Length + 1];
        AdjustEdgeVertices();
    }

    // Update is called once per frame
    void Update()
    {
        AdjustEdgeVertices();
    }

    void AdjustEdgeVertices()
    {
        for (int i = 0; i < verticesPositions.Length; i++)
        {
            if (i == verticesPositions.Length-1)
            {
                verticesPositions[i] = new Vector2(softBodyVertices[0].position.x, softBodyVertices[0].position.y);
            }
            else
                verticesPositions[i] = new Vector2(softBodyVertices[i].position.x, softBodyVertices[i].position.y);
        }
        boundary.points = verticesPositions;
    }
}
