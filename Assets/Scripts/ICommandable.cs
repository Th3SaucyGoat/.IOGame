using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ICommandable
{
    GameObject EntityToFollow { set; }
    void determineFollowState();
    void determineDismissState();
}
