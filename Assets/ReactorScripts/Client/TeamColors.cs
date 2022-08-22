using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeamColors : MonoBehaviour
{
    private static readonly Color[][] colors = new Color[][] 
    {
        // Blue
        new Color[]
    {
        new Color(0, 0.3f, 1),
        new Color(0,1,1)
    },
         // Red
        new Color[]
    {
        new Color(1,0.2f,0.2f),
        new Color(1, 0.5f, 0.5f)
    },
        // Green
        new Color[]
    {
        new Color(0.2f, 1, 0.2f),
        new Color(0.7f, 1, 0.7f)
    },
        // Yellow
        new Color[]
    {
        new Color(1, .95f, .17f),
        new Color(1, .96f, .68f)
    },
    };

    public static Color[] DetermineTeamColor(uint teamId)
    {
        int Id = (int) teamId;
        if (teamId > colors.Length)
        {
            Id = (int) teamId % colors.Length;
        }
        return colors[Id];
    }
}
