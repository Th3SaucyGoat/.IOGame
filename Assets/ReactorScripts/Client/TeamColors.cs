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
        print(teamId);
        switch (teamId)
        {
            case 1:
                return team1;
            case 2:
                return team2;
            case 3:
                return team3;
            case 4:
                return team4;
            default:
                return null;
        }
    }
}
