using KS.Reactor;
using KS.Reactor.Server;
using System.Collections.Generic;




public class ServerRoom : ksServerRoomScript
{

    private int num = 0;
    private float[] xBounds = new float[] { -8.54f, 12.73f };
    private float[] yBounds = new float[] { -6.17f , 7.04f  };

    private ksRandom random;
// Initialize the script. Called once when the script is loaded.
public override void Initialize()
    {
        // Register a event handler that will be called when a new player joins the server.
        Room.OnPlayerJoin += PlayerJoin;

        // Register a event handler that will be called when a player leaves the server.
        Room.OnPlayerLeave += PlayerLeave;

        // Register an update event to be called at every frame at order 0.
        Room.OnUpdate[0] += Update;

        random = new ksRandom();
        
    }

    // Cleanup the script. Called once when the script is unloaded.
    public override void Detached()
    {
        Room.OnPlayerJoin -= PlayerJoin;
        Room.OnPlayerLeave -= PlayerLeave;
        Room.OnUpdate[0] -= Update;
    }

    public override uint Authenticate(ksIServerPlayer player, ksMultiType[] args)
    {
        // args contains the arguments we passed to ksRoom.Connect().
        if (args.Length != 1)
        {
            // Returning a non-zero value means authentication failed.
            return 1;
        }
        // Set the name to the value provided by the client. You can do your own validation here.
        player.Properties[Prop.NAME] = args[0];
        return 0;
    }
    // Handle player join events.
    private void PlayerJoin(ksIServerPlayer player)
    {
        player.Properties[Prop.READY] = false;
        ksLog.Info("Player " + player.Properties[Prop.READY] + " joined");


    }

    // Handle player leave events.
    private void PlayerLeave(ksIServerPlayer player)
    {
        ksLog.Info("Player " + player.Id + " left");
  
    
    }
    private void Update()
    {

    }

    [ksRPC(RPC.SETREADYSTATUS)]
    private void setPlayerReadyStatus(ksIServerPlayer player, bool readyStatus)
    {
        // Find player label with the reference id. Set ready status to ready.
        ksLog.Info("RPC RECEIVED " + readyStatus.ToString() + " and " + player.Id.ToString());
        ksLog.Info("Hehe" + readyStatus.ToString());
        player.Properties[Prop.READY] = readyStatus;
        Room.CallRPC(RPC.READYSTATUS, player.Id, readyStatus);
        ksConstList<ksIServerPlayer> connectedPlayers = Room.Players;
        bool initiateMatch = true;
        foreach (ksIServerPlayer p in connectedPlayers)
        {
            if (!p.Properties[Prop.READY])
            {
                initiateMatch = false;
                break;
            }
        }
        if (initiateMatch && connectedPlayers.Count > 0)
        {
            // Start match countdown
            ksLog.Info("Start Match!");
            // Send RPC telling all clients to start match. UI Changes
            // Spawn in a hivemind for all players. Set player control to only their hivemind.
            foreach (ksIServerPlayer p in Room.Players)
            {
                ksIServerEntity hivemind = Room.SpawnEntity("Hivemind", new ksVector2(10f, -5f));
                hivemind.SetController(new UnitController(), p);
                p.Properties[Prop.HIVEMINDID] = hivemind.Id;
                // Send player username and id to this entity to all clients.
                hivemind.CallRPC(RPC.SENDINFO, new ksMultiType[] { p.Properties[Prop.NAME], p.Id });
            }


            for (int i = 0; i < 50; i++)
            {
                ksVector3 pos = new ksVector3(random.NextFloat(xBounds[0], xBounds[1]), random.NextFloat(yBounds[0], yBounds[1]), 0f);

                Room.SpawnEntity("Food", pos);

            }
        }


    }




}