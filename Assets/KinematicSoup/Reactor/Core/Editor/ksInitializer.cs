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
using System.IO;
using UnityEditor;
using UnityEngine;
using KS.Unity.Editor;

namespace KS.Reactor.Client.Unity.Editor
{
    /// <summary>
    /// Performs initialization logic when the Unity editor loads.
    /// - Loads Reactor configs.
    /// - Checks Reactor package versions.
    /// - Prepares caches.
    /// - Attaches Reactor scripts to game objects.
    /// - Initializes UI windows.
    /// </summary>
    [InitializeOnLoad]
    public class ksInitializer
    {
        private static readonly string LOG_CHANNEL = typeof(ksInitializer).ToString();

        /// <summary>Static constructor</summary>
        static ksInitializer()
        {
            ksRoomType.LocalServerRunningChecker = ksLocalServer.Manager.IsLocalServerRunning;
            EditorApplication.update += Init;
        }

        /// <summary>Performs initialization logic that must wait until after Unity deserialization finishes.</summary>
        private static void Init()
        {
            EditorApplication.update -= Init;
            CreateReactorConfig();

            // Detect if we are in a development environment
            if (File.Exists(Application.dataPath + "/KSSource/Reactor.asmdef"))
            {
                if (ksEditorUtils.SetDefineSymbol("KS_DEVELOPMENT"))
                {
                    return;
                }
            }
            else if (ksEditorUtils.ClearDefineSymbol("KS_DEVELOPMENT"))
            {
                return;
            }

            ksPublishService.Get().Start();
            PrioritizeUpdate();
            UpdateVersion();
            new ksIconManager().SetScriptIcons();
            ksServerProjectUpdater.Instance.GenerateMissingAsmDefs();
            ksPaths.FindCommonAndServerFolders();
            ksServerProjectWatcher.Get().Run();
            ksScriptGenerator.Get().LoadAttachments();
            
            // Set default server path to the project directory / Reactor
            if (string.IsNullOrEmpty(ksReactorConfig.Instance.Server.ServerPath))
            {
                ksReactorConfig.Instance.Server.ServerPath = "Reactor" + Path.DirectorySeparatorChar;
                ksServerProjectUpdater.Instance.UpdateOutputPath();
            }

            // Initialize Publish Window
            ksWindow.SetMenuType(ksWindow.REACTOR_PUBLISH, typeof(ksPublishMenu));
            ksWindow window = ksWindow.Find(ksWindow.REACTOR_PUBLISH);
            if (window != null)
            {
                window.Repaint();
            }

            // Initialize Servers Window
            ksWindow.SetMenuType(ksWindow.REACTOR_SERVERS, typeof(ksServersMenu));
            window = ksWindow.Find(ksWindow.REACTOR_SERVERS);
            if (window != null)
            {
                window.Repaint();
            }

            // Initialize Console Window
            ksWindow.SetMenuType(ksWindow.REACTOR_CONSOLE, typeof(ksServerLogMenu));
            window = ksWindow.Find(ksWindow.REACTOR_CONSOLE);
            if (window != null)
            {
                window.Repaint();
            }
        }

        /// <summary>
        /// Sets priority for <see cref="ksUpdateHook"/> to ensure our update loop runs before other scripts.
        /// </summary>
        private static void PrioritizeUpdate()
        {
            string scriptName = typeof(ksUpdateHook).Name;
            foreach (MonoScript monoScript in MonoImporter.GetAllRuntimeMonoScripts())
            {
                if (monoScript.name == scriptName)
                {
                    // without this we will get stuck in an infinite loop
                    if (MonoImporter.GetExecutionOrder(monoScript) != -1000)
                    {
                        MonoImporter.SetExecutionOrder(monoScript, -1000);
                    }
                    break;
                }
            }
        }

        /// <summary>Updates the project API version number.</summary>
        private static void UpdateVersion()
        {
            try
            {
                ksVersion.Current = ksVersion.FromString(ksReactorConfig.Instance.Version);
            }
            catch (ArgumentException)
            {
                ksLog.Warning(LOG_CHANNEL, "Invalid Reactor version string " +
                    ksReactorConfig.Instance.Version);
                return;
            }
            string versionString = ksReactorConfig.Instance.Build.LastBuildVersion;
            if (versionString != "")
            {
                ksVersion version = new ksVersion();
                bool isValid = true;
                try
                {
                    version = ksVersion.FromString(versionString);
                }
                catch (ArgumentException)
                {
                    ksLog.Warning(LOG_CHANNEL, "Project has invalid version string (" + versionString +
                        ") and may be incompatible with the current version (" + ksVersion.Current + ")");
                    isValid = false;
                }
                if (isValid)
                {
                    if (version < new ksVersion(0, 10, 0, 0))
                    {
                        DisplayUpgradeMessage("Cannot automatically upgrade projects older than " +
                            "0.10.0. You should upgrade to 0.10.0 before upgrading further.", true);
                    }
                    else
                    {
                        if (version < new ksVersion(0, 10, 1, 0))
                        {
                            // Remove the old KSReactor-Editor.dll
                            ksPathUtils.Delete(ksPaths.ReactorRoot + "Core/Editor/KSReactor-Editor.dll", true,
                                ksPathUtils.LoggingFlags.ALL);
                            ksPathUtils.Delete(ksPaths.ReactorRoot + "Core/Editor/KSReactor-Editor.xml", true,
                                ksPathUtils.LoggingFlags.ALL);
                            // Move server runtime folder from ReactorScripts/Editor/Server to ReactorScripts/Server
                            ksPathUtils.Move(ksPaths.ReactorScripts + "Editor/Server", ksPaths.ServerScripts, false,
                                ksPathUtils.LoggingFlags.ALL);
                            ksPathUtils.Delete(ksPaths.ReactorScripts + "Editor", false, ksPathUtils.LoggingFlags.ALL);
                            // Rebuild the server runtime, and regenerate project files and asmdefs.
                            new ksConfigWriter().Build(false, true, false, ksServerScriptCompiler.Configurations.LOCAL_RELEASE, true);
                            DisplayUpgradeMessage("Upgrade to 0.10.1 complete. See logs for details");
                        }

                        if (version < ksVersion.Current)
                        {
                            DeleteMarkedFiles();
                            ksLog.Info(LOG_CHANNEL, "This project was using an old Reactor version " + version +
                                ". Your project has been successfully updated to " + ksVersion.Current);
                        }
                        else if (version > ksVersion.Current)
                        {
                            if (!EditorUtility.DisplayDialog(
                                "Reactor Project Version Mismatch",
                                "This project is using a newer Reactor version (" + version +
                                ") than the current version (" + ksVersion.Current +
                                ") and may be incompatible. Continue anyway?",
                                "OK",
                                "Cancel"))
                            {
                                EditorApplication.Exit(0);
                            }
                        }
                    }
                }
            }
            ksReactorConfig.Instance.Build.LastBuildVersion = ksReactorConfig.Instance.Version;
            EditorUtility.SetDirty(ksReactorConfig.Instance);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Create a <see cref="ksReactorConfig"/> asset if one does not already exist.
        /// </summary>
        public static void CreateReactorConfig()
        {
            if (ksReactorConfig.Instance == null)
            {
                ksLog.Info(LOG_CHANNEL, "Creating Reactor Config");
                ksPathUtils.Create(ksPaths.Config);
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<ksReactorConfig>(), ksPaths.Config);
            }
        }

        /// <summary>Displays a popup project update message.</summary>
        /// <param name="details">Added to the displayed message.</param>
        /// <param name="error">Was the project updated successfully?</param>
        private static void DisplayUpgradeMessage(string details, bool error = false)
        {
            string title = "Reactor Project Update";
            if (error)
            {
                title += " Error";
                details = "Could not update your project to version " + ksVersion.Current + ".\n" + details +
                    "\nContinue anyway?";
                if (!EditorUtility.DisplayDialog(title, details, "OK", "Quit"))
                {
                    EditorApplication.Exit(0);
                }
            }
            else
            {
                details = "Your project was automatically updated to version " + ksVersion.Current + ".\n" + details;
                EditorUtility.DisplayDialog(title, details, "OK");
            }
        }

        /// <summary>
        /// Deletes all C# files in the KinematicSoup/Reactor folder that contain a single line with the following text:
        /// /**DELETE ME**/
        /// We use this to delete files from older Reactor versions that are no longer used, since Unity doesn't give
        /// us a better way to delete files when importing an asset package.
        /// </summary>
        private static void DeleteMarkedFiles()
        {
            // Do nothing in the development environment so we don't accidentally delete files from the repo.
#if !KS_DEVELOPMENT
            try
            {
                string reactorRoot = "Assets/KinematicSoup/Reactor";
                ksPathUtils.LoggingFlags logging = ksPathUtils.LoggingFlags.ALL;
                foreach (string file in Directory.GetFiles(reactorRoot, "*.cs", SearchOption.AllDirectories))
                {
                    bool delete = false;
                    using (StreamReader reader = new StreamReader(file))
                    {
                        delete = reader.ReadLine() == "/**DELETE ME**/" && reader.ReadToEnd() == "";
                    }
                    if (delete)
                    {
                        ksPathUtils.Delete(file, false, logging);
                    }
                }
            }
            catch (Exception e)
            {
                ksLog.Error(LOG_CHANNEL, "Error deleting old files.", e);
            }
#endif
        }
    }
}
