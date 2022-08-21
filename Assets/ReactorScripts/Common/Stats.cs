using System.Collections;
using System.Collections.Generic;




public class Stats
{
    // Keep naming convention of these variables the same
    public static uint CollectorCost = 0;
    public static uint CollectorMaxHealth = 5;
    public string CollectorButton = "1"; // Temporary
    
    public static uint ShooterCost = 0;
    public static uint ShooterMaxHealth = 5;
    public string ShooterButton = "2";

    public static uint HivemindMaxHealth = 20;
    

    

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

