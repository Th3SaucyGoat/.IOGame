using System.Collections;
using System.Collections.Generic;
using KS.Reactor.Server;

    public class Checks
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

}





