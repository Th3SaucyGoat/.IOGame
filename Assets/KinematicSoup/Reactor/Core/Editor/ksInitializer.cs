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
            ksPublishService.Get().Start();
            CreateReactorConfig();
            PrioritizeUpdate();
            UpdateVersion();
            new ksIconManager().SetScriptIcons();
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

            // Detect if we are in a development environment
            if (File.Exists(Application.dataPath + "/KSSource/Reactor.asmdef"))
            {
                ksEditorUtils.SetDefineSymbol("KS_DEVELOPMENT");
            }
            else
            {
                ksEditorUtils.ClearDefineSymbol("KS_DEVELOPMENT");
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
                    if (version < new ksVersion(0, 9, 16, 0))
                    {
                        DisplayUpgradeMessage("Cannot automatically upgrade projects older than " +
                            "0.9.17. You should upgrade to 0.9.17+ before upgrading to 0.10.", true);
                        return;
                    }

                    if (version < new ksVersion(0, 10, 0, 0))
                    {
                        ksReactorUpgrade_0_10_0.Upgrade();
                        DisplayUpgradeMessage("Upgrade to 0.10.0 complete. See logs for details");
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
            ksReactorConfig.Instance.Build.LastBuildVersion = ksReactorConfig.Instance.Version;
            EditorUtility.SetDirty(ksReactorConfig.Instance);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Create a <see cref="ksReactorConfig"/> asset if one does not already exist.
        /// </summary>
        private static void CreateReactorConfig()
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

        /// <summary>
        /// Upgrades a project to version 0.9.13 from a prior version of Reactor.
        /// - Moves developer Reactor scripts to Assets/ReactorScripts.
        /// - Renames namespace 'KS.Reactor.Client.Unity.Adaptors' to 'KS.Reactor.Client.Unity' in all scripts.
        /// - Updates file ids/guids in scenes, prefabs, and assets to refer to open-source Reactor scripts.
        /// - Deletes old unused Reactor dlls."
        /// </summary>
        private static bool UpgradeTo0_9_13()
        {
            SerializationMode serializationMode = EditorSettings.serializationMode;
            if (serializationMode != SerializationMode.ForceText)
            {
                // We need .meta files to be serialized as text so we can parse and update them.
                // Setting this forces the files to reserialize
                EditorSettings.serializationMode = SerializationMode.ForceText;
            }

            ksPathUtils.LoggingFlags logging = ksPathUtils.LoggingFlags.ALL;
            string reactorRoot = Application.dataPath + "/KinematicSoup/Reactor/";
            ksPathUtils.Move(reactorRoot + "Client", ksPaths.ClientScripts, true, logging);
            ksPathUtils.Move(reactorRoot + "Common", ksPaths.CommonScripts, true, logging);
            ksPathUtils.Move(reactorRoot + "Editor/ServerRuntime", ksPaths.ServerRuntime, true, logging);
            ksPathUtils.Move(reactorRoot + "Proxies", ksPaths.Proxies, true, logging);
            ksPathUtils.Move(reactorRoot + "Resources", ksPaths.ReactorScripts + "Resources", true, logging);
            string icons = reactorRoot + "Editor/Icons/";
            ksPathUtils.Delete(icons + "ConeCollider.png", false, logging);
            ksPathUtils.Delete(icons + "CylinderCollider.png", false, logging);
            ksPathUtils.Delete(icons + "KSClientEntityScript.png", false, logging);
            ksPathUtils.Delete(icons + "KSPlayerEntityScript.png", false, logging);
            ksPathUtils.Delete(icons + "KSRoomEntitySript.png", false, logging);
            ksPathUtils.Delete(icons + "KSEntity.png", false, logging);
            ksPathUtils.Delete(icons + "KSRoomType.png", false, logging);
            ksPathUtils.Delete(icons + "KSServerEntityScript.png", false, logging);
            ksPathUtils.Delete(icons + "KSServerPlayerScript.png", false, logging);
            ksPathUtils.Delete(icons + "KSServerRoomScript.png", false, logging);
            ksPathUtils.Delete(icons + "Offline.png", false, logging);
            ksPathUtils.Delete(icons + "Online.png", false, logging);
            ksPathUtils.Delete(icons + "Reactor.png", false, logging);
            ksPathUtils.Delete(icons, false, logging);
            ksPathUtils.Delete(reactorRoot + "Scripts/ksPlayerAPIExtensions.cs", false, logging);
            string newSampleConnect = reactorRoot + "Examples/ksSampleConnect.cs";
            string oldSampleConnect = reactorRoot + "Scripts/Samples/ksSampleConnect.cs";
            if (File.Exists(newSampleConnect))
            {
                ksPathUtils.Delete(oldSampleConnect, false, logging);
            }
            else
            {
                ksPathUtils.Move(oldSampleConnect, newSampleConnect, false, logging);
            }
            ksPathUtils.Delete(reactorRoot + "Scripts/Samples", false, logging);
            ksPathUtils.Delete(reactorRoot + "Scripts", false, logging);
            ksPathUtils.Delete(reactorRoot + "KSReactor.dll", false, logging);
            ksPathUtils.Delete(reactorRoot + "KSReactor.WebGL.dll", false, logging);

            ksScriptGuidUpdater guidUpdater = new ksScriptGuidUpdater();
            // Guids for ksPhysicsSettings
            guidUpdater.AddReplacement(
              1175044150, "2508e5f770cc93b47b6da4fcac48c53e",
                11500000, "dc28043044ad1994785f539458df6e85");
            // Guids for ksRoomType
            guidUpdater.AddReplacement(
             -1169119406, "2508e5f770cc93b47b6da4fcac48c53e",
                11500000, "27f9fafc72047e9419604f25afe390b0");
            // Guids for ksEntityComponent
            guidUpdater.AddReplacement(
                11043901, "2508e5f770cc93b47b6da4fcac48c53e",
                11500000, "ee06f2f9480b8674880fcaa5a0eaa08d");
            // Guids for ksCylinderCollider
            guidUpdater.AddReplacement(
              -143262860, "2508e5f770cc93b47b6da4fcac48c53e",
                11500000, "074d4b57371816c4f917173c65f5e89a");
            // Guids for ksConeCollider
            guidUpdater.AddReplacement(
               -74257297, "2508e5f770cc93b47b6da4fcac48c53e",
                11500000, "c6b6e7c35aa2d4b41bf2fbc0b9b6ed83");
            // Guids for ksReactorConfig
            guidUpdater.AddReplacement(
               699804721, "2508e5f770cc93b47b6da4fcac48c53e",
                11500000, "092fd99ff970176479d0e3364aa61c0c");
            // Guids for ksCollisionFilterAsset
            guidUpdater.AddReplacement(
              -214489396, "2508e5f770cc93b47b6da4fcac48c53e",
                11500000, "af7c2b923e0887143a8f52e243a38955");
            guidUpdater.ReplaceAll();

            ksScriptUpdater scriptUpdater = new ksScriptUpdater();
            scriptUpdater.AddReplacement(@"KS\.Reactor\.Client\.Unity\.Adaptors", "KS.Reactor.Client.Unity");
            scriptUpdater.ExcludePath(Application.dataPath + "/KinematicSoup");
            scriptUpdater.ReplaceAll();

            if (serializationMode != SerializationMode.ForceText)
            {
                // Restore serialization mode
                EditorSettings.serializationMode = serializationMode;
            }

            DisplayUpgradeMessage("- Moved developer Reactor scripts to Assets/ReactorScripts.\n" +
                "- Renamed namespace 'KS.Reactor.Client.Unity.Adaptors' to 'KS.Reactor.Client.Unity' in all scripts.\n" +
                "- Updated file ids/guids in scenes, prefabs, and assets to refer to open-source Reactor scripts.\n" +
                "- Deleted old unused Reactor dlls.\n" +
                "See logs for more details.");

            return true;
        }
    }
}
