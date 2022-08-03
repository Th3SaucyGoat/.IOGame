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
using System.IO;
using KS.Unity.Editor;

namespace KS.Reactor.Client.Unity.Editor
{
    /// <summary>This class is responsible for the file menu and context menu GUIs.</summary>
    public class ksReactorMenu
    {
        private const string LOG_CHANNEL = "KS.Unity.ReactorMenu";

        /// <summary>Opens Reactor settings window.</summary>
        [MenuItem(ksMenuNames.REACTOR + "Settings", false, ksStyle.WINDOW_GROUP)]
        public static void Init()
        {
            Selection.objects = new Object[] { ksReactorConfig.Instance };
            ksEditorUtils.FocusInspectorWindow();
        }

        /// <summary>Publishes the current scene online.</summary>
        [MenuItem(ksMenuNames.REACTOR + "Publishing", priority = ksStyle.WINDOW_GROUP)]
        static void Publish()
        {
            ksWindow.Open(ksWindow.REACTOR_PUBLISH, delegate (ksWindow window)
            {
                window.titleContent = new GUIContent(" Publish", ksTextures.Logo);
                window.minSize = new Vector2(380f, 100f);
                window.Menu = ScriptableObject.CreateInstance<ksPublishMenu>();
            });
        }

        /// <summary>Launches instances of a server.</summary>
        [MenuItem(ksMenuNames.REACTOR + "Servers", priority = ksStyle.WINDOW_GROUP)]
        static void ServerManagers()
        {
            ksWindow.Open(ksWindow.REACTOR_SERVERS, delegate (ksWindow window)
            {
                window.titleContent = new GUIContent(" Servers", ksTextures.Logo);
                window.minSize = new Vector2(380f, 100f);
                window.Menu = ScriptableObject.CreateInstance<ksServersMenu>();
            });
        }


        /// <summary>Shows local server log files.</summary>
        [MenuItem(ksMenuNames.REACTOR + "Local Server Logs", priority = ksStyle.CONFIG_GROUP)]
        static void ShowConsole()
        {
            ksWindow.Open(ksWindow.REACTOR_CONSOLE, delegate (ksWindow window)
            {
                window.titleContent = new GUIContent(" Logs", ksTextures.Logo);
                window.minSize = new Vector2(380f, 100f);
                window.Menu = ScriptableObject.CreateInstance<ksServerLogMenu>();
            });
        }

        /// <summary>Builds the current scene locally.</summary>
        [MenuItem(ksMenuNames.REACTOR + "Build Scene Configs %F2", priority = ksStyle.CONFIG_GROUP)]
        public static void BuildConfig()
        {
            if (!CheckInstallationPath())
            {
                return;
            }
            ksConfigWriter configWriter = new ksConfigWriter();
            configWriter.PrettyConfigs = true;
            if (configWriter.Build(false, false))
            {
                ksLog.Info("Build complete." + configWriter.Summary);
                ksLocalServer.Manager.StopServers();
                ksLocalServer.Manager.RestartOrRemoveStoppedServers();
            }
        }

        /// <summary>Builds the server runtime.</summary>
        /// <returns>True if the server runtime built successfully.</returns>
        [MenuItem(ksMenuNames.REACTOR + "Build KSServerRuntime %F3", priority = ksStyle.BUILD_GROUP)]
        public static bool BuildServerRuntime()
        {
            if (!CheckInstallationPath())
            {
                return false;
            }

            EditorUtility.DisplayProgressBar("KS Reactor Build", "Building runtime.", 0.1f);
            bool buildSuccess = false;
            try
            {
                ksConfigWriter configWriter = new ksConfigWriter();
                if (configWriter.RebuildRuntime())
                {
                    buildSuccess = true;
                    ksLog.Info("Config build complete." + configWriter.Summary);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            return buildSuccess;
        }

        /// <summary>Rebuilds the server runtime.</summary>
        [MenuItem(ksMenuNames.REACTOR + "Rebuild All %F4", priority = ksStyle.BUILD_GROUP)]
        public static void RebuildAll()
        {
            if (!CheckInstallationPath())
            {
                return;
            }

            ksConfigWriter configWriter = new ksConfigWriter();
            configWriter.PrettyConfigs = true;
            if (configWriter.Build(true, true, true))
            {
                ksLog.Info("Config and server build complete." + configWriter.Summary);
            }
        }

        /// <summary>Launches local cluster.</summary>
        [MenuItem(ksMenuNames.REACTOR + "Launch Local Cluster", priority = ksStyle.BUILD_GROUP)]
        private static void LaunchLocalCluster()
        {
            ksLocalServerMenu.Open();
        }

        /// <summary>Context menu for creating a <see cref="ksRoomScript"/> in the project views right click menu.</summary>
        [MenuItem(ksMenuNames.CREATE + "Client Room Script", priority = ksStyle.CLIENT_GROUP)]
        static void CreateClientRoomScript()
        {
            ksNewScriptMenu.Open(ScriptableObject.CreateInstance<ksClientScriptTemplate>().Initialize(
                ksClientScriptTemplate.ScriptType.ROOM), false, GetSelectedFolder());
        }

        /// <summary>Context menu for creating a <see cref="ksPlayerScript"/> in the project views right click menu.</summary>
        [MenuItem(ksMenuNames.CREATE + "Client Player Script", priority = ksStyle.CLIENT_GROUP)]
        static void CreateClientPlayerScript()
        {
            ksNewScriptMenu.Open(ScriptableObject.CreateInstance<ksClientScriptTemplate>().Initialize(
                ksClientScriptTemplate.ScriptType.PLAYER), false, GetSelectedFolder());
        }

        /// <summary>
        /// Context menu for creating a <see cref="ksEntityScript"/> in the project views right click menu.
        /// This will not attach it to a game object for you.
        /// </summary>
        [MenuItem(ksMenuNames.CREATE + "Client Entity Script", priority = ksStyle.CLIENT_GROUP)]
        static void CreateClientEntityScript()
        {
            ksNewScriptMenu.Open(ScriptableObject.CreateInstance<ksClientScriptTemplate>().Initialize(
                ksClientScriptTemplate.ScriptType.ENTITY), false, GetSelectedFolder());
        }

        /// <summary>
        /// Context menu for creating a <see cref="ksPredictor"/> in the project views right click menu.
        /// This will not attach it to a game object for you.
        /// </summary>
        [MenuItem(ksMenuNames.CREATE + "Predictor", priority = ksStyle.CLIENT_GROUP)]
        static void CreatePredictor()
        {
            ksNewScriptMenu.Open(
                ScriptableObject.CreateInstance<ksPredictorTemplate>().Initialize(),
                false,
                GetSelectedFolder());
        }

        /// <summary>Checks if the selected path is a valid location for a client script.</summary>
        /// <returns>True if a client script can be created at the selected path.</returns>
        [MenuItem(ksMenuNames.CREATE + "Client Room Script", true)]
        [MenuItem(ksMenuNames.CREATE + "Client Player Script", true)]
        [MenuItem(ksMenuNames.CREATE + "Client Entity Script", true)]
        [MenuItem(ksMenuNames.CREATE + "Collision Filter", true)]
        [MenuItem(ksMenuNames.CREATE + "Predictor Script", true)]
        static bool ValidateClientScriptPath()
        {
            string path = GetSelectedFolder();
            return !string.IsNullOrEmpty(path) &&
                !path.StartsWith(ksPaths.CommonScripts) &&
                !path.StartsWith(ksPaths.Proxies) &&
                !path.StartsWith(ksPaths.ServerRuntime) &&
                !Application.isPlaying;
        }


        /// <summary>
        /// Context menu for creating a <see cref="ksPlayerController"/> in the project views right click menu.
        /// This will not attach it to a game object for you.
        /// </summary>
        [MenuItem(ksMenuNames.CREATE + "Player Controller", priority = ksStyle.COMMON_GROUP)]
        static void CreatePlayerController()
        {
            ksNewScriptMenu.Open(ScriptableObject.CreateInstance<ksPlayerControllerTemplate>(), false,
                GetSelectedFolder());
        }

        /// <summary>Checks if the selected path is a valid location for a <see cref="ksPlayerController"/>.</summary>
        /// <returns>True if a <see cref="ksPlayerController"/> can be created at the selected path.</returns>
        [MenuItem(ksMenuNames.CREATE + "Player Controller", true)]
        static bool ValidatePlayerControllerPath()
        {
            string path = GetSelectedFolder();
            return !string.IsNullOrEmpty(path) &&
                path.StartsWith(ksPaths.CommonScripts) &&
                !Application.isPlaying;
        }

        /// <summary>
        /// Context menu for creating a <see cref="ksServerEntityScript"/> in the project views right click menu.
        /// This will not attach it to a game object for you.
        /// </summary>
        [MenuItem(ksMenuNames.CREATE + "Server Entity Script", priority = ksStyle.SERVER_GROUP)]
        static void CreateServerEntityScript()
        {
            ksNewScriptMenu.Open(ScriptableObject.CreateInstance<ksServerScriptTemplate>().Initialize(
                ksServerScriptTemplate.ScriptType.ENTITY), false, GetSelectedFolder());
        }

        /// <summary>
        /// Context menu for creating a <see cref="ksServerRoomScript"/> in the project view's right click menu.
        /// This will not attach it to a game object for you.
        /// </summary>
        [MenuItem(ksMenuNames.CREATE + "Server Room Script", priority = ksStyle.SERVER_GROUP)]
        static void CreateServerRoomScript()
        {
            ksNewScriptMenu.Open(ScriptableObject.CreateInstance<ksServerScriptTemplate>().Initialize(
                ksServerScriptTemplate.ScriptType.ROOM), false, GetSelectedFolder());
        }

        /// <summary>
        /// Context menu for creating a <see cref="ksScriptAsset"/> in the project view's right click menu.
        /// </summary>
        [MenuItem(ksMenuNames.CREATE + "Script Asset", priority = ksStyle.COMMON_GROUP)]
        static void CreateScriptAsset()
        {
            ksNewScriptMenu.Open(ScriptableObject.CreateInstance<ksScriptAssetTemplate>(), false, GetSelectedFolder());
        }

        /// <summary>
        /// Context menu for creating a <see cref="ksServerPlayerScript"/> in the project views right click menu.
        /// This will not attach it to a game object for you.
        /// </summary>
        [MenuItem(ksMenuNames.CREATE + "Server Player Script", priority = ksStyle.SERVER_GROUP)]
        static void CreateServerPlayerScript()
        {
            ksNewScriptMenu.Open(ScriptableObject.CreateInstance<ksServerScriptTemplate>().Initialize(
                ksServerScriptTemplate.ScriptType.PLAYER), false, GetSelectedFolder());
        }

        /// <summary>Checks if the selected path is a valid location for a server script.</summary>
        /// <returns>True if a server script can be created at the selected path.</returns>
        [MenuItem(ksMenuNames.CREATE + "Server Entity Script", true)]
        [MenuItem(ksMenuNames.CREATE + "Server Room Script", true)]
        [MenuItem(ksMenuNames.CREATE + "Server Player Script", true)]
        static bool ValidateServerScriptPath()
        {
            string path = GetSelectedFolder();
            return !string.IsNullOrEmpty(path) &&
                path.StartsWith(ksPaths.ServerRuntime) &&
                !Application.isPlaying;
        }

        /// <summary>Checks if the selected path is a valid location for a script asset.</summary>
        /// <returns>True if a script asset can be created at the selected path.</returns>
        [MenuItem(ksMenuNames.CREATE + "Script Asset", true)]
        static bool ValidateScriptAssetPath()
        {
            return ValidateServerScriptPath() || ValidatePlayerControllerPath();
        }

        /// <summary>
        /// Context menu for creating a <see cref="ksCollisionFilterAsset"/> in the project views right click menu.
        /// This will not attach it to a game object for you.
        /// </summary>
        [MenuItem(ksMenuNames.CREATE + "Collision Filter", priority = ksStyle.ASSET_GROUP)]
        static void CreateCollisionFilter()
        {
            ksCollisionFilterAsset asset = ScriptableObject.CreateInstance<ksCollisionFilterAsset>();
            ProjectWindowUtil.CreateAsset(asset, "New " + typeof(ksCollisionFilterAsset).Name + ".asset");
        }

        /// <summary>
        /// Hierarchy context menu for creating a game object with a <see cref="ksRoomType"/> and a
        /// <see cref="ksPhysicsSettings"/>.
        /// </summary>
        /// <param name="command">Contains menu context data.</param>
        [MenuItem(ksMenuNames.HIERARCHY + "Physics Room", false, 10)]
        static void CreatePhysicsRoom(MenuCommand command)
        {
            GameObject gameObject = new GameObject();
            gameObject.name = "Room";
            gameObject.AddComponent<ksRoomType>();
            gameObject.AddComponent<ksPhysicsSettings>();
            GameObjectUtility.SetParentAndAlign(gameObject, command.context as GameObject);
            Undo.RegisterCreatedObjectUndo(gameObject, "Create " + gameObject.name);
            Selection.activeGameObject = gameObject;
        }

        /// <summary>Hierarchy context menu for creating a game object with a <see cref="ksRoomType"/>.</summary>
        /// <param name="command">Contains menu context data.</param>
        [MenuItem(ksMenuNames.HIERARCHY + "Room", false, 10)]
        static void CreateRoom(MenuCommand command)
        {
            GameObject gameObject = new GameObject();
            gameObject.name = "Room";
            gameObject.AddComponent<ksRoomType>();
            GameObjectUtility.SetParentAndAlign(gameObject, command.context as GameObject);
            Undo.RegisterCreatedObjectUndo(gameObject, "Create " + gameObject.name);
            Selection.activeGameObject = gameObject;
        }

        /// <summary>Returns path to the selected folder relative to the project.</summary>
        /// <returns>Selected folder path.</returns>
        private static string GetSelectedFolder()
        {
            if (Selection.assetGUIDs == null || Selection.assetGUIDs.Length != 1)
            {
                return null;
            }
            string path = AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]);
            if (!Directory.Exists(path))
            {
                int index = path.LastIndexOf("/");
                if (index != -1)
                {
                    path = path.Substring(0, index);
                }
            }
            return path.Replace('\\', '/') + "/";
        }

        /// <summary>Prompts the user to enter an installation path if the installation path is not set.</summary>
        /// <returns>True if the installation path is set.</returns>
        public static bool CheckInstallationPath()
        {
            if (!File.Exists(ksReactorConfig.Instance.Server.ServerPath + "Reactor.exe"))
            {
                EditorUtility.DisplayDialog(
                    "Unable to locate the Reactor server path",
                    "Please update the server path in the Reactor config",
                    "OK");

                string path = EditorUtility.OpenFolderPanel(
                    "Reactor Server Directory",
                    ksReactorConfig.Instance.Server.ServerPath,
                    ""
                );
                if (!File.Exists(path + "/Reactor.exe"))
                {
                    ksLog.Error("Missing Reactor server path. Check Reactor settings");
                    return false;
                }
                // If the path is in the project, make the path relative to the project.
                path = ksPathUtils.Clean(path);
                string root = ksPathUtils.Clean(ksPaths.ProjectRoot);
                if (path.StartsWith(root))
                {
                    path = path.Substring(root.Length + 1);
                    if (path.Length == 0)
                    {
                        path = ".";
                    }
                }
                ksReactorConfig.Instance.Server.ServerPath = path + Path.DirectorySeparatorChar;
                ksServerProjectUpdater.Instance.UpdateOutputPath();
            }
            return true;
        }
    }
}