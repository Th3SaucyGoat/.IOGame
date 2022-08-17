using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeamColors : MonoBehaviour
{
    // Blue
    private static readonly Color[] team1 = new Color[]
    {
        new Color(0, 0.3f, 1),
        new Color(0,1,1)
    };
    // Red
    private static readonly Color[] team2 = new Color[]
    {
        new Color(1,0.2f,0.2f),
        new Color(1, 0.5f, 0.5f)
    };
    // Green
    private static readonly Color[] team3 = new Color[]
{
        new Color(0.2f, 1, 0.2f),
        new Color(0.7f, 1, 0.7f)
};
    // Yellow
    private static readonly Color[] team4 = new Color[]
{
        new Color(1, .95f, .17f),
        new Color(1, .96f, .68f)
};


    public static Color[] DetermineTeamColor(uint teamId)
    {
        int Id = (int) teamId;
        if (teamId > 4)
        {
            Id = (int) teamId % 4;
        }
        return Id switch
        {
            1 => team1,
            2 => team2,
            3 => team3,
            4 => team4,
            _ => null,
        };
    }
}
