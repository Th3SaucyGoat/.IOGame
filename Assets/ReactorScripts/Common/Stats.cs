using System.Collections;
using System.Collections.Generic;




public class Stats
{
    // Keep naming convention of these variables the same
    public static int CollectorCost = 5;
    public static int CollectorMaxHealth = 5;
    public string CollectorButton = "1"; // Temporary
    
    public static int ShooterCost = 15;
    public static int ShooterMaxHealth = 10;
    public string ShooterButton = "2";

    public static int HivemindMaxHealth = 20;
    

    

    //public float IdleDistance = 3.0f;

    private static Stats _current;
    public static Stats current
    {
        get
        {
            if (_current == null)
            {
                _current = new Stats();
            }
            return _current;
        }

    }
}

