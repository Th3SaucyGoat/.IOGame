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
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KS.Reactor.Client.Unity
{
    /// <summary>
    /// Unity implementation of ksBaseRoom. Rooms are used to connect to server rooms. The room maintains 
    /// a simulation state that is regularly synced with the server.
    /// </summary>
    public class ksRoom : ksBaseRoom<ksPlayer, ksEntity>
    {
        /// <summary>Game object for attaching room scripts.</summary>
        public GameObject GameObject
        {
            get { return m_gameObject; }
        }
        private GameObject m_gameObject;
        private bool m_isSceneLoaded;
        private ksEntityFactory m_defaultEntityFactory = new ksEntityFactory();

        // List of entity type regex pattern paired with entity factories. A list is used to preserve the order the pairs are added. 
        private List<KeyValuePair<string, ksEntityFactory>> m_regexFactories = new List<KeyValuePair<string, ksEntityFactory>>();

        // Mapping of asset IDs to entity factories.  When a entity type is matched using the regex factories for the first time, they are then
        // added to this dictionary. This speeds up factory lookups for known entity types.
        private Dictionary<uint, ksEntityFactory> m_assetFactories = new Dictionary<uint, ksEntityFactory>();

        private ksPredictor m_defaultPredictor;
        private ksPredictor m_defaultInputPredictor;

        /// <summary>Reactor physics interface.</summary>
        public override ksIPhysics Physics
        {
            get { return m_physics; }
        }
        private ksPhysicsAdaptor m_physics = new ksPhysicsAdaptor();

        /// <summary>Gravity applied in physics simulations.</summary>
        protected override ksVector3 Gravity
        {
            get { return m_physics.Gravity; }
            set { m_physics.Gravity = value; }
        }

        private delegate bool ValidateScriptHandler<T>(T script) where T : MonoBehaviour;

        private ksRoomType m_roomType;
        private ksRoomComponent m_component;

        /// <summary>Constructor</summary>
        /// <param name="roomInfo">Room connection information.</param>
        public ksRoom(ksRoomInfo roomInfo)
            : base(roomInfo)
        {
            Scene scene = SceneManager.GetSceneByName(roomInfo.Scene);
            m_isSceneLoaded = scene.IsValid() && scene.isLoaded;
            if (m_isSceneLoaded)
            {
                ksReactor.GetEntityLinker(scene).Initialize();
            }
            else
            {
                ksLog.Warning(this, "The scene '" + roomInfo.Scene + "' for the room is not loaded. If your scene " +
                    "uses instance entities or permanent entities, the scene will not sync properly.");
            }
            if (Info.Type == null || Info.Type.Length == 0)
            {
                m_gameObject = new GameObject("ksRoom");
            }
            else if (!FindRoomType(Info.Type))
            {
                m_gameObject = new GameObject("ksRoom(" + Info.Type + ")");
            }
            else
            {
                m_gameObject = CloneScripts<ksRoomScript>(m_roomType.gameObject);
                m_gameObject.name = "ksRoom (" + Info.Type + ")";
                FindDefaultPredictors(m_roomType.gameObject);
                ksPhysicsSettings settings = m_roomType.GetComponent<ksPhysicsSettings>();
                if (settings != null)
                {
                    m_physics.AutoSync = settings.AutoSync;
                    ksCollisionFilter.Default = ksReactor.Assets.FromProxy<ksCollisionFilter>(
                        settings.DefaultCollisionFilter);
                    ksBaseUnityRigidBody.DefaultType = settings.DefaultEntityPhysics.RigidBodyType;
                }
            }
            m_gameObject.transform.position = ksVector3.Zero;
            m_gameObject.transform.rotation = ksQuaternion.Identity;
            m_gameObject.transform.localScale = ksVector3.One;
            m_component = m_gameObject.AddComponent<ksRoomComponent>();
            m_component.ParentObject = this;
            if (m_isSceneLoaded)
            {
                SceneManager.MoveGameObjectToScene(m_gameObject, scene);
            }
            m_gameObject.SetActive(true);
        }

        /// <summary>Default constructor.</summary>
        public ksRoom()
            : this(new ksRoomInfo())
        {
        }

        /// <summary>Connect to the server room.</summary>
        /// <param name="session">Player session</param>
        /// <param name="authArgs">Room authentication arguments.</param>
        public void Connect(ksPlayerAPI.Session session, params ksMultiType[] authArgs)
        {
            if (!Application.isPlaying)
            {
                ksLog.Warning(this, "You can only connect to a room while the game is playing.");
            }
            if (ksReactor.Service != null)
            {
                ksReactor.Service.JoinRoom(this, session, authArgs);
            }
        }

        /// <summary>Connect to the server room.</summary>
        /// <param name="authArgs">Room authentication arguments.</param>
        public void Connect(params ksMultiType[] authArgs)
        {
            if (!Application.isPlaying)
            {
                ksLog.Warning(this, "You can only connect to a room while the game is playing.");
            }
            if (ksReactor.Service != null)
            {
                ksReactor.Service.JoinRoom(this, null, authArgs);
            }
        }

        /// <summary>Disconnect from the server.</summary>
        /// <param name="immediate">
        /// If immediate is false, then disconnection will be delayed until all queued RPC
        /// calls have been sent.
        /// </param>
        public void Disconnect(bool immediate = false)
        {
            if (ksReactor.Service != null)
            {
                ksReactor.Service.LeaveRoom(this, immediate);
            }
        }

        /// <summary>
        /// Creates a predictor for an entity or player/room properties. Looks for a <see cref="ksPredictor"/> script
        /// on the <paramref name="baseEntity"/>'s game object. If the entity has a controller with
        /// <see cref="ksPlayerController.UseInputPrediction"/> true, looks for a predictor with
        /// <see cref="ksPredictor.UseWith"/> set to <see cref="ksPredictor.UseModes.CONTROLLER"/> or 
        /// <see cref="ksPredictor.UseModes.ALL"/>. If not, it looks for one with 
        /// <see cref="ksPredictor.UseModes.NO_CONTROLLER"/> or <see cref="ksPredictor.UseModes.ALL"/>. If not
        /// found, uses the default predictor found on the <see cref="ksRoomType"/> object, or calls the base method to
        /// construct a default predictor if none was found.
        /// </summary>
        /// <param name="baseEntity">Entity to get predictor for. Null if predicting player/room properties.</param>
        public override ksIPredictor CreatePredictor(ksBaseEntity baseEntity)
        {
            if (baseEntity != null)
            {
                ksEntity entity = (ksEntity)baseEntity;
                // Return the current predictor if it's UseWith mode allows it to be used.
                ksPredictor current = entity.Predictor as ksPredictor;
                if (CanUsePredictor(current, entity))
                {
                    return current;
                }
                if (entity.GameObject != null)
                {
                    ksPredictor predictor = null;
                    foreach (ksPredictor pred in entity.GameObject.GetComponents<ksPredictor>())
                    {
                        if (CanUsePredictor(pred, entity))
                        {
                            if (predictor != null)
                            {
                                ksLog.Warning(this, "Multiple predictors found for entity " + entity.Type + " " +
                                    entity.Id + ". Using " + predictor.GetType().Name + ".");
                                break;
                            }
                            predictor = pred;
                        }
                    }
                    if (predictor != null)
                    {
                        return predictor is ksNullPredictor ? null : predictor;
                    }
                    if (entity.PlayerController != null && entity.PlayerController.UseInputPrediction)
                    {
                        if (m_defaultInputPredictor != null)
                        {
                            return m_defaultInputPredictor is ksNullPredictor ? null : ksUnityReflectionUtils.CloneScript(
                                m_defaultInputPredictor, entity.GameObject) as ksIPredictor;
                        }
                    }
                    else if (m_defaultPredictor != null)
                    {
                        return m_defaultPredictor is ksNullPredictor ? null : ksUnityReflectionUtils.CloneScript(
                            m_defaultPredictor, entity.GameObject) as ksIPredictor;
                    }
                }
            }
            return base.CreatePredictor(baseEntity);
        }

        /// <summary>
        /// Checks if a predictor can be used for an entity by checking the <see cref="ksPredictor.UseWith"/> mode.
        /// </summary>
        /// <param name="predictor">Predictor</param>
        /// <param name="entity">Entity</param>
        /// <returns>True if the predictor can be used with the entity.</returns>
        private bool CanUsePredictor(ksPredictor predictor, ksEntity entity)
        {
            if (predictor == null || !predictor.enabled)
            {
                return false;
            }
            if (entity.PlayerController != null && entity.PlayerController.UseInputPrediction)
            {
                return predictor.UseWith != ksPredictor.UseModes.NO_CONTROLLER;
            }
            return predictor.UseWith != ksPredictor.UseModes.CONTROLLER;
        }

        /// <summary>Create a new <see cref="ksPlayer"/></summary>
        /// <param name="id">Player ID</param>
        /// <returns>Player object</returns>
        protected override ksPlayer CreatePlayer(uint id)
        {
            return new ksPlayer(id);
        }

        /// <summary>Loads permanent entities from the scene.</summary>
        /// <returns>
        /// A list of entities which have <see cref="ksEntityComponent.IsPermanent"/> properties set to true.
        /// </returns>
        protected override List<ksEntity> LoadPermanentEntities()
        {
            List<ksEntity> entities = new List<ksEntity>();
            ksEntityLinker linker = ksReactor.GetEntityLinker(m_gameObject.scene);
            foreach (ksEntityComponent component in linker.GetPermanentObjects())
            {
                component.enabled = true;
                ksEntity entity = ksObjectPool<ksEntity>.Instance.Fetch();
                ((ksEntity.IInternals)entity).Initialize(
                    component.EntityId, 
                    component.AssetId, 
                    component.Type, 
                    component,
                    true);
                entities.Add(entity);
            }
            return entities;
        }

        /// <summary>Create a new <see cref="ksEntity"/></summary>
        /// <param name="id">
        /// If <paramref name="isGhost"/> is true, then this is the id of the original entity and we will
        /// use 0 for the ghost entity. Otherwise, it is the id of the entity to spawn.
        /// </param>
        /// <param name="assetId">Asset ID</param>
        /// <param name="isGhost">if true, we are creating an entity for a server ghost.</param>
        /// <returns>Entity object</returns>
        protected override ksEntity CreateEntity(uint id, uint assetId, bool isGhost = false)
        {
            GameObject gameObject = null;
            ksEntityComponent component = null;
            bool isSceneEntity = false;

            if (isGhost)
            {
                // If we are spawning a server ghost, instantiate it from the original.
                ksEntity original = GetEntity(id);
                if (original != null)
                {
                    gameObject = GameObject.Instantiate<GameObject>(original.GameObject);
                    gameObject.name = original.GameObject.name;
                }
                else
                {
                    ksLog.Error(this, "Could not find entity " + id + " to create server ghost from.");
                    return null;
                }
            }

            // Check if there's a game object in the scene for this entity
            if (gameObject == null && m_isSceneLoaded)
            {
                gameObject = ksReactor.GetEntityLinker(m_gameObject.scene).GetGameObjectForEntity(id);
                if (gameObject != null)
                {
                    component = gameObject.GetComponent<ksEntityComponent>();
                    if (component == null || component.AssetId != assetId)
                    {
                        gameObject = null;
                    }
                    isSceneEntity = true;
                }
            }

            ksEntityFactory factory = null;
            string entityType = ksReactor.PrefabCache.GetEntityType(assetId);

            // If the game object is null, then this is not a scene entity. Find and use a factory to create the gameobject.
            if (gameObject == null)
            {
                try
                {
                    GameObject prefab = ksReactor.PrefabCache.GetPrefab(assetId, entityType);
                    factory = GetEntityFactory(assetId, entityType);
                    gameObject = factory.GetGameObject(assetId, entityType, prefab);
                }
                catch (Exception e)
                {
                    ksLog.Error(this, "Error creating entity game object with id " + id, e);
                }
            }

            if (!ValidateEntityGameObject(gameObject, out component))
            {
                string type = string.IsNullOrEmpty(entityType) ? assetId.ToString() : entityType;
                gameObject = new GameObject("Missing Entity (" + type + ")");
                component = gameObject.AddComponent<ksEntityComponent>();
            }
            if (m_gameObject.transform.parent == null ||
                ksReactor.GetEntityLinker(m_gameObject.scene).Contains(gameObject))
            {
                gameObject.transform.parent = m_gameObject.transform;
            }
            component.IsPermanent = false;

            ksEntity entity = ksObjectPool<ksEntity>.Instance.Fetch();
            ((ksEntity.IInternals)entity).Initialize(isGhost ? 0 : id, assetId, entityType, component, isSceneEntity);
            if (factory != null)
            {
                factory.InitializeEntity(entity);
            }
            return entity;
        }

        /// <summary>
        /// Validates an entity game object. To be valid, the game object must:
        /// - not be null.
        /// - not be a prefab.
        /// - not be assigned to another non-destroyed entity.
        /// - have a <see cref="ksEntityComponent"/> component.
        /// </summary>
        /// <param name="gameObject">Entity game object</param>
        /// <param name="component">Component from the game object, or null if the game object is not valid.</param>
        /// <returns>True if the game object is a valid entity</returns>
        private bool ValidateEntityGameObject(GameObject gameObject, out ksEntityComponent component)
        {
            if (gameObject == null)
            {
                component = null;
                return false;
            }
            component = gameObject.GetComponent<ksEntityComponent>();
            if (component == null)
            {
                ksLog.Error(this, "Entity GameObject '" + gameObject.name + "' does not have a ksEntityComponent. " +
                    "Your entity factory must return game objects with ksEntityComponents.");
                GameObject.Destroy(gameObject);
                return false;
            }
            if (component.Entity != null && !component.Entity.IsDestroyed)
            {
                ksLog.Error(this, "Entity GameObject '" + gameObject.name + "' belongs to another entity. Your " +
                    "entity factory cannot return game objects that belong to non-destroyed entities.");
                component = null;
                return false;
            }
            if (gameObject.scene.rootCount == 0 || ksReactor.PrefabCache.IsPseudoPrefab(gameObject))
            {
                ksLog.Error(this, "Entity GameObject '" + gameObject.name + "' is a prefab. Your entity factory " +
                    "must return instantiated game objects.");
                component = null;
                return false;
            }
            return true;
        }

        /// <summary>Loads and attaches player scripts to a player.</summary>
        /// <param name="player">Player to attach the <see cref="ksPlayerScript"/> components to.</param>
        protected override void LoadPlayerScripts(ksPlayer player)
        {
            string name = player.IsLocal ? "Local Player" : "Player " + player.Id;
            ksPlayer.IInternals internalPlayer = player;
            if (m_roomType == null)
            {
                internalPlayer.SetGameObject(new GameObject(name));
            }
            else
            {
                internalPlayer.SetGameObject(
                    CloneScripts<ksPlayerScript>(m_roomType.gameObject, internalPlayer.ValidateScript));
                player.GameObject.name = name;
            }
            player.GameObject.transform.parent = GameObject.transform;
            player.GameObject.SetActive(true);
        }

        /// <summary>Initializes room scripts.</summary>
        protected override void InitializeScripts()
        {
            m_component.InitializeScripts();
        }

        /// <summary>Does nothing in Unity. Updates are handled by Unity's MonoBehaviour Update() methods.</summary>
        protected override void UpdateScripts()
        {
        }

        /// <summary>Finds the ksRoomType in the scene of the given type.</summary>
        /// <param name="type">Room type</param>
        /// <returns>True if the room object exists in the scene.</returns>
        private bool FindRoomType(string type)
        {
            GameObject roomTypeObject = GameObject.Find(type);
            if (roomTypeObject == null)
            {
                return false;
            }
            m_roomType = roomTypeObject.GetComponent<ksRoomType>();
            return m_roomType != null;
        }

        /// <summary>
        /// Clones a game object and removes all scripts that aren't of the templated type. Takes
        /// an optional validator delegate for more control of which scripts are copied.
        /// </summary>
        /// <typeparam name="MonobehaviourScript">Script type</typeparam>
        /// <param name="source">Source object to clone the scripts from</param>
        /// <param name="validator">
        /// Callback to invoke with each script of the template type. If it returns false, the script will be removed.
        /// </param>
        /// <returns>Cloned game object.</returns>
        private GameObject CloneScripts<MonobehaviourScript>(
            GameObject source,
            ValidateScriptHandler<MonobehaviourScript> validator = null) where MonobehaviourScript : MonoBehaviour
        {
            source.SetActive(false); // prevent Awake() calls on scripts we remove
            GameObject gameObject = GameObject.Instantiate(source);
            MonoBehaviour[] behaviours = gameObject.GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour behaviour in behaviours)
            {
                MonobehaviourScript script = behaviour as MonobehaviourScript;
                if (script == null || (validator != null && !validator(script)))
                {
                    MonoBehaviour.DestroyImmediate(behaviour);
                }
                else
                {
                    ksRoomScript asRoomScript = script as ksRoomScript;
                    if (asRoomScript != null)
                    {
                        script.enabled = ((ksMonoScript<ksRoom, ksRoomScript>.IInternals)asRoomScript).AddToScriptList;
                        continue;
                    }

                    ksPlayerScript asPlayerScript = script as ksPlayerScript;
                    if (asPlayerScript != null)
                    {
                        script.enabled = ((ksMonoScript<ksPlayer, ksPlayerScript>.IInternals)asPlayerScript).AddToScriptList;
                        continue;
                    }
                }
            }
            source.SetActive(true);
            return gameObject;
        }

        /// <summary>
        /// Finds the default predictors for input prediction and non-input prediction by looking for
        /// <see cref="ksPredictor"/> scripts on <paramref name="gameObject"/>. The first script with
        /// <see cref="ksPredictor.UseWith"/> set to <see cref="ksPredictor.UseModes.CONTROLLER"/> or
        /// <see cref="ksPredictor.UseModes.ALL "/>will be the default input predictor, and the first with
        /// it set to <see cref="ksPredictor.UseModes.NO_CONTROLLER"/> or <see cref="ksPredictor.UseModes.ALL"/>
        /// will be the default non-input predictor.
        /// </summary>
        /// <param name="gameObject">
        /// Game object to get default predictors from. This should be the room type game object.
        /// </param>
        private void FindDefaultPredictors(GameObject gameObject)
        {
            foreach (ksPredictor predictor in gameObject.GetComponents<ksPredictor>())
            {
                if (predictor.UseWith != ksPredictor.UseModes.NO_CONTROLLER && predictor.enabled)
                {
                    if (m_defaultInputPredictor == null)
                    {
                        m_defaultInputPredictor = predictor;
                    }
                    else
                    {
                        ksLog.Warning(this,
                            "Multiple default predictors found for entities with controllers on room " +
                            gameObject.name + ". Using " + m_defaultInputPredictor.GetType().Name);
                    }
                }
                if (predictor.UseWith != ksPredictor.UseModes.CONTROLLER && predictor.enabled)
                {
                    if (m_defaultPredictor == null)
                    {
                        m_defaultPredictor = predictor;
                    }
                    else
                    {
                        ksLog.Warning(this,
                            "Multiple default predictors found for entities without controllers on room " +
                            gameObject.name + ". Using " + m_defaultPredictor.GetType().Name);
                    }
                }
            }
        }

        /// <summary>Invoke managed RPCs on all room scripts.</summary>
        /// <param name="rpcId">RPC ID</param>
        /// <param name="rpcArgs">RPC arguments</param>
        protected override void InvokeRPC(uint rpcId, ksMultiType[] rpcArgs)
        {
            foreach(ksRoomScript script in m_component.Scripts)
            {
                ksRPCManager<ksRPCAttribute>.Instance.InvokeRPC(
                    rpcId, script, script.InstanceType, this, rpcArgs);
            }
        }

        /// <summary>
        /// Factories can only be added once. This is to allow mapping the asset ID to a factory after a match is 
        /// found in <see cref="ksRoom.GetEntityFactory(uint, string)"/>. This avoids repetative regex tests against 
        /// the entity type after a match has been found.
        /// </summary>
        /// <param name="regexPattern"></param>
        /// <param name="factory"></param>
        public void AddEntityFactory(string regexPattern, ksEntityFactory factory)
        {
            if (string.IsNullOrEmpty(regexPattern))
            {
                ksLog.Warning(this, "Entity type pattern cannot be null or empty.");
                return;
            }

            if (factory == null)
            {
                ksLog.Warning(this, "Factory cannot be null.");
                return;
            }

            for (int i = 0; i < m_regexFactories.Count; ++i)
            {
                if (m_regexFactories[i].Key == regexPattern)
                {
                    ksLog.Warning(this, "A factory for this pattern has already been registered.");
                    return;
                }
            }
            m_regexFactories.Add(new KeyValuePair<string, ksEntityFactory>(regexPattern, factory));
        }

        /// <summary>
        /// Look for a factory mapped to the asset ID. If one is not found, then find the first factory mapped 
        /// to a regex pattern that matches the entity type. If a factory was found, add it to the asset mapping
        /// and return the factory. Else if a factory still has not been found, then use the default factory and 
        /// add it to the asset mapping before returning it.
        /// </summary>
        /// <param name="assetId"></param>
        /// <param name="entityType"></param>
        /// <returns></returns>
        public ksEntityFactory GetEntityFactory(uint assetId, string entityType)
        {
            ksEntityFactory factory = null;
            if (m_assetFactories.TryGetValue(assetId, out factory))
            {
                return factory;
            }

            for (int i = m_regexFactories.Count - 1; i >= 0; --i)
            {
                if (Regex.IsMatch(entityType, m_regexFactories[i].Key)) 
                {
                    factory = m_regexFactories[i].Value;
                    break;
                }
            }

            if (factory == null)
            {
                factory = m_defaultEntityFactory;
            }
            m_assetFactories.Add(assetId, factory);
            return factory;
        }

        /// <summary>Apply the server time scale to the unity time object.</summary>
        /// <param name="timeScale">Amount to scale time by.</param>
        /// <returns>True if the time scale was successfully applied to the Unity time object.</returns>
        protected override bool ApplyServerTimeScale(float timeScale)
        {
            if (m_roomType.ApplyServerTimeScale)
            {
                UnityEngine.Time.timeScale = timeScale;
                return true;
            }
            return false;
        }
    }
}
