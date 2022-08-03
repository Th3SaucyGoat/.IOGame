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

namespace KS.Reactor.Client.Unity
{
    /// <summary>Collision shape interface for doing sweep and overlap tests using Unity's API.</summary>
    public interface ksICollisionShape
    {
        /// <summary>Description of the collider shape.</summary>
        string Description { get; }

        /// <summary>Sweeps the shape and reports collisions.</summary>
        /// <param name="position">Start position</param>
        /// <param name="rotation">Rotation</param>
        /// <param name="delta">Sweep vector</param>
        /// <returns>List of collisions.</returns>
        RaycastHit[] Sweep(ksVector3 position, ksQuaternion rotation, ksVector3 delta);

        /// <summary>Checks for colliders that overlap with the shape.</summary>
        /// <param name="position">Position</param>
        /// <param name="rotation">Rotation</param>
        /// <returns>Colliders overlapping this shape.</returns>
        Collider[] Overlap(ksVector3 position, ksQuaternion rotation);
    }

    /// <summary>Collision box.</summary>
    public class CollisionBox : ksICollisionShape
    {
        private ksVector3 m_halfExtents;
        private ksVector3 m_offset;

        /// <summary>Description of the box.</summary>
        public string Description
        {
            get { return "box"; }
        }

        /// <summary>Constructor</summary>
        /// <param name="halfExtents">Vector representing the half extents of the box.</param>
        /// <param name="offset">Offset relative to the entity position.</param>
        public CollisionBox(ksVector3 halfExtents, ksVector3 offset)
        {
            m_halfExtents = halfExtents;
            m_offset = offset;
        }

        /// <summary>Sweeps the box and reports collisions.</summary>
        /// <param name="position">Start position</param>
        /// <param name="rotation">Rotation</param>
        /// <param name="delta">Sweep vector</param>
        /// <returns>List of collisions.</returns>
        public RaycastHit[] Sweep(ksVector3 position, ksQuaternion rotation, ksVector3 delta)
        {
            return Physics.BoxCastAll(position + m_offset, m_halfExtents, delta, rotation, delta.Magnitude());
        }

        /// <summary>Returns an array of colliders overlapping this shape.</summary>
        /// <param name="position">Position</param>
        /// <param name="rotation">Rotation</param>
        /// <returns>Colliders overlapping this shape.</returns>
        public Collider[] Overlap(ksVector3 position, ksQuaternion rotation)
        {
            return Physics.OverlapBox(position + m_offset, m_halfExtents, rotation);
        }
    }

    /// <summary>Collision sphere.</summary>
    public class CollisionSphere : ksICollisionShape
    {
        /// <summary>Radius</summary>
        public float Radius
        {
            get { return m_radius; }
        }
        private float m_radius;

        private ksVector3 m_offset;

        /// <summary>Constructor</summary>
        /// <param name="radius">Radius</param>
        /// <param name="offset">Offset relative to the entity position.</param>
        public CollisionSphere(float radius, ksVector3 offset)
        {
            m_radius = radius;
            m_offset = offset;
        }

        /// <summary>Description of the collider shape.</summary>
        public string Description
        {
            get { return "sphere with radius " + m_radius; }
        }

        /// <summary>Sweeps the shape and reports collisions.</summary>
        /// <param name="position">Start position</param>
        /// <param name="rotation">Rotation</param>
        /// <param name="delta">Sweep vector</param>
        /// <returns>List of collisions.</returns>
        public RaycastHit[] Sweep(ksVector3 position, ksQuaternion rotation, ksVector3 delta)
        {
            return Physics.SphereCastAll(position + m_offset * rotation, m_radius, delta, delta.Magnitude());
        }

        /// <summary>Returns an array of colliders overlapping this shape.</summary>
        /// <param name="position">Position</param>
        /// <param name="rotation">Rotation</param>
        /// <returns>Colliders overlapping this shape.</returns>
        public Collider[] Overlap(ksVector3 position, ksQuaternion rotation)
        {
            return Physics.OverlapSphere(position + m_offset * rotation, m_radius);
        }
    }

    /// <summary>Collision capsule.</summary>
    public class CollisionCapsule : ksICollisionShape
    {
        /// <summary>Radius</summary>
        public float Radius
        {
            get { return m_radius; }
        }
        private float m_radius;

        /// <summary>Height</summary>
        public float Height
        {
            get { return m_height; }
        }
        private float m_height;

        /// <summary>Position offset.</summary>
        public ksVector3 PositionOffset
        {
            get { return m_positionOffset; }
        }
        private ksVector3 m_positionOffset;

        /// <summary>Rotation offset.</summary>
        public ksQuaternion RotationOffset
        {
            get { return m_rotationOffset; }
        }
        private ksQuaternion m_rotationOffset;

        /// <summary>Constructor</summary>
        /// <param name="radius">Radius</param>
        /// <param name="height">Height</param>
        /// <param name="positionOffset">Offset relative to the entity position.</param>
        /// <param name="rotationOffset">Rotation relative to the entity rotation.</param>
        public CollisionCapsule(float radius, float height, ksVector3 positionOffset, ksQuaternion rotationOffset)
        {
            m_radius = radius;
            m_height = height;
            m_positionOffset = positionOffset;
            m_rotationOffset = rotationOffset;
        }

        /// <summary>Description of the collider shape.</summary>
        public string Description
        {
            get { return "capsule"; }
        }

        /// <summary>Sweeps the shape and reports collisions.</summary>
        /// <param name="position">Start position</param>
        /// <param name="rotation">Rotation</param>
        /// <param name="delta">Sweep vector</param>
        /// <returns>List of collisions.</returns>
        public RaycastHit[] Sweep(ksVector3 position, ksQuaternion rotation, ksVector3 delta)
        {
            ksVector3 point1, point2;
            GetEndPoints(position, rotation, out point1, out point2);
            return Physics.CapsuleCastAll(point1, point2, m_radius, delta, delta.Magnitude());
        }

        /// <summary>Returns an array of colliders overlapping this shape.</summary>
        /// <param name="position">Position</param>
        /// <param name="rotation">Rotation</param>
        /// <returns>Colliders overlapping this shape.</returns>
        public Collider[] Overlap(ksVector3 position, ksQuaternion rotation)
        {
            ksVector3 point1, point2;
            GetEndPoints(position, rotation, out point1, out point2);
            ksVector3 delta = point2 - point1;
            RaycastHit[] hits = Physics.SphereCastAll(point1, m_radius, delta, delta.Magnitude());
            Collider[] colliders = Physics.OverlapSphere(point1, m_radius);
            List<Collider> colliderList = new List<Collider>(colliders);
            foreach (RaycastHit hit in hits)
            {
                if (!colliderList.Contains(hit.collider))
                    colliderList.Add(hit.collider);
            }
            return colliderList.ToArray();
        }

        /// <summary>Gets the center of two spheres on either end of the capsule.</summary>
        /// <param name="position">Position</param>
        /// <param name="rotation">Rotation</param>
        /// <param name="point1">Point1</param>
        /// <param name="point2">Point2</param>
        private void GetEndPoints(
            ksVector3 position, 
            ksQuaternion rotation, 
            out ksVector3 point1, 
            out ksVector3 point2)
        {
            position += m_positionOffset * rotation;
            ksVector3 offset = new ksVector3(0, m_height / 2 - m_radius, 0) * (rotation * m_rotationOffset);
            point1 = position + offset;
            point2 = position - offset;
        }
    }

    /// <summary>Composite collision shape made up of multiple shapes.</summary>
    public class CompositeCollisionShape : ksICollisionShape
    {
        private List<ksICollisionShape> m_shapes = new List<ksICollisionShape>();

        /// <summary>Constructor</summary>
        /// <param name="shapes">List of <see cref="ksICollisionShape"/> that make up this shape.</param>
        public CompositeCollisionShape(List<ksICollisionShape> shapes)
        {
            m_shapes = shapes;
        }

        /// <summary>Description of the collider shape.</summary>
        public string Description
        {
            get { return "composite shape"; }
        }

        /// <summary>Sweeps the shape and reports collisions.</summary>
        /// <param name="position">Start position</param>
        /// <param name="rotation">Rotation</param>
        /// <param name="delta">Sweep vector</param>
        /// <returns>List of collisions.</returns>
        public RaycastHit[] Sweep(ksVector3 position, ksQuaternion rotation, ksVector3 delta)
        {
            List<RaycastHit> hits = new List<RaycastHit>();
            foreach (ksICollisionShape shape in m_shapes)
            {
                foreach (RaycastHit hit in shape.Sweep(position, rotation, delta))
                {
                    bool inserted = false;
                    for (int i = 0; i < hits.Count; i++)
                    {
                        if (hit.distance < hits[i].distance)
                        {
                            hits.Insert(i, hit);
                            inserted = true;
                            break;
                        }
                    }
                    if (!inserted)
                    {
                        hits.Add(hit);
                    }
                }
            }
            return hits.ToArray();
        }

        /// <summary>Returns an array of colliders overlapping this shape.</summary>
        /// <param name="position">Position</param>
        /// <param name="rotation">Rotation</param>
        /// <returns>Colliders overlapping this shape.</returns>
        public Collider[] Overlap(ksVector3 position, ksQuaternion rotation)
        {
            HashSet<Collider> colliderHash = new HashSet<Collider>();
            foreach (ksICollisionShape shape in m_shapes)
            {
                foreach (Collider collider in shape.Overlap(position, rotation))
                {
                    colliderHash.Add(collider);
                }
            }
            Collider[] colliders = new Collider[colliderHash.Count];
            int i = 0;
            foreach (Collider collider in colliderHash)
            {
                colliders[i] = collider;
                i++;
            }
            return colliders;
        }
    }
}
