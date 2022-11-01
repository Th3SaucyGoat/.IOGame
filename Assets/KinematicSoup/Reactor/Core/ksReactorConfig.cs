﻿/*
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

using KS.Unity;
using System;
using UnityEngine;

namespace KS.Reactor.Client.Unity
{
    /// <summary>Reactor configuration setttings.</summary>
    public class ksReactorConfig : ScriptableObject
    {
        private const string CONFIG_PATH = "ksReactorConfig";

        /// <summary>Web API URLs.</summary>
#if KS_DEVELOPMENT
        [Serializable]
#endif
        public class URLConfigs
        {
            /// <summary>Base URL for publishing Reactor images.</summary>
            public string Publishing = "https://publish.kinematicsoup.com:447/api";

            /// <summary>Base URL for player API calls.</summary>
            public string PlayerAPI = "https://players.kinematicsoup.com/api";

            /// <summary>Base URL for retrieving running public server instances.</summary>
            public string Instances = "https://instances.kinematicsoup.com/getInstances";
            
            /// <summary>Web console URL for account, project and instance managment.</summary>
            public string WebConsole = "https://console.kinematicsoup.com";
            
            /// <summary>Reactor API documentation, tutorials, and samples.</summary>
            public string Documentation = "https://docs.kinematicsoup.com";
            
            /// <summary>Email support address.</summary>
            public string SupportEmail = "support@kinematicsoup.com";
        };

        /// <summary>Local server settings.</summary>
        [Serializable]
        public class ServerConfigs
        {
            /// <summary>Server runtime defines.</summary>
            [Tooltip("Define symbols to use when compiling server runtime scripts.")]
            public string DefineSymbols = "";

            /// <summary>Location of the local Reactor server.</summary>
            [Tooltip("Location of the local Reactor server.")]
            [ksPathFinder(true, "Select Reactor Server Folder", true)]
            public string ServerPath = "";

            /// <summary>Path to the application used to build server runtimes.</summary>
            [Tooltip("Path the application used to build server runtimes.")]
            [ksPathFinder(false, "Select Build Tool")]
            public string BuildToolPath = "";

            /// <summary>Ammount of log files to keep.</summary>
            [Tooltip("Ammount of log files to keep.")]
            public int LogHistorySize = 5;

            /// <summary>Restart local server instances when publishing or building.</summary>
            [Tooltip("Restart local server instances after building ksServerRutime or publishing configs.")]
            public bool RestartOnUpdate = true;

            /// <summary>
            /// Automatically rebuild the KSServerRuntime dll when server script changes are detected and Unity
            /// rebuilds.
            /// </summary>
            [Tooltip("Automatically rebuild the KSServerRuntime dll when server script changes are detected and Unity rebuilds.")]
            public bool AutoRebuildServerRuntime = true;
        };

        /// <summary>Build settings.</summary>
        [Serializable]
        public class BuildConfigs
        {
            /// <summary>Last Reactor build version.</summary>
            public string LastBuildVersion = "0.10.1";

            /// <summary>Identification information for finding running public server instances.</summary>
            public string ImageBinding = "";

            /// <summary>Player API Key.</summary>
            public string APIKey = "";

            /// <summary>Player API secret for clients.</summary>
            public string APIClientSecret = "";

            /// <summary>Player API secret for servers.</summary>
            public string APIServerSecret = "";
        }

        /// <summary>Local cluster settings</summary>
        [Serializable]
        public class ClusterConfigs
        {
            /// <summary>Port used by local cluster instances.</summary>
            public ushort ClusterPort = 1234;

            /// <summary>Ports the cluster can assign to rooms. </summary>
            public string RoomPorts = "8000-8010";

            /// <summary>Scene the cluster uses when launching local servers instances.</summary>
            public string Scene = "";

            /// <summary>Rooms the cluster uses when launching local servers instances.</summary>
            public string Room = "";

            /// <summary>Tags assigned to server instances started by the cluster.</summary>
            public string Tags = "";

            /// <summary>Network protocol used by rooms launched by the cluster.</summary>
            public string Protocol = "tcp";
        }

        /// <summary>Debug settings.</summary>
        [Serializable]
        public class DebugConfigs
        {
            public Color DefaultServerGhostColor = new Color(0f, 1f, 0f, .5f);
            public Material DefaultServerGhostMaterial;
        }


#if !KS_DEVELOPMENT
        [NonSerialized]
#endif
        /// <summary>Reactor version.</summary>
        public string Version = "0.10.1";

        /// <summary>Reactor client release number for the current version.</summary>
        public string ClientVersion = "0";

        /// <summary>Web API URLs.</summary>
        public URLConfigs Urls = new URLConfigs();

        /// <summary>Local server settings.</summary>
        public ServerConfigs Server = new ServerConfigs();

        /// <summary>Build settings.</summary>
        public BuildConfigs Build = new BuildConfigs();

        /// <summary>Local cluster settings.</summary>
        public ClusterConfigs Cluster = new ClusterConfigs();

        /// <summary>Debug settings.</summary>
        public DebugConfigs Debug = new DebugConfigs();

        /// <summary>Names assigned to collision groups.</summary>
        public string[] CollisionGroups = new string[32] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10",
            "11", "12", "13", "14", "15", "16", "17", "18", "19", "20", "21", "22", "23", "24", "25", "26", "27",
            "28", "29", "30", "31"};


        /// <summary>
        /// Singleton instance. Load the config from the asset database if the instance is not initialized.
        /// </summary>
        public static ksReactorConfig Instance
        {
            get
            {
                if (m_instance == null)
                {
                    m_instance = Resources.Load<ksReactorConfig>(CONFIG_PATH);
                }
                return m_instance;
            }
        }
        private static ksReactorConfig m_instance;

        /// <summary>Assign the singleton instance to this instance when it is enabled.</summary>
        private void OnEnable()
        {
            hideFlags = HideFlags.None;
            if (m_instance == null)
            {
                m_instance = this;
            }
        }
    }
}
