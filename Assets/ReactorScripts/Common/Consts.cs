using System.Collections;
using System.Collections.Generic;

public class Prop
{
    public const uint NAME = 0;               // Used by player
    public const uint READY = 1;              // Used by player
    public const uint FOOD = 2;               // Used by certain entities
    public const uint HIVEMINDID = 3;         // Used by player and entites
    public const uint TEAMID = 4;             // Used by player and entites
    public const uint CONTROLLEDPLAYERID = 5; // Used by entities
    public const uint CONTROLLEDENTITYID = 6; // Used by player
    public const uint PLAYERFOLLOWINGID = 7;  // Used by entites
    public const uint FIRING = 7;             // Used by certain entities
}

public class RPC
{
    public const uint SETREADYSTATUS = 0;
    public const uint READYSTATUS = 1;
    public const uint STARTMATCH = 2;
    public const uint SENDINFO = 3;
    public const uint SPAWNUNIT = 4;
    public const uint REQUESTCONTROL = 5;
    public const uint TAKECONTROL = 6;
    public const uint FIRING = 7;
    public const uint RELAYMOUSEINFO = 8;
    public const uint FOLLOW = 9;
    public const uint DISMISS = 10;
    public const uint PLAYERCONTROLLEDENTITYDESTROYED = 11;  // Called by entities sometimes when they are destroyed
}
