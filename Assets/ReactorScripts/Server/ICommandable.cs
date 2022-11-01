using System.Collections;
using System.Collections.Generic;
using KS.Reactor;
using KS.Reactor.Server;


public interface ICommandable
{
    ksIServerEntity EntityToFollow { set; get; }
    ksIServerEntity Hivemind { set; get; }

    void DetermineState();
}
