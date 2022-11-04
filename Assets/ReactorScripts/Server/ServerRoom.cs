using KS.Reactor;
using KS.Reactor.Server;
using System.Collections.Generic;
using Example;

public class ServerRoom : ksServerRoomScript
{

    private int num = 0;
    private float[] xBounds = new float[] { -5f, 5f };
    private float[] yBounds = new float[] { -5f, 5f };

    private ksRandom random;

    private Timer foodTimer;
    private Timer clusterfoodTimer;
    private Timer clusterfoodDelay;
    private Timer matchStartTimer;

    private int clusterNum;
    private int currentclusterNum;
    private ksVector2 clusterPos;


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

        foodTimer = new Timer(0.75f, SpawnTimerTimeout, true);
        clusterfoodTimer = new Timer(15.0f, ClusterSpawnFood, true);
        matchStartTimer = new Timer(1.0f, InitiateMatch, false);
        clusterfoodDelay = new Timer(0.1f, clusterfoodDelayTimeout, false);
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
            ResetRoom();
        }
    }
    private void Update()
    {
        foodTimer.Tick(Time.Delta);
        clusterfoodDelay.Tick(Time.Delta);
        clusterfoodTimer.Tick(Time.Delta);
        matchStartTimer.Tick(Time.Delta);
    }

    private void InitiateMatch()
    {
        Room.SpawnEntity("Organelle", ksVector2.Zero);
        Room.SpawnEntity("Organelle", FindRandomPosition());


        // Spawn in a hivemind for all players. Set player control to only their hivemind. Set unique team Ids.
        foreach (ksIServerPlayer p in Room.Players)
        {
            ksIServerEntity hivemind = Room.SpawnEntity("Hivemind", FindRandomPosition());
            activeTeams.Add(teamId);
            p.Properties[Prop.TEAMID] = teamId;
            p.Properties[Prop.HIVEMINDID] = hivemind.Id;
            hivemind.Properties[Prop.TEAMID] = teamId;
            hivemind.CallRPC(RPC.SENDINFO, teamId);
            // Set the hivemind to be controlled by the player id. I will need to adjust this for 4 players.
            PlayerRequestControl(p, hivemind.Id);
            teamId++;
        }

        // Initial food spawn in
        for (int i = 0; i < (25 * Room.ConnectedPlayerCount) + 50; i++)
        {
            SpawnFood(FindRandomPosition());
        }
        foodTimer.Start();
        clusterfoodTimer.Start();
    }

    // Handle Room Cleanup
    private void ResetRoom()
    {
        foreach (ksIServerEntity entity in Room.Entities)
        {
            if (entity.CollisionFilter.AssetName == "Obstacles") // Used to filter out isPermanent Obstacles
            {
                continue;
            }
            entity.Destroy();
        }
        foodTimer.Stop();
        clusterfoodTimer.Stop();
        Room.PublicTags.Remove("InMatch");
    }

[ksRPC(RPC.SETREADYSTATUS)]
    private void setPlayerReadyStatus(ksIServerPlayer player, bool readyStatus)
    {
        if (Room.PublicTags.Contains("InMatch"))
        {
            return;
        }
        // Find player label with the reference id. Set ready status to ready.
        player.Properties[Prop.READY] = readyStatus;
        Room.CallRPC(RPC.READYSTATUS, player.Id, readyStatus);
        ksConstList<ksIServerPlayer> connectedPlayers = Room.Players;
        bool initiateMatch = true;
        foreach (ksIServerPlayer p in connectedPlayers)
        {
            if (p.Properties[Prop.READY].Bool != true)
            {
                initiateMatch = false;
                break;
            }
        }
        if (initiateMatch && connectedPlayers.Count > 0)
        {
            // Start match countdown
            Room.PublicTags.Add("InMatch");
            matchStartTimer.Start();
            Room.CallRPC(RPC.STARTMATCH);
        }
    }

    private void SpawnTimerTimeout()
    {
        SpawnFood(FindRandomPosition());
    }

    private void SpawnFood(ksVector2 pos)
    {
        Room.SpawnEntity("Food", pos);
    }

    private void ClusterSpawnFood()
    {
        currentclusterNum = 0;
        clusterNum = random.Next(30, 50);
        clusterPos = FindRandomPosition();
        clusterfoodDelay.Start();
    }

    private void clusterfoodDelayTimeout()
    {
        //ksLog.Info("cluster food");
        float MaxRadius = 5f;
        // Find random angle and radius
        float angle = random.NextFloat(0, 360);
        float radius = random.NextFloat(0, MaxRadius);
        // Convert to position
        ksVector2 pos = new ksVector2(ksMath.Cos(angle) * radius, ksMath.Sin(angle) * radius) + clusterPos;
        // Spawn food at that position
        SpawnFood(pos);
        currentclusterNum++;
        // Create a slight delay between next while iteration
        if (currentclusterNum >= clusterNum)
        {
            clusterfoodDelay.Stop();
        }
        else
        {
            clusterfoodDelay.Start();
        }
    }

    private ksVector2 FindRandomPosition()
    {
        return new ksVector2(random.NextFloat(xBounds[0], xBounds[1]), random.NextFloat(yBounds[0], yBounds[1]));

    }

    [ksRPC(RPC.REQUESTCONTROL)]
    private void PlayerRequestControl(ksIServerPlayer p, uint entityId)
    {
        ksIServerEntity entity = Room.GetEntity(entityId);
        //ksLog.Info("Right Here "+ ServerUtils.CheckTeam(p, entity).ToString() + p.Properties[Prop.TEAMID].ToString() + " , " + entity.Properties[Prop.TEAMID].ToString());
        // Do same validity checks
        if (ServerUtils.CheckTeam(p, entity) && entity.Properties[Prop.CONTROLLEDPLAYERID] == "")
        {

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

    public void OnEntityDestroyed(ksIServerEntity entity)
    {

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
        List<ksIServerPlayer> allPlayers = new List<ksIServerPlayer> { };
        

            if (activeTeams.Count == 1)
        {
            // Send Victory RPC to those players
            players = new List<ksIServerPlayer> { };
            foreach (ksIServerPlayer player in Room.Players)
            {
                allPlayers.Add(player);
                if (player.Properties[Prop.TEAMID].Int == activeTeams[0])
                {
                    players.Add(player);
                    ksIServerEntity controlledEntity = Room.GetEntity(player.Properties[Prop.CONTROLLEDENTITYID]);
                    if (ServerUtils.IsTargetValid(controlledEntity))
                    {
                        controlledEntity.RemoveController();
                    }
                }
            }
            Room.CallRPC(players, RPC.ENDGAME, true);
            //foreach (ksIServerPlayer player in players)
            //{
            //    ksLog.Info("here" + player.Properties[Prop.NAME].ToString());
            //}
            Room.CallRPC(allPlayers, RPC.REDIRECT);
        }
        //else if (activeTeams.Count == 5) // Change back
        //{
        //    foreach (ksIServerPlayer player in Room.Players)
        //    {
        //        allPlayers.Add(player);
        //    }
        //Room.CallRPC(allPlayers, RPC.REDIRECT);
    }

}
