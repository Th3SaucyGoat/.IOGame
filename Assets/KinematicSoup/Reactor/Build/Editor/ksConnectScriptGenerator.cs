using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace KS.Reactor.Client.Unity.Editor
{
    public class ksConnectScriptGenerator
    {
        private const string TEMPLATE =
@"
using UnityEngine;
using KS.Reactor.Client.Unity
%USING%

%NAMESPACE_TEMPLATE%";


        private const string NAMESPACE_TEMPLATE =
@"namespace %NAMESPACE%
{
    %CLASS_TEMPLATE%
}";

        private const string CLASS_TEMPLATE =
@" % TAGS%
public class %NAME% : MonoBehaviour
{
        private ksRoom m_room; 

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

        private void OnConnect(ksBaseRoom.ConnectError status, uint customStatus)
        {
            if (status == ksBaseRoom.ConnectError.CONNECTED)
            {
                ksLog.Info($""Connected to {m_room}"");
            }
            else
            {
                ksLog.Error(this, $""Unable to connect to {m_room}. Status = {status}({customStatus})"");
                m_room.CleanUp();
                m_room = null;
            }
        }

        private void OnDisconnect(ksBaseRoom.DisconnectError status)
        {
            ksLog.Info($""Disconnected from {m_room}. Status = {status}"");
            m_room.CleanUp();
            m_room = null;
        }
    }";



    }
}
