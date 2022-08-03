using System.Collections.Generic;
using UnityEngine;

namespace KS.Reactor.Client.Unity.Samples
{
    /// <summary>
    /// Sample connection component for use with Reactor.
    /// Attach this component to a gameobject with a ksRoomType component. When the game begins the start method will 
    /// begin the process of acquiring a player session and connecting to the first available room.
    /// Use UsePlayerAPI to enable player registration.This requires the player API to be configured in the Reactor settings.
    /// Use ConnectionType, Host, and Port to connect to online sessions or directly to a hosted room.
    /// </summary>
    public class ksSampleConnect : MonoBehaviour
    {
        /// <summary>Methods used to establish connections to a server room.</summary>
        public enum ServerConnectionMethods
        {
            /// <summary>Use the Reactor API to find and connect to a public server.</summary>
            ONLINE,
            /// <summary>Connect directly to a server using a host and port</summary>
            DIRECT
        };
        /// <summary>Selected connection method</summary>
        public ServerConnectionMethods ConnectionMethod = ServerConnectionMethods.DIRECT;
        /// <summary>Should connections use the ksPlayer API. If true then a DeviceID session will be assigned to players</summary>
        public bool UsePlayerAPI = false;
        /// <summary>Host to use when <see cref="ConnectionMethod"/> = <see cref="ServerConnectionMethods.DIRECT"/></summary>
        public string Host = "localhost";
        /// <summary>Port to use when <see cref="ConnectionMethod"/> = <see cref="ServerConnectionMethods.DIRECT"/></summary>
        public ushort Port = 8000;

        private ksPlayerAPI.Session m_session = null;
        private ksRoom m_room = null;

        /// <summary>Start is called before the first frame update</summary>
        void Start()
        {
            BeginConnect();
        }

        /// <summary>
        /// Starts the room connection process. 
        /// If a ksPlayer is required then attempt to load a cached session.If a ksPlayer is not required, then skip ahead and get a room list.
        /// </summary>
        public void BeginConnect()
        {
            if (UsePlayerAPI)
            {
                ksLog.Info("Loading Session...");
                ksReactor.PlayerAPI.LoadSession(OnLoadPlayerSession);
            }
            else
            {
                GetRooms();
            }
        }

        /// <summary>
        /// Handle the response from a ksReactor.PlayerAPI.LoadSession call.
        /// If a session was not returned request a new session using a deviceID.If a session was returned, then get a list of rooms.
        /// </summary>
        /// <param name="error">Description of a Reactor web API service error.</param>
        /// <param name="session">Player session, will be null if the call to the player API load session failed.</param>
        /// <param name="asyncState">User state object provided to a LoadSession call.</param>
        private void OnLoadPlayerSession(string error, ksPlayerAPI.Session session, object asyncState)
        {
            if (!string.IsNullOrEmpty(error))
            {
                ksLog.Warning(this, "Error establishing player session. " + error);
            }

            if (session == null)
            {
                ksLog.Info("Creating new session...");
                ksReactor.PlayerAPI.LoginWithDevice(OnPlayerLogin);
            }
            else
            {
                m_session = session;
                GetRooms();
            }
        }

        /// <summary>
        /// Handle the response from a ksReactor.PlayerAPI.LoginWithDevice call.
        /// If a session was returned then cache the session and get a list of rooms.
        /// </summary>
        /// <param name="error">Description of a Reactor web API service error.</param>
        /// <param name="session">Player session, will be null if the call to the player API login failed.</param>
        /// <param name="asyncState">User state object provided to a LoadSession call.</param>
        private void OnPlayerLogin(string error, ksPlayerAPI.Session session, object asyncState)
        {
            if (!string.IsNullOrEmpty(error))
            {
                ksLog.Error(this, "Error establishing player session. " + error);
                return;
            }

            m_session = session;
            ksLog.Info("Saving Session...");
            ksReactor.PlayerAPI.SaveSession(m_session);
            GetRooms();
        }

        /// <summary>
        /// Get a list of rooms that this project is allowed to connect to.
        /// If the connection type is ONLINE then make an API request for a list of rooms.If a direct connection is required then
        /// use the host and port properties to create a room info connection object and then attempt to connect to the room.
        /// </summary>
        private void GetRooms()
        {
            if (ConnectionMethod == ServerConnectionMethods.ONLINE)
            {
                ksLog.Info("Get Rooms...");
                ksReactor.GetServers(OnGetRooms);
            }
            else if (ConnectionMethod == ServerConnectionMethods.DIRECT)
            {
                ksRoomInfo roomInfo = GetComponent<ksRoomType>().GetRoomInfo();
                roomInfo.Host = Host;
                roomInfo.Port = Port;
                ConnectToRoom(roomInfo);
            }
        }

        /// <summary>
        /// Handle the response from a ksReactor.GetServers call.
        /// Check for errors and a non empty room list before connecting to the first room in the list.
        /// </summary>
        /// <param name="rooms">List of available rooms</param>
        /// <param name="error">Description of a Reactor web API service error.</param>
        private void OnGetRooms(List<ksRoomInfo> rooms, string error)
        {
            if (!string.IsNullOrEmpty(error))
            {
                ksLog.Error(this, "Error fetching room list. " + error);
                return;
            }

            if (rooms == null || rooms.Count == 0)
            {
                ksLog.Warning(this, "No rooms available");
                return;
            }

            ksRoomInfo selectedRoom = rooms[0];
#if UNITY_WEBGL && !UNITY_EDITOR
            // WebGL rooms connect securely using websockets on port 451
            selectedRoom.Port = 451;
#endif
            ConnectToRoom(selectedRoom);
        }

        /// <summary>
        /// Create and connect to a new room defined in roomInfo.
        /// Registers an OnConnect handler, OnDisconnect handler, and uses a connection function depending on the 
        /// m_session state.
        /// </summary>
        /// <param name="roomInfo">Information needed to connect to a server room.</param>
        private void ConnectToRoom(ksRoomInfo roomInfo)
        {
            m_room = new ksRoom(roomInfo);
            m_room.OnConnect += OnConnect;
            m_room.OnDisconnect += OnDisconnect;

#if UNITY_WEBGL && !UNITY_EDITOR
            ksWSConnection.Config.Secure = ConnectionMethod == ServerConnectionMethods.ONLINE;
            m_room.Protocol = ksConnection.Protocols.WEBSOCKETS;
#endif

            if (m_session != null)
            {
                m_room.Connect(m_session);
            }
            else
            {
                m_room.Connect();
            }
        }

        /// <summary>Log the result of a connect attempt.</summary>
        /// <param name="status">Connection status.</param>
        /// <param name="customStatus">Custom server authentication failure result set when status = AUTH_ERR_USER_DEFINED</param>
        private void OnConnect(ksBaseRoom.ConnectError status, uint customStatus)
        {
            if (status == ksBaseRoom.ConnectError.CONNECTED)
            {
                ksLog.Info("Connected to " + m_room);
            }
            else
            {
                ksLog.Error(this, "Unable to connect to " + m_room + ". Status = " + status + "(" + customStatus + ")");
            }
        }

        /// <summary>Log a disconnect event.</summary>
        /// <param name="status">Connection status.</param>
        private void OnDisconnect(ksBaseRoom.DisconnectError status)
        {
            ksLog.Info("Disconnected from " + m_room + ". Status = " + status);
        }
    }
}
