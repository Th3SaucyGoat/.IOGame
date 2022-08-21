using System.Collections.Generic;
using UnityEngine;
using KS.Reactor;
using KS.Reactor.Client.Unity;
using TMPro;
using UnityEngine.UI;

// Connect to a Reactor server instance.
public class Connect : MonoBehaviour
{
    // Public property used to enable connections to local or live servers.
    public bool UseLocalServer = true;

    public string UserName;
    // Current room.
    private ksRoom m_room;

    [SerializeField]
    private GameObject walls;
    private bool firstGameDone = false;  // Deals with issue of reInstantiating the walls

    private void Start()
    {
        ksReactor.InputManager.BindAxis(Axes.X, "Horizontal");
        ksReactor.InputManager.BindAxis(Axes.Y, "Vertical");
        ksReactor.InputManager.BindButton(Buttons.SpawnCollector, "Fire3");
        GameEvents.current.Disconnected += matchLeft;
    }

    // Run when the game starts.
    public void StartMatchmaking()
    {
        if (m_room != null)
        {
            return;
        }

        // If we are using a local server, then fetch room information from the ksRoomType component. Otherwise use the
        // ksReactor service to request a list of live servers.
        if (UseLocalServer)
        {
            ConnectToServer(GetComponent<ksRoomType>().GetRoomInfo());
        }
        else
        {
            ksReactor.GetServers(OnGetServers);
        }
    }

    // Handle the response to a ksReactor.GetServers request.
    private void OnGetServers(List<ksRoomInfo> roomList, string error)
    {
        // Write errors to the logs.
        if (error != null)
        {
            ksLog.Error("Error fetching servers. " + error);
            return;
        }
        if (roomList != null && roomList.Count > 0)
        {
            List<ksRoomInfo> viableRooms = new List<ksRoomInfo> { };

            // Check if rooms are not current matches
            foreach (ksRoomInfo room in roomList)
            {
                print(room.PublicTags);
                if (!room.PublicTags.Contains("InMatch"))
                {
                    viableRooms.Add(room);
                }
            }
            if (viableRooms.Count > 0)
            {
                ConnectToServer(viableRooms[0]);
            }
            else
            {
                // Let user know there is a match in progress that needs to finish first
                print("Match in progress");

            }
        }
        else
        {
            ksLog.Warning("No servers found.");
            print("No servers found");
        }



    }

    // Connect to a server whose location is described in a ksRoomInfo object.
    private void ConnectToServer(ksRoomInfo roomInfo)
    {
        m_room = new ksRoom(roomInfo);

        // Register an event handler that will be called when a connection attempt completes.
        m_room.OnConnect += HandleConnect;

        // Register an event handler that will be called when a connected room disconnects.
        m_room.OnDisconnect += HandleDisconnect;

        if (UserName == "")
        {
            UserName = "NoName";
        }

        m_room.Connect(UserName);
    }

    // Handle a ksRoom connect event.
    private void HandleConnect(ksRoom.ConnectError status, uint customStatus)
    {
        if (status == ksRoom.ConnectError.CONNECTED)
        {
            ksLog.Info("Connected to " + m_room);
            GameEvents.current.RoomFound?.Invoke();
        }
        else
        {
            ksLog.Error(this, "Unable to connect to " + m_room + " (" + status + ", " + customStatus + ")");
            m_room.CleanUp();
            Destroy(m_room.GameObject);
            m_room = null;
            GameEvents.current.Disconnected?.Invoke();

        }
    }

    // Handle a ksRoom disconnect event.
    private void HandleDisconnect(ksRoom.DisconnectError status)
    {
        ksLog.Info("Disconnected from " + m_room + " (" + status + ")");
        m_room.CleanUp();
        Destroy(m_room.GameObject);
        m_room = null;
    }

    private void matchLeft()
    {
        m_room.Disconnect();
        if (!firstGameDone)
        {
            firstGameDone = true;
            // Instantiate(walls);   Delete? Might have fixed with making walls isPermanent again.
        }
    }

    public void setUsername(TMP_InputField input)
    {
        UserName = input.text;
    }
}