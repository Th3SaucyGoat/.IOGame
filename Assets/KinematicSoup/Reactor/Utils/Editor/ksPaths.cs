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

using UnityEngine;
using UnityEditor;

namespace KS.Reactor.Client.Unity.Editor
{
    /// <summary>Provides static access to paths used throughout the project.</summary>
    [InitializeOnLoad]
    public static class ksPaths
    {
        private static string LOG_CHANNEL = typeof(ksPaths).ToString();

        private const string REACTOR_SCRIPTS = "Assets/ReactorScripts/";
        private const string SERVER_SCRIPTS = "Editor/Server/";
        private const string CLIENT_SCRIPTS = "Client/";
        private const string COMMON_SCRIPTS = "Common/";
        private const string PROXIE_SCRIPTS = "Proxies/";

        private static string m_projectRoot;
        private static string m_reactorRoot = null;

        /// <summary>Static intialization</summary>
        static ksPaths()
        {
            // Remove "Assets" from the end.
            m_projectRoot = Application.dataPath.Substring(0, Application.dataPath.Length - 6);
        }

        /// <summary>
        /// Detects the Reactor folder by querying Unity's asset database for the location of the ReactorRoot script.
        /// </summary>
        private static void FindReactorRoot()
        {
            string scriptName = "ReactorRoot";
            ScriptableObject script = ScriptableObject.CreateInstance(scriptName);
            if (script == null)
            {
                UseDefaultReactorFolder("Unable to load script " + scriptName);
                return;
            }
            string path = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(script));
            ScriptableObject.DestroyImmediate(script);
            if (path == null)
            {
                UseDefaultReactorFolder("Unable to get asset path for " + scriptName);
                return;
            }
            path = path.Replace('\\', '/');
            if (!path.StartsWith("Assets/"))
            {
                UseDefaultReactorFolder(scriptName + " asset path does not start with Assets/");
                return;
            }
            m_reactorRoot = path.Substring(0, path.LastIndexOf('/') + 1);
        }

        /// <summary>Root location of Reactor assets.</summary>
        public static string ReactorRoot
        {
            get
            {
                if (m_reactorRoot == null)
                {
                    FindReactorRoot();
                }
                return m_reactorRoot;
            }
        }

        /// <summary>Root of the Unity project.</summary>
        public static string ProjectRoot
        {
            get { return m_projectRoot; }
        }

        /// <summary>Location of textures.</summary>
        public static string Textures
        {
            get { return ReactorRoot + "UI/Editor/Icons/"; }
        }

        /// <summary>Folder containing developer Reactor scripts.</summary>
        public static string ReactorScripts
        {
            get { return REACTOR_SCRIPTS; }
        }

        /// <summary>Asset storing asset ids for Unity assets used as proxy assets.</summary>
        public static string AssetIdData
        {
            get { return REACTOR_SCRIPTS + "ksAssetIds.asset"; }
        }

        /// <summary>Reactor config file.</summary>
        public static string Config
        {
            get { return REACTOR_SCRIPTS + "Resources/ksReactorConfig.asset"; }
        }

        /// <summary>Gets the path to the file mapping IDs to assets for an asset bundle.</summary>
        /// <param name="assetBundle">Name to get file for, or null or empty string for resource assets.</param>
        /// <returns>Path to asset data file.</returns>
        public static string GetAssetDataPath(string assetBundle)
        {
            if (string.IsNullOrEmpty(assetBundle))
            {
                return REACTOR_SCRIPTS + "Resources/ksAssets.txt";
            }
            // File name must match ksPrefabCache.cs
            return AssetBundles + assetBundle + "/ksAssets.txt";
        }

        /// <summary>Path to folder containing entity data files for asset bundles.</summary>
        public static string AssetBundles
        {
            get { return REACTOR_SCRIPTS + "AssetBundles/"; }
        }

        /// <summary>Server runtime folder.</summary>
        public static string ServerRuntime
        {
            get { return REACTOR_SCRIPTS + SERVER_SCRIPTS; }
        }

        /// <summary>Server runtime project.</summary>
        public static string ServerRuntimeProject
        {
            get { return ServerRuntime + "KSServerRuntime.csproj"; }
        }

        /// <summary>Server runtime solution.</summary>
        public static string ServerRuntimeSolution
        {
            get { return ServerRuntime + "KSServerRuntime.sln"; }
        }

        /// <summary>Local server runtime dll.</summary>
        public static string LocalServerRuntimeDll
        {
            get { return BuildDir + "KSServerRuntime.Local.dll"; }
        }

        /// <summary>Server runtime dll.</summary>
        public static string ServerRuntimeDll
        {
            get { return BuildDir + "KSServerRuntime.dll"; }
        }

        /// <summary>Default location for client scripts.</summary>
        public static string ClientScripts
        {
            get { return REACTOR_SCRIPTS + CLIENT_SCRIPTS; }
        }

        /// <summary>Default location for server proxy scripts.</summary>
        public static string Proxies
        {
            get { return REACTOR_SCRIPTS + PROXIE_SCRIPTS; }
        }

        /// <summary>Location of temporary proxy meta files.</summary>
        public static string TempMetafiles
        {
            get { return Proxies + ".Temp/"; }
        }

        /// <summary>Default location for common scripts.</summary>
        public static string CommonScripts
        {
            get { return REACTOR_SCRIPTS + COMMON_SCRIPTS; }
        }

        /// <summary>Reactor build directory.</summary>
        public static string BuildDir
        {
            get { return ksReactorConfig.Instance.Server.ServerPath + "projects/" + Application.productName + "/"; }
        }

        /// <summary>Reactor cluster directory.</summary>
        public static string ClusterDir
        {
            get { return ksReactorConfig.Instance.Server.ServerPath + "cluster/"; }
        }

        /// <summary>Reactor log directory.</summary>
        public static string LogDir
        {
            get { return BuildDir + "logs/"; }
        }

        /// <summary>Game config file.</summary>
        public static string GameConfig
        {
            get { return BuildDir + "config.json"; }
        }

        /// <summary>Reactor scene config file for the active scene.</summary>
        /// <param name="scene">Scene name</param>
        /// <returns>Scene config file path.</returns>
        public static string SceneConfigFile(string scene)
        {
            return BuildDir + "config." + scene + ".json";
        }

        /// <summary>Reactor scene geometry file for the active scene.</summary>
        /// <param name="scene">Scene name</param>
        /// <returns>Scene geometry file path.</returns>
        public static string SceneGeometryFile(string scene)
        {
            return BuildDir + "geometry." + scene + ".dat";
        }

        /// <summary>Reactor geometry data file.</summary>
        public static string GeometryFile
        {
            get { return BuildDir + "geometry.dat"; }
        }

        /// <summary>Current operating system.</summary>
        public static string OperatingSystem
        {
            get
            {
                if (m_operatingSystem == null)
                {
                    string osstring = System.Environment.OSVersion.ToString();
                    string[] split = osstring.Split(' ');
                    m_operatingSystem = split[0];
                    return split[0];
                }
                else
                {
                    return m_operatingSystem;
                }
            }
        }
        private static string m_operatingSystem = null;

        /// <summary>Converts a prefab path to an entity type.</summary>
        /// <param name="assetPath">Asset path to prefab.</param>
        /// <param name="assetBundle">
        /// AssetBundle the prefab belongs to, or null or empty string if it is not part of an AssetBundle.
        /// </param>
        /// <returns>Entity type</returns>
        public static string GetEntityType(string assetPath, string assetBundle = null)
        {
            assetPath = assetPath.Replace("\\", "/");
            if (string.IsNullOrEmpty(assetBundle))
            {
                string resourcesStr = "/Resources/";
                int index = assetPath.IndexOf(resourcesStr);
                if (index < 0)
                {
                    return "";
                }
                assetPath = assetPath.Substring(index + resourcesStr.Length);
            }
            else
            {
                int index = assetPath.LastIndexOf("/");
                assetPath = assetBundle + ":" + assetPath.Substring(index + 1);
            }
            string ext = ".prefab";
            if (assetPath.EndsWith(ext))
            {
                assetPath = assetPath.Substring(0, assetPath.Length - ext.Length);
            }
            return assetPath;
        }

        /// <summary>Sets the Reactor folder to the default and logs an error message.</summary>
        /// <param name="errorMessage"></param>
        private static void UseDefaultReactorFolder(string errorMessage)
        {
            m_reactorRoot = "Assets/KinematicSoup/Reactor/";
            ksLog.Error(LOG_CHANNEL, "Error detecting Reactor folder: " + errorMessage +
                ". Setting Reactor folder to " + m_reactorRoot);
        }
    }
}