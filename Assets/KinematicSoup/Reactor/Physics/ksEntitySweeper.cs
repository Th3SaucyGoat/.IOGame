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
using UnityEngine;

namespace KS.Reactor.Client.Unity
{
    /// <summary>Provides sweep-and-slide entity movement used for input prediction.</summary>
    public class ksEntitySweeper
    {
        // Sweep start position is moved back this amount. Sweeps can miss detecting hits with objects that start
        // nearly overlapping, so this helps prevent penetrations.
        private const float SWEEP_START_OFFSET = .1f;
        // Amount of distance to keep between the entity and collided objects. Prevents getting stuck on corners.
        private const float COLLISION_MARGIN = .01f;
        // Collider half extents are reduced by this amount. Helps prevent penetrations.
        private const float COLLIDER_DEFLATION = .02f;
        // Maximum distance to sweep when resolving penetrations--if the server entity penetrates by more than this
        // we won't resolve the penetration.
        private const float RESOLVE_PENETRATION_DEPTH = .5f;

        private ksEntityComponent m_entityComponent;
        private ksIPhysics m_physics;
        private GameObject m_gameObject;
        private ksVector3 m_lastScale;

        /// <summary>Collision shape.</summary>
        public ksICollisionShape Shape
        {
            get { return m_shape; }
        }
        private ksICollisionShape m_shape;

        /// <summary>Construct a sweeper for a specific entity.</summary>
        /// <param name="entityComponent">EntityComponent to use for sweeps.</param>
        public ksEntitySweeper(ksEntityComponent entityComponent)
        {
            m_entityComponent = entityComponent;
            m_physics = entityComponent.Entity.Room.Physics;
            m_gameObject = entityComponent.gameObject;
            m_shape = GetShape();
            m_lastScale = m_gameObject.transform.localScale;
        }

        /// <summary>Resolves penetrations resulting from moving between two points.</summary>
        /// <param name="from">Start position</param>
        /// <param name="to">End position</param>
        /// <param name="rotation">Rotation</param>
        /// <returns>
        /// Position between start and end points before penetration occurred, 
        /// or end position if no penetration occurred.
        /// </returns>
        public ksVector3 ResolvePenetration(ksVector3 from, ksVector3 to, ksQuaternion rotation)
        {
            if (m_shape == null)
            {
                return to;
            }
            if (m_gameObject.transform.localScale != m_lastScale)
            {
                m_lastScale = m_gameObject.transform.localScale;
                m_shape = GetShape();
                if (m_shape == null)
                {
                    return to;
                }
            }
            if (m_physics.AutoSync)
            {
                m_physics.SyncTransforms();
            }
            Collider[] colliders = m_shape.Overlap(to, rotation);
            List<Collider> penetratedColliders = new List<Collider>();
            foreach (Collider collider in colliders)
            {
                if (ShouldCollide(collider))
                {
                    penetratedColliders.Add(collider);
                }
            }
            if (penetratedColliders.Count == 0)
                return to;
            ksVector3 position, normal;
            ksVector3 delta = to - from;
            float distance = delta.Magnitude();
            distance = Math.Min(RESOLVE_PENETRATION_DEPTH, distance);
            from = to - delta.Normalized() * distance;
            Sweep(from, to, rotation, penetratedColliders, out position, out normal);
            return position;
        }

        /// <summary>Checks if the entity will collide with anything when moving between two points.</summary>
        /// <param name="from">Position to sweep from.</param>
        /// <param name="to">Position to sweep to.</param>
        /// <param name="rotation">Rotation</param>
        /// <param name="position">
        /// Position of the entity at the end of the sweep. If a collision occurred, this is the entity's position 
        /// when it happened.
        /// </param>
        /// <param name="normal">Normal of collision surface.</param>
        /// <returns>True if a collision occured.</returns>
        public bool Sweep(ksVector3 from, ksVector3 to, ksQuaternion rotation,
            out ksVector3 position, out ksVector3 normal)
        {
            return Sweep(from, to, rotation, null, out position, out normal);
        }

        /// <summary>Checks if the entity will collide with anything when moving between two points.</summary>
        /// <param name="from">Position to sweep from.</param>
        /// <param name="to">Position to sweep to.</param>
        /// <param name="rotation">Rotation</param>
        /// <param name="whiteList">List of colliders the entity can collide with.</param>
        /// <param name="position">
        /// Position of the entity at the end of the sweep. If a collision occurred, this is the entity's position
        /// when it happened.
        /// </param>
        /// <param name="normal">Normal of collision surface.</param>
        /// <returns>True if a collision occured.</returns>
        private bool Sweep(ksVector3 from, ksVector3 to, ksQuaternion rotation, List<Collider> whiteList,
            out ksVector3 position, out ksVector3 normal)
        {
            normal = ksVector3.Zero;
            position = to;
            if (from == to || m_shape == null)
            {
                return false;
            }
            if (m_gameObject.transform.localScale != m_lastScale)
            {
                m_lastScale = m_gameObject.transform.localScale;
                m_shape = GetShape();
                if (m_shape == null)
                {
                    return false;
                }
            }
            if (m_physics.AutoSync)
            {
                m_physics.SyncTransforms();
            }
            ksVector3 delta = to - from;
            ksVector3 direction = delta.Normalized();
            float distance = delta.Magnitude() + SWEEP_START_OFFSET;
            RaycastHit[] hits = m_shape.Sweep(from - direction * SWEEP_START_OFFSET, rotation, direction * distance);
            bool collided = false;
            foreach (RaycastHit hit in hits)
            {
                if (hit.distance < distance && hit.distance > 0 && ShouldCollide(hit.collider) &&
                    (whiteList == null || whiteList.Contains(hit.collider)))
                {
                    distance = hit.distance;
                    normal = hit.normal;
                    collided = true;
                }
            }
            if (collided)
            {
                if (distance > SWEEP_START_OFFSET + COLLISION_MARGIN + COLLIDER_DEFLATION)
                {
                    position = from + delta.Clamped(distance - SWEEP_START_OFFSET - COLLISION_MARGIN - COLLIDER_DEFLATION);
                }
                else
                {
                    position = from;
                }
            }
            return collided;
        }

        /// <summary>
        /// Checks if the entity should collide with a collider. An entity will collide with a collider
        /// if the collider belongs to another entity, and the entities' each belong to a collision
        /// group the other reacts with.
        /// </summary>
        /// <param name="collider">Collider</param>
        /// <returns>True if this entity should collide with the collider.</returns>
        private bool ShouldCollide(Collider collider)
        {
            ksEntityComponent other = collider.GetComponent<ksEntityComponent>();
            ksIUnityCollider iCollider = null;
            if (other == null)
            {
                ksEntity.Indicator indicator = collider.GetComponent<ksEntity.Indicator>();
                if (indicator != null)
                {
                    other = collider.GetComponentInParent<ksEntityComponent>();
                    iCollider = indicator.Collider;
                }
            }
            else
            {
                iCollider = new ksUnityCollider(collider);
            }

            if (other == null || other == m_entityComponent || iCollider.IsTrigger || !iCollider.IsSimulationCollider)
            {
                return false;
            }

            ksCollisionFilter filter = iCollider.CollisionFilter;
            return (m_entityComponent.GetCollisionFilter().Group & filter.Collide) != 0 &&
                    (m_entityComponent.GetCollisionFilter().Collide & filter.Group) != 0;
        }

        /// <summary>Gets a collision shape for the entity to use for client prediction.</summary>
        /// <returns>Collision shape to use for predicting collisions.</returns>
        private ksICollisionShape GetShape()
        {
            List<ksICollisionShape> shapes = new List<ksICollisionShape>();
            foreach (ksIUnityCollider collider in m_entityComponent.GetColliders())
            {
                // Do not return non simulation colliders
                ksColliderData colliderData;
                if (!m_entityComponent.TryGetColliderData(collider, out colliderData)
                    || !colliderData.IsSimulation || collider.IsTrigger)
                {
                    continue;
                }

                if (collider.ShapeType == ksShape.ShapeTypes.SPHERE)
                {
                    SphereCollider sphere = collider.Component as SphereCollider;
                    Vector3 scale = m_gameObject.transform.localScale;
                    float radiusScale = Mathf.Max(
                        m_gameObject.transform.localScale.x,
                        m_gameObject.transform.localScale.y,
                        m_gameObject.transform.localScale.z);

                    float radius = Deflate(sphere.radius * radiusScale);
                    shapes.Add(new CollisionSphere(radius, Vector3.Scale(sphere.center, scale)));
                    continue;
                }

                if (collider.ShapeType == ksShape.ShapeTypes.CAPSULE)
                {
                    CapsuleCollider capsule = collider.Component as CapsuleCollider;
                    Vector3 scale = m_gameObject.transform.localScale;
                    Vector2 alignedScale = GetAxisAlignedScale(scale, (ksShape.Axis)capsule.direction);

                    shapes.Add(CreateCapsule(
                        Deflate(capsule.radius * alignedScale.x),
                        Deflate(capsule.height * alignedScale.y, 2f * COLLIDER_DEFLATION),
                        Vector3.Scale(capsule.center, scale),
                        ksShapeUtils.AxisToRotationOffset((ksShape.Axis)capsule.direction)));
                    continue;
                }

                if (collider.ShapeType == ksShape.ShapeTypes.BOX)
                {
                    BoxCollider box = collider.Component as BoxCollider;
                    ksVector3 halfExtents = ksVector3.Scale(box.size, m_gameObject.transform.localScale) * .5f;
                    halfExtents = Deflate(halfExtents);
                    ksVector3 offset = ksVector3.Scale(box.center, m_gameObject.transform.localScale);
                    shapes.Add(new CollisionBox(halfExtents, offset));
                    continue;
                }

                if (collider.ShapeType == ksShape.ShapeTypes.CONVEX_MESH || collider.ShapeType == ksShape.ShapeTypes.TRIANGLE_MESH)
                {
                    MeshCollider mesh = collider.Component as MeshCollider;
                    ksICollisionShape shape = ksShapeUtils.ApproximateShape(Deflate(mesh.bounds.extents));
                    LogShapeApproximation(shape);
                    shapes.Add(shape);
                    continue;
                }

                if (collider.ShapeType == ksShape.ShapeTypes.CYLINDER)
                {
                    ksCylinderCollider cylinder = (ksCylinderCollider)collider;
                    Vector3 scale = m_gameObject.transform.localScale;
                    Vector2 alignedScale = GetAxisAlignedScale(scale, cylinder.Direction);
                    ksICollisionShape shape = CreateCapsule(
                        Deflate(cylinder.Radius * alignedScale.x),
                        Deflate(cylinder.Height * alignedScale.y, 2f * COLLIDER_DEFLATION),
                        Vector3.Scale(cylinder.Center, scale),
                        ksShapeUtils.AxisToRotationOffset(cylinder.Direction)
                    );
                    LogShapeApproximation(shape);
                    shapes.Add(shape);
                    continue;
                }

                if (collider.ShapeType == ksShape.ShapeTypes.CONE)
                {
                    ksConeCollider cone = (ksConeCollider)collider;
                    Vector3 scale = m_gameObject.transform.localScale;
                    Vector2 alignedScale = GetAxisAlignedScale(scale, cone.Direction);
                    ksICollisionShape shape = CreateCapsule(
                        Deflate(cone.Radius * alignedScale.x),
                        Deflate(cone.Height * alignedScale.y, 2f * COLLIDER_DEFLATION),
                        Vector3.Scale(cone.Center, scale),
                        ksShapeUtils.AxisToRotationOffset(cone.Direction)
                    );
                    LogShapeApproximation(shape);
                    shapes.Add(shape);
                    continue;
                }
            }

            if (shapes.Count == 0)
            {
                return null;
            }
            if (shapes.Count == 1)
            {
                return shapes[0];
            }
            return new CompositeCollisionShape(shapes);
        }

        /// <summary>Deflates a collider half extent.</summary>
        /// <param name="size">Size of the half extent.</param>
        /// <param name="amount">Amount to deflate.</param>
        /// <returns>
        /// Maximum of half the <paramref name="amount"/> or <paramref name="size"/> minus <paramref name="amount"/>.
        /// </returns>
        private float Deflate(float size, float amount = COLLIDER_DEFLATION)
        {
            return Math.Max(size - amount, amount / 2f);
        }

        /// <summary>Deflates collider half extents.</summary>
        /// <param name="halfExtents">Half extents to deflate.</param>
        /// <returns>Deflated half extents.</returns>
        private ksVector3 Deflate(ksVector3 halfExtents)
        {
            halfExtents.X = Deflate(halfExtents.X);
            halfExtents.Y = Deflate(halfExtents.Y);
            halfExtents.Z = Deflate(halfExtents.Z);
            return halfExtents;
        }

        /// <summary>
        /// Logs a warning that we're using another shape to approximate a collider because Unity does not support
        /// sweeping the collider type on the entity.
        /// </summary>
        /// <param name="shape">Shape used to approximate a collider.</param>
        private void LogShapeApproximation(ksICollisionShape shape)
        {
            ksLog.Warning(this, "Client collision prediction in Unity is only supported with spheres, capsules and boxes. " +
                "Using a " + shape.Description + " to approximate collisions.");
        }

        /// <summary>
        /// Creates a capsule. Will create a sphere instead if the height is less than or equal to the diameter.
        /// </summary>
        /// <param name="radius">Radius</param>
        /// <param name="height">Height</param>
        /// <param name="positionOffset">Position offset</param>
        /// <param name="rotationOffset">Rotation offset</param>
        /// <returns>Capsule collision shape.</returns>
        private ksICollisionShape CreateCapsule(
            float radius,
            float height,
            ksVector3 positionOffset,
            ksQuaternion rotationOffset)
        {
            if (height <= radius * 2)
            {
                return new CollisionSphere(radius, positionOffset);
            }
            return new CollisionCapsule(radius, height, positionOffset, rotationOffset);
        }

        /// <summary>
        /// Get the radius and axis scaling for a collider that uses a rotation axis alignment (capsule, cylinder, cone).
        /// </summary>
        /// <param name="scale">Game object scale</param>
        /// <param name="alignment">Axis of rotation.</param>
        /// <returns>A vector where x = radius scale, y = axis scale.</returns>
        private Vector2 GetAxisAlignedScale(Vector3 scale, ksShape.Axis alignment)
        {
            switch (alignment)
            {
                case ksShape.Axis.X:
                    return new Vector2(Mathf.Max(scale.y, scale.z), scale.x);
                case ksShape.Axis.Z:
                    return new Vector2(Mathf.Max(scale.x, scale.y), scale.z);
                case ksShape.Axis.Y:
                default:
                    return new Vector2(Mathf.Max(scale.x, scale.z), scale.y);
            }
        }
    }
}