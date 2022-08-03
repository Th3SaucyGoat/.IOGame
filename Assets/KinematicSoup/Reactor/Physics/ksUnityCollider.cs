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
    /// <summary>Wraps a Unity collider for use with Reactor.</summary>
    public class ksUnityCollider : ksIUnityCollider
    {
        private Collider m_collider = null;
        private ksShape.ShapeTypes m_shapeType = ksShape.ShapeTypes.NO_COLLIDER;
        private ksVector3 m_size = ksVector3.Zero;
        private object m_asset;
        private ksConvexHull m_hull;

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
            get { return m_collider.enabled; }
            set { m_collider.enabled = value; }
        }

        /// <summary>Unity collider component.</summary>
        public Component Component
        {
            get { return m_collider; }
        }

        /// <summary>Unity collider.</summary>
        public Collider Collider
        {
            get { return m_collider; }
        }

        /// <summary>Physics material.</summary>
        public PhysicMaterial Material
        {
            get { return m_collider.sharedMaterial; }
        }

        /// <summary>Get the collider shape type.</summary>
        public ksShape.ShapeTypes ShapeType
        {
            get { return m_shapeType; }
        }

        /// <summary>Get the <see cref="ksEntityComponent"/> on the collider gameobject.</summary>
        private ksEntityComponent EntityComponent
        {
            get { return m_collider != null ? m_collider.gameObject.GetComponent<ksEntityComponent>() : null; }
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
            get { return m_collider.isTrigger; }
            set { m_collider.isTrigger = value; }
        }

        /// <summary>Get/Set the collision filter on the collider.</summary>
        public ksCollisionFilter CollisionFilter
        {
            get {
                ksEntityComponent entity = EntityComponent;
                return entity != null ? entity.GetCollisionFilter(this) : new ksCollisionFilter();
            }
            set {
                ksEntityComponent entity = EntityComponent;
                if (entity)
                {
                    entity.SetCollisionFilter(this, value);
                }
            }
        }

        /// <summary>
        /// Get the bounds of a collider at the position of its attached entity.
        /// </summary>
        public ksBounds Bounds
        {
            get { return m_collider.bounds; }
        }

        /// <summary>Get the collider center.</summary>
        public Vector3 Center
        {
            get {
                switch (m_shapeType)
                {
                    case ksShape.ShapeTypes.SPHERE: return ((SphereCollider)m_collider).center;
                    case ksShape.ShapeTypes.BOX: return ((BoxCollider)m_collider).center;
                    case ksShape.ShapeTypes.CAPSULE: return ((CapsuleCollider)m_collider).center;
                }
                return Vector3.zero;
            }
        }

        /// <summary>Get/Set contact offset on a collider</summary>
        public float ContactOffset
        {
            get { return m_collider.contactOffset; }
            set { m_collider.contactOffset = value; }
        }

        /// <summary>Constructor</summary>
        /// <param name="collider">
        /// Determines shape type base on the Unity collider type. Not all colliders are supported.
        /// </param>
        public ksUnityCollider(Collider collider)
        {
            // Null collider exception
            if ((object)collider == null)
            {
                throw new ArgumentNullException("collider");
            }

            SphereCollider sphere;
            BoxCollider box;
            CapsuleCollider capsule;
            MeshCollider mesh;
            TerrainCollider terrain;
            CharacterController characterController;

            // Determine the shape type and size or asset
            // We extract size and asset from the Unity collider so we can use it for equality comparisons after the
            // Unity collider is destroyed.
            if ((sphere = collider as SphereCollider) != null)
            {
                m_shapeType = ksShape.ShapeTypes.SPHERE;
                m_size.X = sphere.radius;
            }
            else if ((box = collider as BoxCollider) != null)
            {
                m_shapeType = ksShape.ShapeTypes.BOX;
                m_size = box.size;
            }
            else if ((capsule = collider as CapsuleCollider) != null)
            {
                m_shapeType = ksShape.ShapeTypes.CAPSULE;
                m_size = new ksVector3(capsule.radius, capsule.height, capsule.direction);
            }
            else if ((mesh = collider as MeshCollider) != null)
            {
                m_shapeType = mesh.convex ? ksShape.ShapeTypes.CONVEX_MESH : ksShape.ShapeTypes.TRIANGLE_MESH;
                m_asset = mesh.sharedMesh;
            }
            else if ((terrain = collider as TerrainCollider) != null)
            {
                m_shapeType = ksShape.ShapeTypes.HEIGHT_FIELD;
                m_asset = terrain.terrainData;
            }
            else if ((characterController = collider as CharacterController) != null)
            {
                m_shapeType = ksShape.ShapeTypes.CAPSULE_CONTROLLER;
                m_size = new ksVector3(characterController.radius, characterController.height, 1);
            }

            // Invalid shape exception
            if (m_shapeType == ksShape.ShapeTypes.NO_COLLIDER)
            {
                throw new ArgumentException("Unsupported collider type");
            }

            m_collider = collider;
        }

        /// <summary>Calculate the volume of the collider.</summary>
        /// <returns>Volume of the collider.</returns>
        public float Volume()
        {
            switch (m_shapeType)
            {
                case ksShape.ShapeTypes.SPHERE:
                {
                    return 4f * (float)Math.PI * m_size.X * m_size.X * m_size.X / 3f;
                }
                case ksShape.ShapeTypes.BOX:
                {
                    return m_size.X * m_size.Y * m_size.Z;
                }
                case ksShape.ShapeTypes.CAPSULE:
                {
                    return 4f * (float)Math.PI * m_size.X * m_size.X * m_size.X / 3f +
                        (float)Math.PI * m_size.X * m_size.X * Math.Max(0f, (m_size.Y - 2f * m_size.X));
                }
                case ksShape.ShapeTypes.CONVEX_MESH:
                {
                    if (m_hull == null)
                    {
                        m_hull = ksConvexHull.Get((Mesh)m_asset);
                        if (m_hull == null)
                        {
                            return 0f;
                        }
                    }
                    return m_hull.Volume;
                }
            }
            return 0f;
        }

        /// <summary>The geometric center of the collider in local space.</summary>
        /// <returns>Geometric center</returns>
        public Vector3 GeometricCenter()
        {
            if (m_shapeType != ksShape.ShapeTypes.CONVEX_MESH)
            {
                return Center;
            }
            if (m_hull == null)
            {
                m_hull = ksConvexHull.Get((Mesh)m_asset);
                if (m_hull == null)
                {
                    return Vector3.zero;
                }
            }
            return m_hull.GeometricCenter;
        }

        /// <summary>Calculate the center of mass of the collider.</summary>
        /// <returns>Center of mass of the collider.</returns>
        [Obsolete("Use GeometricCenter() instead.")]
        public Vector3 CenterOfMass()
        {
            return GeometricCenter();
        }

        /// <summary>Serializes the collider by shape type.</summary>
        /// <returns>Serialized shape.</returns>
        public byte[] Serialize()
        {
            switch(m_shapeType)
            {
                case ksShape.ShapeTypes.SPHERE: return SerializeSphereCollider((SphereCollider)m_collider);
                case ksShape.ShapeTypes.BOX: return SerializeBoxCollider((BoxCollider)m_collider);
                case ksShape.ShapeTypes.CAPSULE: return SerializeCapsuleCollider((CapsuleCollider)m_collider);
                case ksShape.ShapeTypes.CONVEX_MESH: return SerializeConvexMesh((MeshCollider)m_collider);
                case ksShape.ShapeTypes.TRIANGLE_MESH: return SerializeTriangleMesh((MeshCollider)m_collider);
                case ksShape.ShapeTypes.HEIGHT_FIELD: return SerializeTerrainCollider((TerrainCollider)m_collider);
            }
            return null;
        }

        /// <summary>Serializes radius data from a sphere collider.</summary>
        /// <param name="collider">Sphere collider</param>
        /// <returns>Serialized data.</returns>
        private byte[] SerializeSphereCollider(SphereCollider collider)
        {
            byte[] geometry = new byte[sizeof(float)];
            using (MemoryStream memStream = new MemoryStream(geometry))
            {
                using (BinaryWriter writer = new BinaryWriter(memStream))
                {
                    writer.Write(collider.radius);
                }
            }
            return geometry;
        }

        /// <summary>Serializes x, y, and z data from a box collider.</summary>
        /// <param name="collider">Box collider</param>
        /// <returns>Serialized data.</returns>
        private byte[] SerializeBoxCollider(BoxCollider collider)
        {
            byte[] geometry = new byte[
                3 * sizeof(float)       // width + height + depth
            ];
            using (MemoryStream memStream = new MemoryStream(geometry))
            {
                using (BinaryWriter writer = new BinaryWriter(memStream))
                {
                    writer.Write(collider.size.x);
                    writer.Write(collider.size.y);
                    writer.Write(collider.size.z);
                }
            }
            return geometry;
        }

        /// <summary>Serializes radius, height, and axis data from a capsule collider.</summary>
        /// <param name="collider">Capsule collider</param>
        /// <returns>Serialized data.</returns>
        private byte[] SerializeCapsuleCollider(CapsuleCollider collider)
        {
            byte[] geometry = new byte[
                1 +                     // axis orientation
                2 * sizeof(float)       // radius + height
            ];
            using (MemoryStream memStream = new MemoryStream(geometry))
            {
                using (BinaryWriter writer = new BinaryWriter(memStream))
                {
                    writer.Write((byte)collider.direction);
                    writer.Write(collider.radius);
                    writer.Write(collider.height);
                }
            }
            return geometry;
        }

        /// <summary>Serializes vertex data from a mesh collider.</summary>
        /// <param name="collider">Mesh collider</param>
        /// <returns>Serialized data.</returns>
        private byte[] SerializeConvexMesh(MeshCollider collider)
        {
            if (m_hull == null)
            {
                m_hull = ksConvexHull.Get(collider.sharedMesh);
                if (m_hull == null)
                {
                    return null;
                }
            }
            uint vertexCount = (uint)m_hull.Vertices.Count;
            byte[] geometry = new byte[
                sizeof(int) +                       // vertex count
                vertexCount * 3 * sizeof(float)     // vertex count * vertex position (float + float + float)
            ];
            using (MemoryStream memStream = new MemoryStream(geometry))
            {
                using (BinaryWriter writer = new BinaryWriter(memStream))
                {
                    writer.Write(vertexCount);
                    foreach (Vector3 vertex in m_hull.Vertices)
                    {
                        writer.Write(vertex.x);
                        writer.Write(vertex.y);
                        writer.Write(vertex.z);
                    }
                }
            }
            return geometry;
        }

        /// <summary>Serializes vertex and triangle data from a mesh collider.</summary>
        /// <param name="collider">Mesh collider</param>
        /// <returns>Serialized data.</returns>
        private byte[] SerializeTriangleMesh(MeshCollider collider)
        {
            Mesh mesh = collider.sharedMesh;
            if (mesh == null)
            {
                return null;
            }
            uint vertexCount = (uint)mesh.vertexCount;
            uint triangleCount = (uint)mesh.triangles.Length / 3;
            byte[] geometry = new byte[
                sizeof(int) +                       // vertex count
                vertexCount * 3 * sizeof(float) +   // vertex count * vertex position (float + float + float)
                sizeof(int) +                       // triangle count
                triangleCount * 3 * sizeof(int)     // triangle count * vertex indices (int + int  + int)
            ];
            using (MemoryStream memStream = new MemoryStream(geometry))
            {
                using (BinaryWriter writer = new BinaryWriter(memStream))
                {
                    writer.Write(vertexCount);
                    foreach (Vector3 vertex in mesh.vertices)
                    {
                        writer.Write(vertex.x);
                        writer.Write(vertex.y);
                        writer.Write(vertex.z);
                    }

                    writer.Write(triangleCount);
                    foreach (int vertexIndex in mesh.triangles)
                    {
                        writer.Write((uint)vertexIndex);
                    }
                }
            }
            return geometry;
        }

        /// <summary>Serializes heightmap data from a terrain collider.</summary>
        /// <param name="collider">Terrain collider</param>
        /// <returns>Serialized data.</returns>
        private byte[] SerializeTerrainCollider(TerrainCollider collider)
        {
            TerrainData heightMap = collider.terrainData;
            if (heightMap == null)
            {
                return null;
            }
#if UNITY_2019_3_OR_NEWER
            int xRes = heightMap.heightmapResolution;
            int yRes = heightMap.heightmapResolution;
#else
            int xRes = heightMap.heightmapWidth;
            int yRes = heightMap.heightmapHeight;
#endif

            int dataOffset =
                3 * sizeof(float) +     // x, y, and z sizes (float + float + float)
                2 * sizeof(int) +       // resolution (int + int)
                sizeof(float);          // thickness (float)
            byte[] geometry = new byte[dataOffset + xRes * yRes * sizeof(float)];

            using (MemoryStream memStream = new MemoryStream(geometry))
            {
                using (BinaryWriter writer = new BinaryWriter(memStream))
                {
                    writer.Write(heightMap.size.x);
                    writer.Write(heightMap.size.y);
                    writer.Write(heightMap.size.z);
                    writer.Write(xRes);
                    writer.Write(yRes);
#if UNITY_2019_3_OR_NEWER
                    // Unity removed the thickness property from the terrain data. So we write the default value 1.
                    writer.Write(1);
#else
                    writer.Write(heightMap.thickness);
#endif
                }
            }

            Buffer.BlockCopy(heightMap.GetHeights(0, 0, xRes, yRes), 0, geometry, dataOffset, xRes * yRes * sizeof(float));
            return geometry;
        }

        /// <summary>Compare the geometry sources of each collider and return true if they are equal.</summary>
        /// <param name="other">Collider to compare to.</param>
        /// <returns>True if the collider geometry is equal.</returns>
        public bool IsGeometryEqual(ksIUnityCollider other)
        {
            if (other.ShapeType != m_shapeType)
            {
                return false;
            }
            ksUnityCollider wrapper = (ksUnityCollider)other;
            return m_size == wrapper.m_size && m_asset == wrapper.m_asset;
        }
    }
}
