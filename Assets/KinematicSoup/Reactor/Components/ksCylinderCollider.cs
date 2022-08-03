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
using UnityEngine;

namespace KS.Reactor.Client.Unity
{
    /// <summary>Cylinder collider component.</summary>
    [AddComponentMenu("Physics/Cylinder Collider", -1)]
    public class ksCylinderCollider : MonoBehaviour, ksIUnityCollider
    {
        /// <summary>Get the entity this collider is attached to.</summary>
        public ksIEntity Entity
        {
            get
            {
                ksEntityComponent entityComponent = EntityComponent;
                return entityComponent == null ? null : entityComponent.Entity;
            }
        }

        /// <summary>Get/Set the enabled state of a collider</summary>
        public bool IsEnabled
        {
            get { return enabled; }
            set 
            { 
                enabled = value;
                if (m_childCollider != null)
                {
                    m_childCollider.enabled = value;
                }
            }
        }

        /// <summary>The component this interface is for.</summary>
        public Component Component
        {
            get { return this; }
        }

        /// <summary>The collider associated with the component.</summary>
        public Collider Collider
        {
            get { return m_childCollider; }
        }

        /// <summary>Get the collider shape type.</summary>
        public ksShape.ShapeTypes ShapeType
        {
            get { return ksShape.ShapeTypes.CYLINDER; }
        }

        /// <summary>Get the <see cref="ksEntityComponent"/> on the collider gameobject.</summary>
        private ksEntityComponent EntityComponent
        {
            get {
                ksEntityComponent c = gameObject.GetComponent<ksEntityComponent>();
                return c;
            }
        }

        /// <summary>Get/Set if this collider is used in scene queries.</summary>
        public bool IsQueryCollider
        {
            get
            {
                ksEntityComponent entity = EntityComponent;
                ksColliderData colliderData;
                if (entity != null && entity.TryGetColliderData(this, out colliderData))
                {
                    return colliderData.IsQuery;
                }
                ksLog.Warning(this, "Missing Entity Component");
                return true;
            }
            set
            {
                ksEntityComponent entity = EntityComponent;
                ksColliderData colliderData;
                if (entity != null && entity.TryGetColliderData(this, out colliderData))
                {
                    colliderData.IsQuery = value;
                }
            }
        }

        /// <summary>Get/Set if this collider is used in physics simulations.</summary>
        public bool IsSimulationCollider
        {
            get
            {
                ksEntityComponent entity = EntityComponent;
                ksColliderData colliderData;
                if (entity != null && entity.TryGetColliderData(this, out colliderData))
                {
                    return colliderData.IsSimulation;
                }
                ksLog.Warning(this, "Missing Entity Component");
                return true;
            }
            set
            {
                ksEntityComponent entity = EntityComponent;
                ksColliderData colliderData;
                if (entity != null && entity.TryGetColliderData(this, out colliderData))
                {
                    colliderData.IsSimulation = value;
                }
            }
        }

        /// <summary>Get/Set if this collider is a trigger.</summary>
        public bool IsTrigger
        {
            get { return m_isTrigger; }
            set
            {
                m_isTrigger = value;
                if (m_childCollider != null)
                {
                    m_childCollider.isTrigger = value;
                }
            }
        }
        [SerializeField]
        private bool m_isTrigger = false;

        /// <summary>Get/Set the collision filter on the collider.</summary>
        public ksCollisionFilter CollisionFilter
        {
            get
            {
                ksEntityComponent entity = EntityComponent;
                return entity != null ? entity.GetCollisionFilter(this) : new ksCollisionFilter();
            }
            set
            {
                ksEntityComponent entity = EntityComponent;
                if (entity)
                {
                    entity.SetCollisionFilter(this, value);
                }
            }
        }

        /// <summary>
        /// Get the bounds of a collider at the position of it attached entity.
        /// </summary>
        public ksBounds Bounds
        {
            get { return m_childCollider.bounds; }
        }

        /// <summary>Physics material</summary>
        public PhysicMaterial Material
        {
            get { return m_material; }
            set { m_material = value; }
        }
        [SerializeField]
        private PhysicMaterial m_material;

        /// <summary>Collider center relative to the entity position.</summary>
        public Vector3 Center
        {
            get { return m_center; }
            set { m_center = value; }
        }
        [SerializeField]
        private Vector3 m_center = Vector3.zero;

        /// <summary>Cylinder height</summary>
        public float Height
        {
            get { return m_height; }
            set { m_height = value; }
        }
        [SerializeField]
        private float m_height = 1.0f;

        /// <summary>Cylinder radius</summary>
        public float Radius
        {
            get { return m_radius; }
            set { m_radius = value; }
        }
        [SerializeField]
        private float m_radius = 0.5f;

        /// <summary>Number of sides on the mesh appoximation.</summary>
        public int NumSides
        {
            get { return m_numSides; }
            set { m_numSides = value; }
        }
        [SerializeField]
        private int m_numSides = 20;

        /// <summary>Axis alignment</summary>
        public ksShape.Axis Direction
        {
            get { return m_direction; }
            set { m_direction = value; }
        }
        [SerializeField]
        private ksShape.Axis m_direction = ksShape.Axis.Y;

        /// <summary>
        /// Is scale initialized? If false, will set scale to fit the mesh when this component is inspected.
        /// </summary>
        public bool Initialized
        {
            get { return m_initialized; }
            set { m_initialized = value; }
        }
        [SerializeField]
        private bool m_initialized;

        /// <summary>Get/Set contact offset on a collider</summary>
        public float ContactOffset {
            get { return m_childCollider.contactOffset; }
            set { m_childCollider.contactOffset = value; }
        }

        private GameObject m_child;
        private MeshCollider m_childCollider;

        /// <summary>
        /// Unity start method. Generates a collision mesh and attaches it to a hidden child object.
        /// </summary>
        public void Start()
        {
            m_child = new GameObject();
            m_child.hideFlags = HideFlags.HideInHierarchy;
            m_child.transform.parent = transform;
            m_child.transform.localPosition = m_center;
            m_child.transform.localScale = Vector3.one;
            m_child.transform.localRotation = Quaternion.identity;
            m_child.AddComponent<ksEntity.Indicator>().Collider = this;
            m_childCollider = m_child.AddComponent<MeshCollider>();
            m_childCollider.isTrigger = m_isTrigger;
            m_childCollider.sharedMesh = ksCylinderMeshFactory.GetMesh(m_radius, m_height, m_direction, m_numSides);
            m_childCollider.convex = true;
        }

        /// <summary>
        /// Called when this script is detached. Removes the hidden child object with generated collision mesh.
        /// </summary>
        public void OnDestroy()
        {
            if (m_child != null)
            {
                Destroy(m_child);
                m_child = null;
            }
        }

        /// <summary>Volume of the cylinder.</summary>
        /// <returns>Volume</returns>
        public float Volume()
        {
            return (float)Math.PI * m_radius * m_radius * m_height;
        }

        /// <summary>The geometric center of the cylinder in local space.</summary>
        /// <returns>Geometric center</returns>
        public Vector3 GeometricCenter()
        {
            return m_center;
        }

        /// <summary>Center of mass of the cylinder.</summary>
        /// <returns>Center of mass</returns>
        [Obsolete("Use GeometricCenter() instead.")]
        public Vector3 CenterOfMass()
        {
            return GeometricCenter();
        }

        /// <summary>Serializes the cylinder collider by generating convex hull vertices.</summary>
        /// <returns>Serialized geometry</returns>
        public byte[] Serialize()
        {
            Vector3[] vertices = ksCylinderMeshFactory.GenerateVertices(m_radius, m_height, m_direction, m_numSides);
            byte[] geometry = new byte[
               sizeof(int) +                       // vertex count
               vertices.Length * 3 * sizeof(float) // vertex count * vertex position (float + float + float)
            ];
            using (MemoryStream memStream = new MemoryStream(geometry))
            {
                using (BinaryWriter writer = new BinaryWriter(memStream))
                {
                    writer.Write((uint)vertices.Length);
                    foreach (Vector3 vertex in vertices)
                    {
                        writer.Write(vertex.x);
                        writer.Write(vertex.y);
                        writer.Write(vertex.z);
                    }
                }
            }

            return geometry;
        }

        /// <summary>Compare the geometry sources of each collider and return true if they are equal.</summary>
        /// <param name="other">Collider to compare to.</param>
        /// <returns>True if the collider geometry is equal.</returns>
        public bool IsGeometryEqual(ksIUnityCollider other)
        {
            if (other.ShapeType != ShapeType)
            {
                return false;
            }

            ksCylinderCollider b = (ksCylinderCollider)other;
            return m_radius == b.m_radius &&
                m_height == b.m_height &&
                m_numSides == b.m_numSides &&
                m_direction == b.m_direction;
        }
    }
}

