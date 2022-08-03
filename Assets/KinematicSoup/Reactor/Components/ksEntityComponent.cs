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

using System.Collections.Generic;
using UnityEngine;
using System;

namespace KS.Reactor.Client.Unity
{
    /// <summary>
    /// Attach this to your GameObjects to make the server aware of them when you publish your scene.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu(ksMenuNames.REACTOR + "ksEntityComponent")]
    public class ksEntityComponent : ksScriptsComponent<ksEntity, ksEntityScript>
    {
        /// <summary>
        /// If true, the gameobject will be destroyed when the server destroys the entity.
        /// </summary>
        [Tooltip("If true, the gameobject will be destroyed when the server destroys the entity.")]
        public bool DestroyWithServer = true;

        [Tooltip("Permanent entities are static entities that can never move or be deleted. They are not synced " +
            "over the network since the client will already know where they are. Permanent entities can use Unity's " +
            "static batching to optimize draw calls.")]
        [SerializeField]
        private bool m_isPermanent = true;

        /// <summary>Collision filter for entity groups, notifications, and interactions.</summary>
        [Tooltip("Collision filter for entity groups, notifications, and interactions.")]
        [ksAsset]
        public ksCollisionFilterAsset CollisionFilter = null;

        /// <summary>The physics material for colliders on this entity to use if they do not have a material set.</summary>
        [Tooltip("The physics material for colliders on this entity to use if they do not have a material set.")]
        public PhysicMaterial PhysicsMaterial;

        /// <summary>Modify simulation and scene query properties on indiviual colliders.</summary>
        [Tooltip("Modify simulation and scene query properties on indiviual colliders.")]
        public List<ksColliderData> ColliderData = new List<ksColliderData>();

        /// <summary>
        /// Override room <see cref="ksPhysicsSettings"/> per entity. A value of -1 means that the 
        /// value in the room <see cref="ksPhysicsSettings"/> will be used.
        /// </summary>
        [Tooltip("Override room phyiscs settings per entity. A value of -1 means that the value in the room" +
            "physics settings will be used.")]
        public ksEntityPhysicsSettings PhysicsOverrides = new ksEntityPhysicsSettings(
            -1f, -1f, -1f, -1f, -1f, -1, -1, ksRigidBodyTypes.DEFAULT);

        /// <summary>
        /// Override the precision for this entity used when syncing transform data from servers to clients. 
        /// A value of -1 means that the value in the <see cref="ksRoomType"/> will be used.
        /// </summary>
        [Tooltip("Override the precision for this entity used when syncing transform data from servers to clients. " +
            "A value of -1 means that the value in the room type will be used.")]
        public ksTransformPrecisions TransformPrecisionOverrides = new ksTransformPrecisions(-1f, -1f, -1f);

        /// <summary>
        /// If true, a duplicate server ghost with smoothing disabled will be rendered for entities of 
        /// this type that will show the last known server location. Server ghosts are tinted green.
        /// Note that server ghosts will have all the same scripts as the real object, but no colliders. 
        /// Your scripts can check if they are attached to a ghost using Entity.IsServerGhost.
        /// </summary>
        [Tooltip("If true, a duplicate \"server ghost\" with smoothing disabled will be rendered for entities of " +
            "this type that will show the last known server location. Server ghosts are tinted green.\n\nNote that " +
            "server ghosts will have all the same scripts as the real object, but no colliders. Your scripts can " +
            "check if they are attached to a ghost using Entity.IsServerGhost.")]
        public bool ShowServerGhost;

        /// <summary>
        /// This will override <see cref="ksReactorConfig.DebugConfigs.DefaultServerGhostColor"/>.
        /// If this is clear (0, 0, 0, 0) then the default color will be used.
        /// </summary>
        [Tooltip("Server ghost object color. This will override the default color in the Reactor settings. " +
            "If this is clear (0, 0, 0, 0), the default color will be used.")]
        public Color OverrideServerGhostColor;

        /// <summary>
        /// Server ghost object material. This will override
        /// <see cref="ksReactorConfig.DebugConfigs.DefaultServerGhostMaterial"/>.
        /// If this is null, then the default material will be used.
        /// </summary>
        [Tooltip("Server ghost object material. This will override the default material in the Reactor settings.")]
        public Material OverrideServerGhostMaterial;

        /// <summary>Asset Id</summary>
        [ksReadOnly]
        public uint AssetId = 0;

        /// <summary>Entity Id</summary>
        [ksReadOnly]
        public uint EntityId = 0;

        /// <summary>Center of mass</summary>
        [HideInInspector]
        [Obsolete("Use Entity.RigidBody.CenterOfMass instead.")]
        public Vector3 CenterOfMass = new Vector3();

        /// <summary>Physics Enabled</summary>
        [HideInInspector]
        [Obsolete("Check if the entity has a rigid body instead.")]
        public bool PhysicsEnabled = false;

        /// <summary>Entity Type</summary>
        [HideInInspector]
        public string Type = "";

        /// <summary>Is Static</summary>
        [HideInInspector]
        public bool IsStatic = false;

        /// <summary>Entity</summary>
        public ksEntity Entity
        {
            get { return ParentObject; }
        }

        /// <summary>Is this a permanent entity? Permanent entities never move and cannot be deleted.</summary>
        public bool IsPermanent
        {
            get { return IsStatic && m_isPermanent; }
            set { m_isPermanent = value; }
        }

        /// <summary>
        /// Determines which entities to collide with and notify of collision/overlap events.
        /// </summary>
        public ksCollisionFilter GetCollisionFilter()
        {
            return CollisionFilter == null ?
                ksCollisionFilter.Default : ksReactor.Assets.FromProxy<ksCollisionFilter>(CollisionFilter);
        }

        /// <summary>Gets the collision filter for a collider.</summary>
        /// <param name="collider">Collider to get collision filter for.</param>
        /// <returns>Collision filter for the collider.</returns>
        public ksCollisionFilter GetCollisionFilter(ksIUnityCollider collider)
        {
            ksCollisionFilterAsset filter = GetCollisionFilterAsset(collider);
            return filter == null ?
                ksCollisionFilter.Default : ksReactor.Assets.FromProxy<ksCollisionFilter>(filter);
        }

        /// <summary>Set the collision filter for a collider.</summary>
        /// <param name="collider">Collider to set the collision filter for.</param>
        /// <param name="filter">Collision filter value</param>
        public void SetCollisionFilter(ksIUnityCollider collider, ksCollisionFilter filter)
        {
            ksCollisionFilterAsset filterAsset = GetCollisionFilterAsset(collider);
            if (filterAsset != null)
            {
                if (filter == null)
                {
                    filter = ksCollisionFilter.Default;
                }
                filterAsset.Group = filter.Group;
                filterAsset.Notify = filter.Notify;
                filterAsset.Collide = filter.Collide;
            }
        }

        /// <summary>Initializes the scene if it is not already initialized.</summary>
        private void Awake()
        {
            ksReactor.GetEntityLinker(gameObject.scene).Initialize();
        }

        /// <summary>Destroys the game object if it was not instantiated by the server.</summary>
        private void Start()
        {
            if (Entity == null)
            {
                foreach (ksEntityScript script in gameObject.GetComponents<ksEntityScript>())
                {
                    script.enabled = false;
                }
            }
        }

        /// <summary>Gets all colliders on an entity that are supported by Reactor.</summary>
        /// <param name="includeCharacterController"></param>
        /// <returns>List of colliders.</returns>
        public List<ksIUnityCollider> GetColliders(bool includeCharacterController = false)
        {
            List<ksIUnityCollider> colliders = new List<ksIUnityCollider>();
            foreach (Component component in GetComponents<Component>())
            {
                ksIUnityCollider collider = GetCollider(component, includeCharacterController);
                if (collider != null)
                {
                    colliders.Add(collider);
                }
            }
            return colliders;
        }

        /// <summary>Converts a Unity component into a <see cref="ksIUnityCollider"/>.</summary>
        /// <param name="component">component to convert</param>
        /// <param name="includeCharacterController"></param>
        /// <returns>Collider, or null if the component cannot be converted.</returns>
        public ksIUnityCollider GetCollider(Component component, bool includeCharacterController = false)
        {
            ksIUnityCollider collider = component as ksIUnityCollider;
            if (collider != null)
            {
                return collider;
            }

            if (component is SphereCollider ||
                component is BoxCollider ||
                component is CapsuleCollider ||
                component is MeshCollider ||
                component is TerrainCollider ||
                includeCharacterController && component is CharacterController)
            {
                collider = new ksUnityCollider((Collider)component);
                return collider;
            }

            return collider;
        }

        /// <summary>Gets the first enabled collider of the given type on an entity.</summary>
        /// <typeparam name="ColliderType">Unity collider type.</typeparam>
        /// <returns>
        /// First enabled collider of <typeparamref name="ColliderType"/> on the entity, or null if none exist.
        /// </returns>
        public ColliderType GetCollider<ColliderType>() where ColliderType : Collider
        {
            ColliderType[] colliders = GetComponents<ColliderType>();
            foreach (ColliderType collider in colliders)
            {
                if (collider.enabled)
                    return collider;
            }
            return null;
        }

        /// <summary>Gets the first enabled monobehaviour of the given type on an entity.</summary>
        /// <typeparam name="MonobehaviourType">Unity Monobehaviour type.</typeparam>
        /// <returns>
        /// First enabled collider of <typeparamref name="MonobehaviourType"/> on the entity, or null if none exist.
        /// </returns>
        public MonobehaviourType GetMonoBehaviour<MonobehaviourType>() where MonobehaviourType : MonoBehaviour
        {
            MonobehaviourType[] behaviours = GetComponents<MonobehaviourType>();
            foreach (MonobehaviourType behaviour in behaviours)
            {
                if (behaviour.enabled)
                    return behaviour;
            }
            return null;
        }

        /// <summary>
        /// Get the collider data structure for a collider.
        /// </summary>
        /// <param name="collider"></param>
        /// <returns></returns>
        public bool TryGetColliderData(ksIUnityCollider collider, out ksColliderData colliderData)
        {
            foreach (ksColliderData data in ColliderData)
            {
                if (data.Collider == collider.Component)
                {
                    colliderData = data;
                    return true;
                }
            }
            colliderData = null;
            return false;
        }

        /// <summary>Returns the collision filter for a collider.</summary>
        /// <param name="collider">Collider to get <see cref="ksCollisionFilterAsset"/> for.</param>
        /// <param name="defaultFromEntity">
        /// If the collision filter for the collider isn't set, returns the filter from the entity 
        /// if this is true, and null if this is false.
        /// </param> 
        /// <returns>Collision filter assigned to the entity collider.</returns>
        public ksCollisionFilterAsset GetCollisionFilterAsset(ksIUnityCollider collider, bool defaultFromEntity = true)
        {
            foreach (ksColliderData data in ColliderData)
            {
                if (data.Collider == collider.Component)
                {
                    if (data.Filter != null)
                    {
                        return data.Filter;
                    }
                    return defaultFromEntity ? CollisionFilter : null;
                }
            }
            return defaultFromEntity ? CollisionFilter : null;
        }

        /// <summary>Returns the shape ID for a collider.</summary>
        /// <param name="collider">Collider to get the shape ID for.</param>
        /// <returns>Assigned shape ID. (0 if there is no assigned ID) </returns>
        public int GetShapeId(ksIUnityCollider collider)
        {
            foreach (ksColliderData data in ColliderData)
            {
                if (data.Collider == collider.Component)
                {
                    return data.ShapeId;
                }
            }
            return 0;
        }

        /// <summary>Sets the shape ID for a collider.</summary>
        /// <param name="collider">Collider to set the shape ID on.</param>
        /// <param name="shapeId">Shape ID</param>
        /// <returns>True if the shape ID changed.</returns>
        public bool SetShapeId(ksIUnityCollider collider, int shapeId)
        {
            foreach (ksColliderData data in ColliderData)
            {
                if (data.Collider == collider.Component)
                {
                    if (data.ShapeId != shapeId)
                    {
                        data.ShapeId = shapeId;
                        return true;
                    }
                    return false;
                }
            }
            {
                ksColliderData data = new ksColliderData();
                data.Collider = collider.Component;
                data.ShapeId = shapeId;
                ColliderData.Add(data);
            }
            return true;
        }

        /// <summary>Detects the shape type of this entity.</summary>
        /// <returns>Shape type</returns>
        public ksShape.ShapeTypes DetectShapeType()
        {
            if (GetCollider<SphereCollider>() != null)
                return ksShape.ShapeTypes.SPHERE;
            if (GetCollider<BoxCollider>() != null)
                return ksShape.ShapeTypes.BOX;
            if (GetCollider<CapsuleCollider>() != null)
                return ksShape.ShapeTypes.CAPSULE;
            if (GetMonoBehaviour<ksCylinderCollider>() != null)
                return ksShape.ShapeTypes.CYLINDER;
            if (GetMonoBehaviour<ksConeCollider>() != null)
                return ksShape.ShapeTypes.CONE;
            if (GetCollider<MeshCollider>() != null)
            {
                if (GetCollider<MeshCollider>().convex)
                {
                    return ksShape.ShapeTypes.CONVEX_MESH;
                }
                else
                {
                    return ksShape.ShapeTypes.TRIANGLE_MESH;
                }
            }
            if (GetCollider<TerrainCollider>() != null)
                return ksShape.ShapeTypes.HEIGHT_FIELD;
            if (GetComponent<CharacterController>() != null)
                return ksShape.ShapeTypes.CAPSULE_CONTROLLER;
            return ksShape.ShapeTypes.NO_COLLIDER;
        }

        /// <summary>Handle character controller collider hit event.</summary>
        /// <param name="hit">Hit information.</param>
        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            ksUnityCharacterController controller = Entity.CharacterController as ksUnityCharacterController;
            if (controller != null)
            {
                controller.InvokeColliderHitHandler(hit);
            }
        }
    }
}