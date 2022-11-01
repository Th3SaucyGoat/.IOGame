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

namespace KS.Reactor.Client.Unity
{
    /// <summary>
    /// Unity implementation of ksBaseEntity. Entities are objects create, updated, and destroyed by a 
    /// server and replicated to clients.
    /// </summary>
    public class ksEntity : ksBaseEntity, ksEntity.IInternals
    {
        /// <summary>
        /// Explicit interface definition. Methods defined in this interface will only be accessible when 
        /// <see cref="ksEntity"/> instances are cast to this interface.
        /// </summary>
        public interface IInternals
        {
            void Initialize(uint id, uint assetId, string type, ksEntityComponent component, bool isSceneEntity);
        }

        /// <summary>GameObject that represents this entity.</summary>
        public GameObject GameObject
        {
            get { return m_gameObject; }
        }
        private GameObject m_gameObject;

        /// <summary>Room the entity belongs to.</summary>
        public ksRoom Room
        {
            get { return (ksRoom)BaseRoom; }
        }

        /// <summary>Asset ID for prefab.</summary>
        public uint AssetId
        {
            get { return m_assetId; }
        }
        private uint m_assetId;

        /// <summary>Entity position</summary>
        public override ksVector3 Position 
        {
            get { return base.Position; }
            protected set
            {
                if (m_unityCharacterController != null)
                {
                    m_unityCharacterController.enabled = false;
                }
                if (m_gameObject != null)
                {
                    m_gameObject.transform.position = value;
                }
                if (m_unityCharacterController != null)
                {
                    m_unityCharacterController.enabled = true;
                }
                base.Position = value;
            }
        }

        /// <summary>Entity rotation</summary>
        public override ksQuaternion Rotation
        {
            get { return base.Rotation; }
            protected set
            {
                base.Rotation = value;
                if (m_gameObject != null)
                {
                    m_gameObject.transform.rotation = value;
                }
            }
        }

        /// <summary>Entity scale</summary>
        public override ksVector3 Scale
        {
            get { return base.Scale; }
            protected set
            {
                base.Scale = value;
                if (m_gameObject != null)
                {
                    m_gameObject.transform.localScale = value;
                }
            }
        }

        /// <summary>
        /// If true then the Unity game object will be destroyed when this entity is destroyed by the server.
        /// </summary>
        public bool DestroyWithServer
        {
            get { return m_component.DestroyWithServer; }
            set { m_component.DestroyWithServer = value; }
        }

        /// <summary>Collision shape used in sweep and overlap tests.</summary>
        public ksICollisionShape Shape
        {
            get { return Sweeper.Shape; }
        }

        /**
         * The entity's rigid body. Null if the entity does not have a rigid body.
         */
        public new ksUnityRigidBody RigidBody
        {
            get { return (ksUnityRigidBody)m_rigidBody; }
            set { base.RigidBody = value; }
        }

        /**
         * The entity's 2D rigid body. Null if the entity does not have a 2D rigid body.
         */
        public new ksUnityRigidBody2DView RigidBody2D
        {
            get { return (ksUnityRigidBody2DView)m_rigidBody2D; }
            set { base.RigidBody2D = value; }
        }

        /// <summary>Object used to perform sweep and overlap tests for this entity.</summary>
        private ksEntitySweeper Sweeper
        {
            get
            {
                if (m_sweeper == null)
                {
                    m_sweeper = new ksEntitySweeper(m_component);
                }
                return m_sweeper;
            }
        }
        private ksEntitySweeper m_sweeper;

        private ksEntityComponent m_component;
        private bool m_visible = true;
        private bool m_isSceneEntity = false;

        private CharacterController m_unityCharacterController = null;

        /// <summary>Default constructor</summary>
        public ksEntity()
        {
        }

        /// <summary>Initialize the entity.</summary>
        /// <param name="id">Entity ID</param>
        /// <param name="assetId">Asset ID</param>
        /// <param name="type">Entity type</param>
        /// <param name="component">Monobehaviour component containing entity configuration data.</param>
        /// <param isSceneEntity="type">Was the entity defined in the scene hierarchy</param>
        void IInternals.Initialize(uint id, uint assetId, string type, ksEntityComponent component, bool isSceneEntity)
        {
            Id = id;
            component.EntityId = id;
            Type = type;
            m_assetId = assetId;
            m_component = component;
            m_gameObject = component.gameObject;
            m_unityCharacterController = m_gameObject.GetComponent<CharacterController>();
            if (m_unityCharacterController != null)
            {
                m_characterController = new ksUnityCharacterController(this, m_unityCharacterController);
            }
            HasServerGhost = component.ShowServerGhost;
            CollisionFilter = component.GetCollisionFilter();
            component.ParentObject = this;
            m_isSceneEntity = isSceneEntity;

            if (component.IsPermanent)
            {
                Position = m_gameObject.transform.position;
                Rotation = m_gameObject.transform.rotation;
                Scale = m_gameObject.transform.localScale;
            }

            Rigidbody rigidbody = m_gameObject.GetComponent<Rigidbody>();
            if (rigidbody != null)
            {
                // Make non-simulation colliders into triggers so they are excluded from center of mass calculations.
                foreach (ksIUnityCollider collider in m_component.GetColliders())
                {
                    if (!collider.IsSimulationCollider)
                    {
                        collider.IsTrigger = true;
                    }
                }
                // Constructing the wrapper will make the rigidbody kinematic, which must be done after we make the
                // non-simulation colliders into triggers in order for center of mass to be updated.
                ksRigidBodyTypes rigidBodyType = m_component.PhysicsOverrides.RigidBodyType;
                if (rigidBodyType == ksRigidBodyTypes.DEFAULT)
                {
                    rigidBodyType = ksBaseUnityRigidBody.DefaultType;
                }
                if (rigidBodyType == ksRigidBodyTypes.RIGID_BODY_2D)
                {
                    m_rigidBody2D = new ksUnityRigidBody2DView(rigidbody);
                }
                else
                {
                    m_rigidBody = new ksUnityRigidBody(rigidbody);
                }
            }

            // Destroy all joints connected to the world (null) or to other game objects with ksEntityComponents
            Joint[] joints = m_gameObject.GetComponents<Joint>();
            if (joints != null)
            {
                foreach (Joint joint in joints)
                {
                    if (joint.connectedBody == null || joint.connectedBody.GetComponents<ksEntityComponent>() != null)
                    {
                        GameObject.Destroy(joint);
                    }
                }
            }
        }

        /// <summary>Initializes all <see cref="ksEntityScript"/> attached to this entity.</summary>
        protected override void InitializeScripts()
        {
            m_component.InitializeScripts();
        }

        /// <summary>
        /// Cleans up the entity and returns it to a pool of reusable entity resources.
        /// </summary>
        /// <param name="isSyncGroupRemoval">
        /// True when the entity is destroyed because it is in a different sync group than the local player.
        /// </param>
        protected override void Destroy(bool isSyncGroupRemoval)
        {
            // Detach scripts before destroying the game object.
            if (m_component.IsInitialized)
            {
                foreach (ksEntityScript script in m_component.Scripts)
                {
                    script.Detached();
                }
            }
            if ((DestroyWithServer || !Room.IsConnected) && !IsPermanent)
            {
                if (!m_isSceneEntity)
                {
                    Room.GetEntityFactory(AssetId, Type).ReleaseGameObject(this);
                }
                else
                {
                    if (isSyncGroupRemoval)
                    {
                        ksReactor.GetEntityLinker(m_gameObject.scene).TrackEntity(m_component);
                    }
                    else
                    {
                        GameObject.Destroy(m_gameObject);
                    }
                }
            }
            // Clean up object for reuse
            m_sweeper = null;
            m_gameObject = null;
            m_component.CleanUp();
            m_component = null;
            CleanUp();
            ksObjectPool<ksEntity>.Instance.Return(this);
        }

        /// <summary>Resolves penetrations resulting from moving between two points.</summary>
        /// <param name="from">Start position</param>
        /// <param name="to">End position</param>
        /// <param name="rotation">Orientation during the move.</param>
        /// <returns>
        /// Position between start and end points before penetration occurred, or the end position if 
        /// no penetration occurred.
        /// </returns>
        protected override ksVector3 ResolvePenetration(ksVector3 from, ksVector3 to, ksQuaternion rotation)
        {
            return Sweeper.ResolvePenetration(from, to, rotation);
        }

        /// <summary>
        /// Checks if the entity will collide with anything when moving between two points.
        /// </summary>
        /// <param name="from">Start position</param>
        /// <param name="to">End position</param>
        /// <param name="rotation">Orientation during the sweep</param>
        /// <param name="position">
        /// Position of the entity at the end of the sweep.If a collision occurred, this is the entity's position
        /// when it happened.
        /// </param>
        /// <param name="normal">Normal of collision surface.</param>
        /// <returns>True if a collision occured.</returns>
        protected override bool Sweep(ksVector3 from, ksVector3 to, ksQuaternion rotation,
            out ksVector3 position, out ksVector3 normal)
        {
            return Sweeper.Sweep(from, to, rotation, out position, out normal);
        }

        /// <summary>
        /// Sets material and colour on all renderers to indicate it is a server ghost and removes colliders
        /// so it does not interfere with collision prediction.
        /// </summary>
        protected override void BecomeServerGhost()
        {
            ApplyServerGhostMaterial();
            
            foreach (Component c in m_component.GetComponentsInChildren<Component>())
            {
                if (c is Collider || c is ksIUnityCollider)
                {
                    GameObject.Destroy(c);
                }
            }
            m_characterController = null;

            m_gameObject.name += " (ghost)";
            DestroyWithServer = true;
        }

        /// <summary>Applies ghost material and color to the ghost object's renderers.</summary>
        public void ApplyServerGhostMaterial()
        {
            if (Room == null)
            {
                return;
            }
            ksEntity ghostEntity = null;
            if (IsServerGhost)
            {
                ghostEntity = this;
            }
            else
            {
                ghostEntity = Room.GetServerGhostEntity(Id);
                if (ghostEntity == null)
                {
                    return;
                }
                ghostEntity.m_component.OverrideServerGhostColor = m_component.OverrideServerGhostColor;
                ghostEntity.m_component.OverrideServerGhostMaterial =
                    m_component.OverrideServerGhostMaterial;
            }
            if (ghostEntity.GameObject != null)
            {
                foreach (Renderer renderer in ghostEntity.GameObject.GetComponentsInChildren<Renderer>())
                {
                    if (m_component.OverrideServerGhostMaterial != null)
                    {
                        renderer.material = m_component.OverrideServerGhostMaterial;
                    }
                    else if (ksReactorConfig.Instance.Debug.DefaultServerGhostMaterial != null)
                    {
                        renderer.material = ksReactorConfig.Instance.Debug.DefaultServerGhostMaterial;
                    }
                    if (m_component.OverrideServerGhostColor != Color.clear)
                    {
                        renderer.material.color = m_component.OverrideServerGhostColor;
                    }
                    else
                    {
                        renderer.material.color = ksReactorConfig.Instance.Debug.DefaultServerGhostColor;
                    }
                }
            }
        }

        /// <summary>Toggle rendering of the entity game object.</summary>
        /// <param name="visible">If false, the renderers on the game object and its children will be disabled.</param>
        public override void SetVisible(bool visible)
        {
            if (m_visible == visible)
            {
                return;
            }
            m_visible = visible;
            Renderer[] renderers = m_gameObject.GetComponents<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                renderer.enabled = visible;
            }
        }

        /// <summary>
        /// Our non-standard colliders generate collision meshes which are attached as hidden children to the
        /// game object. We attach this indicator to those child game objects to indicate that we should check
        /// the parent to find the <see cref="ksEntityComponent"/> for that game object.
        /// </summary>
        public class Indicator : MonoBehaviour
        {
            /// <summary>Collider that created the child game object this indicator is attached to.</summary>
            public ksIUnityCollider Collider;
        }

        /// <summary>Invoke managed RPCs on all entity scripts.</summary>
        /// <param name="rpcId">ID of the RPC to invoke</param>
        /// <param name="rpcArgs">Arguments to pass to the RPC handlers</param>
        protected override void InvokeRPC(uint rpcId, ksMultiType[] rpcArgs)
        {
            foreach(ksEntityScript script in m_component.Scripts)
            {
                ksRPCManager<ksRPCAttribute>.Instance.InvokeRPC(
                    rpcId, script, script.InstanceType, Room, rpcArgs);
            }
        }
    }
}
