/*
KINEMATICSOUP CONFIDENTIAL
 Copyright(c) 2014-2022 KinematicSoup Technologies Incorporated 
 All Rights Reserved.

NOTICE:  All information contained herein is, and remains the property of 
KinematicSoup Technologies Incorporated and its suppliers, if any. The 
intellectual and technical concepts contained herein are proprietary to 
KinematicSoup Technologies Incorporated and its suppliers and may be covered by
U.S. and Foreign Patents, patents in process, and are protected by trade secret
or copyright law. Dissemination of this information or reproduction of this
material is strictly forbidden unless prior written permission is obtained from
KinematicSoup Technologies Incorporated.
*/

using System;
using UnityEngine;

namespace KS.Reactor.Client.Unity
{
    /// <summary>
    /// Room types hold room configuration data. Room and player scripts can be attached to game objects with room types.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu(ksMenuNames.REACTOR + "ksRoomType")]
    public class ksRoomType : MonoBehaviour
    {
        /// <summary>
        /// Checks if a local server is running for a scene and room type.
        /// </summary>
        /// <param name="scene">scene to check for</param>
        /// <param name="roomType">roomType to check for</param>
        /// <returns>true if a local server is running the room type.</returns>
        public delegate bool LocalServerChecker(string scene, string roomType);

        /// <summary>
        /// Delegate for checking if a local server is running.
        /// </summary>
        public static LocalServerChecker LocalServerRunningChecker
        {
            get { return m_localServerChecker; }
            set { m_localServerChecker = value; }
        }
        private static LocalServerChecker m_localServerChecker;


        /// <summary>
        /// Network updates sent from the server to all clients per second
        /// </summary>
        public int NetworkSyncRate
        {
            get { return m_networkSyncRate; }
            set { m_networkSyncRate = value; }
        }
        [SerializeField]
        [Tooltip("Number of server updates sent to all clients every second.")]
        private int m_networkSyncRate = 30;


        /// <summary>
        /// Try to recover lost update time
        /// </summary>
        public bool RecoverUpdateTime
        {
            get { return m_recoverUpdateTime; }
            set { m_recoverUpdateTime = value; }
        }
        [SerializeField]
        [Tooltip("Throttle updates to match elapsed real world times to elapsed simulation time.")]
        private bool m_recoverUpdateTime = true;


        /// <summary>
        /// Apply time scaling received on server frames to the Unity time.
        /// </summary>
        public bool ApplyServerTimeScale
        {
            get { return m_applyServerTimeScale; }
            set { m_applyServerTimeScale = value; }
        }
        [SerializeField]
        [Tooltip("Apply time scaling received on server frames to the Unity time.")]
        private bool m_applyServerTimeScale = true;


        /// <summary>
        /// Player limit
        /// </summary>
        public uint PlayerLimit
        {
            get { return m_playerLimit; }
            set { m_playerLimit = value; }
        }
        [SerializeField]
        [Tooltip("Number of connections the server will accept before blocking connections. Use 0 for unlimited.")]
        private uint m_playerLimit = 0;

        [Tooltip("Override the precision used for all entities when syncing transform data from servers to clients.")]
        public ksTransformPrecisions TransformPrecisionOverrides = new ksTransformPrecisions();

        /// <summary>
        /// Number of seconds between cluster connection attempts.
        /// </summary>
        public byte ClusterReconnectDelay
        {
            get { return m_clusterReconnectDelay; }
            set { m_clusterReconnectDelay = value; }
        }
        [SerializeField]
        [Tooltip("Number of seconds between cluster connection attempts.")]
        private byte m_clusterReconnectDelay = 10;

        [SerializeField]
        [Tooltip("If true, shutsdown the room if it loses its cluster connection and cannot reconnect.")]
        private bool m_shutdownWithoutCluster;

        /// <summary>
        /// Number of failed cluster connection attempts to make before shuttingdown. < 0: unlimited
        /// </summary>
        public int ClusterReconnectAttempts
        {
            get { return m_shutdownWithoutCluster ? m_clusterReconnectAttempts : -1; }
            set 
            {
                if (value < 0)
                {
                    m_shutdownWithoutCluster = false;
                }
                else
                {
                    m_shutdownWithoutCluster = true;
                    m_clusterReconnectAttempts = (byte)Math.Min(value, 255);
                }
            }
        }
        [SerializeField]
        [Tooltip("Number of failed cluster connection attempts to make before shuttingdown.")]
        private byte m_clusterReconnectAttempts = 3;

        /// <summary>
        /// Port to use for local server.
        /// </summary>
        public ushort LocalServerPort
        {
            get { return m_port; }
            set { m_port = value; }
        }
        [Header("Local Server Testing")]
        [SerializeField]
        [Tooltip("Port used when starting a local server.")]
        private ushort m_port = 8000;

        /// <summary>
        /// Gets room info for connecting to a room of this type on localhost.
        /// </summary>
        /// <returns>Connection info</returns>
        public ksRoomInfo GetRoomInfo()
        {
            ksRoomInfo roomInfo = new ksRoomInfo();
            roomInfo.Port = m_port;
            roomInfo.Type = gameObject.name;
            roomInfo.Scene = gameObject.scene.name;
            return roomInfo;
        }

        /// <summary>
        /// Checks if a local server is running this room type. This can only detect local servers started through
        /// Unity and can only be used in the editor.
        /// </summary>
        /// <returns>true if a local server is running this room type.</returns>
        public bool IsLocalServerRunning()
        {
            return m_localServerChecker == null ? false : m_localServerChecker(gameObject.scene.name, gameObject.name);
        }
    }
}
