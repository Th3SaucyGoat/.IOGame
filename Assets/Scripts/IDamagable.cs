using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDamagable
{
    int health { set; get; }
    int MaxHealth { get; }

}
