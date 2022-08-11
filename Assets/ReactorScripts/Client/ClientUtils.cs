using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KS;
using KS.Reactor.Client.Unity;
using KS.Reactor.Client;
using KS.Reactor;

public class ClientUtils : MonoBehaviour
{
    public static bool IsEntityValid(ksEntity entity) { return !(entity == null || entity.IsDestroyed); }

    public static List<ksEntity> FindCommandableAllies(ksPlayer player, ksConstList<ksEntity> entities)
    {
        var validAllies = new List<ksEntity> { };
        foreach (ksEntity entity in entities)
        {
            // Check if it is a Unit, that is on the same team, that is not controlled, that is not already following a player.
            if (entity.GameObject.TryGetComponent(out Unit unit))
            {
                //print("a");
                if (CheckTeam(player, entity))
                {
                    //print("b");
                    if (entity.Properties[Prop.CONTROLLEDPLAYERID] == "")
                    {
                        //print("c");

                        if (entity.Properties[Prop.PLAYERFOLLOWINGID] == "")
                        {
                            //print("d");

                            validAllies.Add(entity);
                        }
                    }
                }
            }
        }
        return validAllies;
    }

    public static List<ksEntity> RetrieveControllableAllies(ksPlayer player, ksConstList<ksEntity> entities)
    {
        var list = new List<ksEntity> { };
        foreach (ksEntity entity in entities)
        {
            // Check if it is a Unit, that is on the same team, that is not controlled.
            if (entity.GameObject.TryGetComponent(out Unit unit))
            {
                if (CheckTeam(player, entity))
                {
                    if (entity.Properties[Prop.CONTROLLEDPLAYERID] == "")
                    {
                        list.Add(entity);
                    }
                }
            }
        }
        return list;
    }

    public static bool CheckTeam(ksPlayer player, ksEntity entity ) { return entity.Properties[Prop.TEAMID].Int == player.Properties[Prop.TEAMID].Int; }

    public static ksEntity FindClosestEntity(ksEntity entity, List<ksEntity> entities)
    {
        float closestDistance = 999999f;
        ksEntity closestEntity = null;
        foreach (ksEntity e in entities)
        {
            if (!IsEntityValid(e))
            {
                print("FindClosestEntity a entity passed in is not valid");
                continue;
            }
            Vector2 diff = entity.Position2D - e.Position2D;
            float distance = diff.magnitude;
            if (distance < closestDistance)
            {
                closestEntity = e;
                closestDistance = distance;
            }
        }
        if (closestDistance < 9999f)
        {
            return closestEntity;
        }
        else
        {
            return null;
        }
    }
}
