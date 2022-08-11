using KS.Reactor;
using KS.Reactor.Server;
using System.Collections.Generic;
using Example;

public class ServerRoom : ksServerRoomScript
{

    private int num = 0;
    private float[] xBounds = new float[] { -8.54f, 12.73f };
    private float[] yBounds = new float[] { -6.17f, 7.04f };

    private ksRandom random;
    private Timer foodTimer;
    private delegate void OnTimerEnd();

    private int teamId = 1;

    private List<int> activeTeams = new List<int> { };

    //private Dictionary<uint, List<uint>> hivemindPlayers = new Dictionary<uint, List<uint>> { };

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

        foodTimer = new Timer(1.0f, SpawnFood, false);
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
        if (Room.ConnectedPlayerCount == 0)
        {
            // Handle Room Cleanup
            foreach (ksIServerEntity entity in Room.Entities)
            {
                entity.Destroy();
            }
            foodTimer.Stop();
        }
    }
    private void Update()
    {
        foodTimer.Tick(Time.Delta);
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
            // Spawn in a hivemind for all players. Set player control to only their hivemind. Set unique team Ids.
            foreach (ksIServerPlayer p in Room.Players)
            {
                ksIServerEntity hivemind = Room.SpawnEntity("Hivemind", new ksVector2(10f, -5f));
                IMovement movementValues = hivemind.Scripts.Get<IMovement>();
                hivemind.SetController(new UnitController(movementValues.Speed, movementValues.Acceleration), p);
                hivemind.Properties[Prop.TEAMID] = teamId;
                activeTeams.Add(teamId);
                Room.OnDestroyEntity += OnEntityDestroyed;
                //ksLog.Info("THE ID = " + hivemind.Id.ToString());
                p.Properties[Prop.TEAMID] = teamId;
                p.Properties[Prop.HIVEMINDID] = hivemind.Id;
                p.Properties[Prop.CONTROLLEDENTITYID] = hivemind.Id;
                // Set the hivemind to be controlled by the player id. I will need to adjust this for 4 players.
                hivemind.Properties[Prop.CONTROLLEDPLAYERID] = p.Id;
                teamId++;
                ksLog.Info(hivemind.Id.ToString());

                // Send RPC telling all clients to start match. UI Changes. Send Username for hiveminds
                hivemind.CallRPC(RPC.SENDINFO, new ksMultiType[] { p.Properties[Prop.NAME], p.Id });
                // Send player username and id to this entity to all clients.
            }
            Room.CallRPC(RPC.STARTMATCH);

            // Initial food spawn in
            for (int i = 0; i < 50; i++)
            {
                SpawnFood();
            }
            foodTimer.Start();
        }
    }



    private void SpawnFood()
    {
        ksVector3 pos = new ksVector3(random.NextFloat(xBounds[0], xBounds[1]), random.NextFloat(yBounds[0], yBounds[1]), 0f);
        Room.SpawnEntity("Food", pos);
        foodTimer.Start();
    }

    [ksRPC(RPC.REQUESTCONTROL)]
    private void PlayerRequestControl(ksIServerPlayer p, uint entityId)
    {
        //ksLog.Info(Room.GetEntity(entityId).ToString());
        ksLog.Info(Room.ToString());
        ksIServerEntity entity = Room.GetEntity(entityId);
        // Do same validity checks
        if (ServerUtils.CheckTeam(p, entity) && entity.Properties[Prop.CONTROLLEDPLAYERID] == "")
        {
            ksLog.Info("Request to take control of unit passed");

            // Handle the previous entity
            if (p.Properties[Prop.CONTROLLEDENTITYID] != "")
            {
                ksIServerEntity previousEntity = Room.GetEntity(p.Properties[Prop.CONTROLLEDENTITYID]);
                previousEntity.Properties[Prop.CONTROLLEDPLAYERID] = "";
                previousEntity.RemoveController();
                previousEntity.Scripts.Get<ICommandable>().DetermineState();
            }
            p.Properties[Prop.CONTROLLEDENTITYID] = entityId;
            entity.Properties[Prop.CONTROLLEDPLAYERID] = p.Id;
            // Add unit controller to new entity.
            IMovement movementValues = entity.Scripts.Get<IMovement>();
            entity.Scripts.Get<ICommandable>().DetermineState();
            entity.SetController(new UnitController(movementValues.Speed, movementValues.Acceleration), p);

            // Send RPC to inform others. Pass Id which can be used to reference the player's name.
            entity.CallRPC(RPC.TAKECONTROL, p.Id);
        }
    }

    private void OnEntityDestroyed(ksIServerEntity entity)
    {
        if (entity.Type != "Hivemind")
        {
            return;
        }
        // Access the players on this hivemind's team. 
        // Set Hivemind id to empty, send rpc to all those players
        List<ksIServerPlayer> players = new List<ksIServerPlayer> { };
        foreach (ksIServerPlayer player in Room.Players)
        {
            if (ServerUtils.CheckTeam(player, entity))
            {
                player.Properties[Prop.HIVEMINDID] = "";
                players.Add(player);
            }
        }
        Room.CallRPC(players, RPC.ENDGAME, false);
        // Destroy all entities on the same team.
        foreach (ksIServerEntity e in Room.Entities)
        {
            if (ServerUtils.CheckTeam(e, entity))
            {
                e.Destroy();
            }
        }
        activeTeams.Remove(entity.Properties[Prop.TEAMID]);
        if (activeTeams.Count == 1)
        {

            // Send Victory RPC to those players
            players = new List<ksIServerPlayer> { };
            foreach (ksIServerPlayer player in Room.Players)
            {
                if (player.Properties[Prop.TEAMID].Int == activeTeams[0])
                {
                    players.Add(player);
                }
            }
            Room.CallRPC(players, RPC.ENDGAME, true);
            foreach (ksIServerPlayer player in players)
            {
                ksLog.Info("here" + player.Properties[Prop.NAME].ToString());

            }
        }
    }
}