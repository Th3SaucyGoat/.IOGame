using System.Collections;
using System.Collections.Generic;
using KS.Reactor.Server;
using KS.Reactor;

    public class ServerUtils
{
    public static bool CheckTeam(ksIServerEntity entity1, ksIServerEntity entity2)
    {
        return entity1.Properties[Prop.TEAMID].Int == entity2.Properties[Prop.TEAMID].Int;
    }

    public static bool CheckTeam(ksIServerPlayer entity1, ksIServerEntity entity2)
    {
        return entity1.Properties[Prop.TEAMID].Int == entity2.Properties[Prop.TEAMID].Int;
    }

    public static bool IsTargetValid(ksIServerEntity targ) { return !(targ == null || targ.IsDestroyed); }

    public static float DistanceTo(ksIServerEntity ours, ksIServerEntity other)
    {
        ksVector2 pos = ours.Position2D - other.Position2D;
        return pos.Magnitude();
    }

    public static List<ksIServerEntity> UpdateEntityList(List<ksIServerEntity> entities)
    {
        var updatedList = new List<ksIServerEntity> { };
        foreach (ksIServerEntity entity in entities)
        {
            if(IsTargetValid(entity))
            {
                updatedList.Add(entity);
            }
        }
        return updatedList;
    }

    public static ksIServerEntity FindClosestEntity(ksIServerEntity ourEntity, List<ksIServerEntity> entities)
    {
        float closestDistance = 999999f;
        ksIServerEntity closestEntity = null;
        foreach (ksIServerEntity entity in entities)
        {
            ksVector2 position = ourEntity.Position2D - entity.Position2D;
            float distance = position.Magnitude();
            if (distance < closestDistance)
            {
                closestEntity = entity;
                closestDistance = distance;
            }
        }
        return closestEntity;
    }
}





