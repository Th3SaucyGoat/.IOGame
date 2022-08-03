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

using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KS.Reactor.Client.Unity
{
    /// <summary>
    /// Manages Reactor Unity features including: Service initialization, input binding managers, web API calls, 
    /// prefab/entity caching, and room/scene states.
    /// </summary>
    public class ksReactor
    {
        private const string LOG_CHANNEL = "KS.Unity.ksReactor";

        private static readonly string ASSET_DATA_FILE = "ksAssets";

        private static ksService m_service = null;
        private static ksInputManager m_inputManager;
        private static ksPrefabCache m_prefabCache;
        private static ksAssetLoader m_assetLoader;
        private static Dictionary<string, AssetBundle> m_assetBundles = new Dictionary<string, AssetBundle>();
        private static GameObject m_hookObject;
        private static Dictionary<int, ksEntityLinker> m_entityLinkers = new Dictionary<int, ksEntityLinker>();

        /// <summary>Get the Reactor service.</summary>
        public static ksService Service
        {
            get
            {
                Initialize();
                return m_service;
            }
        }

        /// <summary>Access Reactor player web API.</summary>
        public static ksPlayerAPI PlayerAPI
        {
            get
            {
                Initialize();
                return m_service.PlayerAPI;
            }
        }

        /// <summary>Access Reactor input managment.</summary>
        public static ksInputManager InputManager
        {
            get 
            {
                if (m_inputManager == null && Service != null)
                {
                    m_inputManager = new ksInputManager(Service.InputMarshaller);
                }
                return m_inputManager; 
            }
        }

        /// <summary>Get the entity prefab cache which maps entity IDs to Unity prefabs.</summary>
        public static ksPrefabCache PrefabCache
        {
            get
            {
                Initialize();
                return m_prefabCache;
            }
        }

        /// <summary>
        /// Gets the asset loader for loading <see cref="ksScriptAsset"/>s from Resources or asset bundles.
        /// </summary>
        public static ksAssetLoader Assets
        {
            get
            {
                Initialize();
                return m_assetLoader;
            }
        }

        /// <summary>Get the entity linker which maps entity IDs to game object instances in a scene.</summary>
        /// <param name="scene">Unity scene</param>
        /// <returns>Entity linker that maps Entity IDs to game objects.</returns>
        public static ksEntityLinker GetEntityLinker(Scene scene)
        {
            Initialize();
            ksEntityLinker linker;
            if (!m_entityLinkers.TryGetValue(scene.handle, out linker))
            {
                linker = new ksEntityLinker(scene);
                m_entityLinkers.Add(scene.handle, linker);
            }
            return linker;
        }

        /// <summary>Clean up the entity linker for a Unity scene.</summary>
        /// <param name="scene">Unity scene</param>
        private static void CleanupScene(Scene scene)
        {
            m_entityLinkers.Remove(scene.handle);
        }

        /// <summary>Initializes the Reactor service and the scene if they are not already initialized.</summary>
        private static void Initialize()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            // Initialize service
            if (m_service == null)
            {
                ksService.AppDataPath = Application.persistentDataPath;// This makes the path work on Android
                m_service = new ksService();
                Assembly gameAssembly = LoadReflectionData();

                // Initialize player API
                ksReactorConfig config = ksReactorConfig.Instance;
                if (!string.IsNullOrEmpty(config.Build.APIKey) && !string.IsNullOrEmpty(config.Build.APIClientSecret))
                {
                    m_service.PlayerAPI.Initialize(config.Urls.PlayerAPI, config.Build.APIKey, config.Build.APIClientSecret);
                }

                // Set the valid types all RPC methods owner must be a subclass of
                ksRPCManager<ksRPCAttribute>.Instance.SetValidSubclasses(typeof(ksRoomScript), typeof(ksEntityScript));

                // Initialize prefab cache and asset loader.
                m_prefabCache = new ksPrefabCache(m_assetBundles);
                m_assetLoader = new ksAssetLoader(m_assetBundles, gameAssembly);
                ksScriptAsset.Assets = m_assetLoader;
                // Load asset data from Resources.
                RegisterResourceAssets();

                SceneManager.sceneUnloaded += CleanupScene;

                // Set the converging input predictor's velocity tolerance calculator delegate.
                ksConvergingInputPredictor.ConfigData.VelocityToleranceCalculator = 
                    ksConvergingInputPredictorComponent.CalculateVelocityTolerance;

#if !UNITY_EDITOR && UNITY_WEBGL
                Service.WebRequestFactory = ksUnityWebRequest.Create;
                ksWSConnection.Register();
#endif
            }

            if (m_hookObject == null)
            {
                SetUnityCallBacks();
            }
        }

        /// <summary>
        /// Send a web API request for a list of running server instances which match the image binding 
        /// in the <see cref="ksReactorConfig.Build"/>.
        /// </summary>
        /// <param name="callback">Callback to invoke when the API request completes.</param>
        public static void GetServers(ksService.RoomListCallback callback)
        {
            if (Service != null)
            {
                Service.GetServers(
                    ksReactorConfig.Instance.Urls.Instances,
                    ksReactorConfig.Instance.Build.ImageBinding,
                    callback
                );
            }
            else if (callback != null)
            {
                callback(new List<ksRoomInfo>(), "You can only get a list of rooms when the game is playing.");
            }
        }

        /// <summary>Registers an asset bundle so entity prefabs and script assets can be loaded from it.</summary>
        /// <param name="assetBundle">Asset bundle to register.</param>
        public static void RegisterAssetBundle(AssetBundle assetBundle)
        {
            try
            {
                // If we've loaded the asset bundle already we don't need to load it again
                if (!m_assetBundles.ContainsKey(assetBundle.name))
                {
                    TextAsset entityData = assetBundle.LoadAsset<TextAsset>(ASSET_DATA_FILE);
                    if (entityData == null)
                    {
                        ksLog.Warning(LOG_CHANNEL, "Could not find asset data in asset bundle " + assetBundle.name);
                        return;
                    }
                    LoadAssetData(new StringReader(entityData.text), assetBundle.name);
                }
                m_assetBundles[assetBundle.name] = assetBundle;
            }
            catch (Exception e)
            {
                ksLog.Error(LOG_CHANNEL, "Error loading asset data from asset bundle " + assetBundle.name + ".", e);
            }
        }

        /// <summary>Registers entity prefabs and script assets from resources so they can be loaded.</summary>
        private static void RegisterResourceAssets()
        {
            try
            {
                TextAsset entityData = Resources.Load<TextAsset>(ASSET_DATA_FILE);
                if (entityData != null)
                {
                    LoadAssetData(new StringReader(entityData.text));
                }
            }
            catch (Exception e)
            {
                ksLog.Error(LOG_CHANNEL, "Error loading entity data from resources.", e);
            }
        }

        /// <summary>
        /// Loads asset mappings and adds them to the <see cref="PrefabCache"/> and <see cref="Assets"/>.
        /// </summary>
        /// <param name="reader">String reader</param>
        /// <param name="assetBundle">
        /// AssetBundle that the assets belong to, or null if they are not part of an asset bundle.
        /// </param>
        private static void LoadAssetData(StringReader reader, string assetBundle = null)
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                string[] split = line.Split(new char[] { '|' }, 2);
                uint assetId = uint.Parse(split[0]);
                string assetPath = split[1];
                // script asset paths start with '*".
                bool isScriptAsset = assetPath.StartsWith("*");
                if (isScriptAsset)
                {
                    // Remove '*'.
                    assetPath = assetPath.Substring(1);
                }
                if (!string.IsNullOrEmpty(assetBundle))
                {
                    assetPath = assetBundle + ":" + assetPath;
                }
                if (isScriptAsset)
                {
                    m_assetLoader.RegisterAsset(assetId, assetPath);
                }
                else
                {
                    m_prefabCache.RegisterEntityType(assetId, assetPath);
                }
            }
        }

        /// <summary>Sets Unity engine hooks.</summary>
        private static void SetUnityCallBacks()
        {
            m_hookObject = new GameObject();
            m_hookObject.name = "ksReactor";
            UnityEngine.Object.DontDestroyOnLoad(m_hookObject);
            ksUpdateHook hook = m_hookObject.AddComponent<ksUpdateHook>();
            hook.OnUpdate = Update;
            hook.OnQuit = OnQuit;
        }

        /// <summary>Update the input manager and all connected rooms.</summary>
        /// <param name="deltaTime">Game time in seconds since the last update.</param>
        /// <param name="realDeltaTime">Real time in seconds since the last update.</param>
        private static void Update(float deltaTime, float realDeltaTime)
        {
            InputManager.Update();
            m_service.Update(deltaTime, realDeltaTime);
        }

        /// <summary>Disconnects all rooms immediately when the Unity application quits.</summary>
        private static void OnQuit()
        {
            m_service.Disconnect(true);
        }

        /// <summary>Loads reflection data from developer scripts.</summary>
        /// <returns>Game assembly.</returns>
        private static Assembly LoadReflectionData()
        {
            Assembly gameAssembly;
            try
            {
                gameAssembly = Assembly.Load("Assembly-CSharp");
            }
            catch (Exception e)
            {
                ksLog.Error(LOG_CHANNEL, "Unable to load Assembly-CSharp", e);
                return null;
            }
            m_service.PlayerControllerFactory.RegisterFromAssembly(gameAssembly);
            return gameAssembly;
        }
    }
}