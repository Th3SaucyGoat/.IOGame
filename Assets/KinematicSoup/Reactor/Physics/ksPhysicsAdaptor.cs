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
    /// <summary>
    /// <see cref="ksIPhysics"/> implementation that uses Unity physics. Sweeps and overlaps are only 
    /// supported with spheres and capsules because that's all that Unity gives us.
    /// </summary>
    public class ksPhysicsAdaptor : ksIPhysics
    {
        private bool m_autoSync = true;

        /// <summary>
        /// If true, transform changes are synced with the physics system before executing physics queries.
        /// </summary>
        public bool AutoSync
        {
            get { return m_autoSync; }
            set { m_autoSync = value; }
        }

        /// <summary>
        /// Apply entity transform updates to the physics simulation now rather than 
        /// waiting for the next simulation step.
        /// </summary>
        public void SyncAll()
        {
            Physics.SyncTransforms();
        }

        /// <summary>
        /// Apply entity transform updates to the physics simulation now rather than 
        /// waiting for the next simulation step.
        /// </summary>
        public void SyncTransforms()
        {
            Physics.SyncTransforms();
        }


        /// <summary>Gravity</summary>
        public ksVector3 Gravity
        {
            get { return m_gravity; }
            set { m_gravity = value; }
        }
        private ksVector3 m_gravity;

        /// <summary>Perform a raycast and return a list of all raycast hits.</summary>
        /// <param name="origin">Point of origin.</param>
        /// <param name="direction">Ray direction.</param>
        /// <param name="distance">Ray distance.</param>
        /// <param name="includeOverlaps">Include entities that overlap the origin.</param>
        /// <param name="flags">Bit flags for filtering static, dynamic, and first hit.</param>
        /// <param name="groupMask">Bit mask of collsion groups.</param>
        /// <param name="maxResults">Maximum number of results to return.</param>
        /// <returns>List of raycast results.</returns>
        public List<ksRaycastResult> Raycast(
            ksVector3 origin,
            ksVector3 direction,
            float distance,
            bool includeOverlaps = false,
            ksPhysicsQueryFlags flags = ksPhysicsQueryFlags.DEFAULT,
            uint groupMask = 0xffffffff,
            uint maxResults = 255)
        {
            List<ksRaycastResult> results = new List<ksRaycastResult>((int)maxResults);
            if (maxResults == 0)
            {
                return results;
            }
            if (m_autoSync)
            {
                SyncTransforms();
            }

            // Cap max results
            if (maxResults > ksPhysicsConsts.MAX_RESULTS)
            {
                maxResults = ksPhysicsConsts.MAX_RESULTS;
                ksLog.Warning(this, "Scene queries are limit to " + ksPhysicsConsts.MAX_RESULTS + " results");
            }

            if (includeOverlaps)
            {
                // Unity raycasts ignore starting overlaps and ours don't. To make the behaviour consistent, we do a point
                // overlap test using a sphere overlap with radius 0.
                Collider[] overlaps = Physics.OverlapSphere(origin, 0f);
                foreach (Collider unityCollider in overlaps)
                {
                    ksEntity entity;
                    ksIUnityCollider collider;
                    if (GetCollidingEntityAndCollider(unityCollider, groupMask, flags, null, out entity, out collider))
                    {
                        results.Add(new ksRaycastResult()
                        {
                            Entity = entity,
                            Collider = collider,
                            Point = origin,
                            Distance = -1f
                        }); ;

                        // If we were looking for any hit then return
                        if ((flags & ksPhysicsQueryFlags.ANY_HIT) > 0)
                        {
                            return results;
                        }

                        // Do not exceed the requested number of results
                        if (results.Count >= maxResults)
                        {
                            return results;
                        }
                    }
                }
            }

            RaycastHit[] hits = Physics.RaycastAll(origin, direction, distance);
            foreach (RaycastHit hit in hits)
            {
                ksEntity entity;
                ksIUnityCollider collider;
                if (GetCollidingEntityAndCollider(hit.collider, groupMask, flags, null, out entity, out collider))
                {
                    results.Add(new ksRaycastResult()
                    {
                        Entity = entity,
                        Collider = collider,
                        Point = hit.point,
                        Normal = hit.normal,
                        Distance = hit.distance
                    });

                    // If we were looking for any hit then return
                    if ((flags & ksPhysicsQueryFlags.ANY_HIT) > 0)
                    {
                        break;
                    }

                    // Do not exceed the requested number of results
                    if (results.Count >= maxResults)
                    {
                        break;
                    }
                }
            }
            return results;
        }

        /// <summary>Perform a shape sweep and return a list of sweep collisions.</summary>
        /// <param name="shape">Primitive or convex shape.</param>
        /// <param name="origin">Origin point.</param>
        /// <param name="rotation">Rotation of shape.</param>
        /// <param name="direction">Sweep direction.</param>
        /// <param name="distance">Sweep distance.</param>
        /// <param name="includeOverlaps">Include entities that overlap at the origin.</param>
        /// <param name="flags">Bit flags for filtering static, dynamic, and first hit.</param>
        /// <param name="groupMask">Bit mask of collsion groups.</param>
        /// <param name="maxResults">Maximum number of results to return.</param>
        /// <returns>List of sweep results.</returns>
        public List<ksSweepResult> Sweep(
            ksShape shape,
            ksVector3 origin,
            ksQuaternion rotation,
            ksVector3 direction,
            float distance,
            bool includeOverlaps = false,
            ksPhysicsQueryFlags flags = ksPhysicsQueryFlags.DEFAULT,
            uint groupMask = 0xffffffff,
            uint maxResults = 255)
        {
            if (maxResults == 0)
            {
                return new List<ksSweepResult>();
            }
            ksICollisionShape collisionShape = GetCollisionShape(shape);
            if (collisionShape == null)
            {
                return new List<ksSweepResult>();
            }
            return Sweep(
                collisionShape,
                origin,
                rotation,
                direction,
                distance,
                null,
                includeOverlaps,
                flags,
                groupMask,
                maxResults);
        }

        /// <summary>Entity sweep using the current position and rotation as starting points.</summary>
        /// <param name="entity">Entity being swept.</param>
        /// <param name="direction">Sweep direction.</param>
        /// <param name="distance">Sweep distance.</param>
        /// <param name="includeOverlaps">Include entities that overlap at the origin.</param>
        /// <param name="flags">Bit flags for filtering static, dynamic, and first hit.</param>
        /// <param name="groupMask">Bit mask of collsion groups.</param>
        /// <param name="maxResults">Maximum number of results to return.</param>
        /// <returns>List of sweep results.</returns>
        public List<ksSweepResult> Sweep(
            ksIEntity entity,
            ksVector3 direction,
            float distance,
            bool includeOverlaps = false,
            ksPhysicsQueryFlags flags = ksPhysicsQueryFlags.DEFAULT,
            uint groupMask = 0xffffffff,
            uint maxResults = 255)
        {
            return Sweep(
                entity,
                entity.Position,
                entity.Rotation,
                direction,
                distance,
                true,
                includeOverlaps,
                flags,
                groupMask,
                maxResults);
        }

        /// <summary>
        /// Entity sweep using the current position and rotation as starting points and an option to include or
        /// exclude the swept entity from the results.
        /// </summary>
        /// <param name="entity">Entity being swept.</param>
        /// <param name="direction">Sweep direction.</param>
        /// <param name="distance">Sweep distance.</param>
        /// <param name="excludeSelf">Exclude self.</param>
        /// <param name="includeOverlaps">Include entities that overlap at the origin.</param>
        /// <param name="flags">Bit flags for filtering static, dynamic, and first hit.</param>
        /// <param name="groupMask">Bit mask of collsion groups.</param>
        /// <param name="maxResults">Maximum number of results to return.</param>
        /// <returns>List of sweep results.</returns>
        public List<ksSweepResult> Sweep(
            ksIEntity entity,
            ksVector3 origin,
            ksQuaternion rotation,
            ksVector3 direction,
            float distance,
            bool excludeSelf = true,
            bool includeOverlaps = false,
            ksPhysicsQueryFlags flags = ksPhysicsQueryFlags.DEFAULT,
            uint groupMask = 0xffffffff,
            uint maxResults = 255)
        {
            if (maxResults == 0)
            {
                return new List<ksSweepResult>();
            }
            ksEntity clientEntity = entity as ksEntity;
            if (clientEntity == null || clientEntity.Shape == null)
            {
                return new List<ksSweepResult>();
            }
            ksEntity excludeEntity = excludeSelf ? clientEntity : null;
            return Sweep(
                clientEntity.Shape,
                origin,
                rotation,
                direction,
                distance,
                excludeEntity,
                includeOverlaps,
                flags,
                groupMask,
                maxResults);
        }

        /// <summary>Sweeps a collision shape and allows a specific entity to be excluded from the results.</summary>
        /// <param name="shape">Shape to sweep.</param>
        /// <param name="origin">Origin point.</param>
        /// <param name="rotation">Rotation of shape.</param>
        /// <param name="direction">Sweep direction.</param>
        /// <param name="distance">Sweep distance.</param>
        /// <param name="excludeEntity">Entity to exclude from results.</param>
        /// <param name="includeOverlaps">Include entities that overlap at the origin.</param>
        /// <param name="flags">Bit flags for filtering static, dynamic, and first hit.</param>
        /// <param name="groupMask">Bit mask of collsion groups.</param>
        /// <param name="maxResults">Maximum number of results to return.</param>
        /// <returns>List of sweep results.</returns>
        private List<ksSweepResult> Sweep(
            ksICollisionShape shape,
            ksVector3 origin,
            ksQuaternion rotation,
            ksVector3 direction,
            float distance,
            ksEntity excludeEntity,
            bool includeOverlaps = false,
            ksPhysicsQueryFlags flags = ksPhysicsQueryFlags.DEFAULT,
            uint groupMask = 0xffffffff,
            uint maxResults = 255)
        {
            if (m_autoSync)
            {
                SyncTransforms();
            }
            RaycastHit[] hits = shape.Sweep(origin, rotation, direction.Normalized() * distance);
            List<ksSweepResult> results = new List<ksSweepResult>();
            foreach (RaycastHit hit in hits)
            {
                ksEntity entity;
                ksIUnityCollider collider;
                if (GetCollidingEntityAndCollider(hit.collider, groupMask, flags, excludeEntity, out entity, out collider))
                {
                    if (includeOverlaps || hit.distance > 0f)
                    {
                        ksSweepResult result = new ksSweepResult();
                        result.Entity = entity;
                        result.Collider = collider;
                        result.Point = hit.point;
                        result.Normal = hit.normal;
                        result.Distance = hit.distance;

                        results.Add(result);
                        if (results.Count >= maxResults)
                        {
                            break;
                        }

                        // If we were looking for any hit then return
                        if ((flags & ksPhysicsQueryFlags.ANY_HIT) > 0)
                        {
                            break;
                        }
                    }
                }
            }
            return results;
        }

        /// <summary>Tests for entities overlapping a shape.</summary>
        /// <param name="shape">Primitive or convex shape.</param>
        /// <param name="origin">Origin point.</param>
        /// <param name="rotation">Rotation of shape.</param>
        /// <param name="flags">Bit flags for filtering static, dynamic, and first hit.</param>
        /// <param name="groupMask">Bit mask of collsion groups.</param>
        /// <param name="maxResults">Maximum number of results to return.</param>
        /// <returns>List of overlap results.</returns>
        public List<ksOverlapResult> Overlap(
            ksShape shape,
            ksVector3 origin,
            ksQuaternion rotation,
            ksPhysicsQueryFlags flags = ksPhysicsQueryFlags.DEFAULT,
            uint groupMask = 0xffffffff,
            uint maxResults = 255)
        {
            if (maxResults == 0)
            {
                return new List<ksOverlapResult>();
            }
            ksICollisionShape collisionShape = GetCollisionShape(shape);
            if (collisionShape == null)
            {
                return new List<ksOverlapResult>();
            }
            return Overlap(
                collisionShape,
                origin,
                rotation,
                null,
                flags,
                groupMask,
                maxResults);
        }

        /// <summary>Tests for entities overlapping an entity.</summary>
        /// <param name="entity">Entity</param>
        /// <param name="origin">Origin point.</param>
        /// <param name="rotation">Rotation of entity.</param>
        /// <param name="excludeSelf">Exclude self from result.</param>
        /// <param name="flags">Bit flags for filtering static, dynamic, and first hit.</param>
        /// <param name="groupMask">Bit mask of collsion groups.</param>
        /// <param name="maxResults">Maximum number of results to return.</param>
        /// <returns>List of overlap results.</returns>
        public List<ksOverlapResult> Overlap(
            ksIEntity entity,
            ksVector3 origin,
            ksQuaternion rotation,
            bool excludeSelf = true,
            ksPhysicsQueryFlags flags = ksPhysicsQueryFlags.DEFAULT,
            uint groupMask = 0xffffffff,
            uint maxResults = 255)
        {
            if (maxResults == 0)
            {
                return new List<ksOverlapResult>();
            }
            ksEntity clientEntity = entity as ksEntity;
            if (clientEntity == null || clientEntity.Shape == null)
            {
                return new List<ksOverlapResult>();
            }
            ksEntity excludeEntity = excludeSelf ? clientEntity : null;
            return Overlap(
                clientEntity.Shape,
                origin,
                rotation,
                excludeEntity,
                flags,
                groupMask,
                maxResults);
        }

        /// <summary>
        /// Tests for entities overlapping a shape and allows a specific entity to be excluded from the results.
        /// </summary>
        /// <param name="shape">Shape</param>
        /// <param name="origin">Origin point.</param>
        /// <param name="rotation">Rotation of shape.</param>
        /// <param name="excludeEntity">Entity to exclude from results.</param>
        /// <param name="flags">Bit flags for filtering static, dynamic, and first hit.</param>
        /// <param name="groupMask">Bit mask of collsion groups.</param>
        /// <param name="maxResults">Maximum number of results to return.</param>
        /// <returns>List of overlap results.</returns>
        private List<ksOverlapResult> Overlap(
            ksICollisionShape shape,
            ksVector3 origin,
            ksQuaternion rotation,
            ksEntity excludeEntity,
            ksPhysicsQueryFlags flags = ksPhysicsQueryFlags.DEFAULT,
            uint groupMask = 0xffffffff,
            uint maxResults = 255)
        {
            if (m_autoSync)
            {
                SyncTransforms();
            }
            Collider[] colliders = shape.Overlap(origin, rotation);
            List<ksOverlapResult> results = new List<ksOverlapResult>();
            foreach (Collider unityCollider in colliders)
            {
                ksEntity entity;
                ksIUnityCollider collider;
                if (GetCollidingEntityAndCollider(unityCollider, groupMask, flags, excludeEntity, out entity, out collider))
                {
                    ksOverlapResult result = new ksOverlapResult();
                    result.Entity = entity;
                    result.Collider = collider;
                    results.Add(result);

                    if (results.Count >= maxResults)
                    {
                        break;
                    }

                    // If we were looking for any hit then return
                    if ((flags & ksPhysicsQueryFlags.ANY_HIT) > 0)
                    {
                        break;
                    }
                }
            }
            return results;
        }

        /// <summary>
        /// Compute the minimal translation required to separate the given colliders apart at specified poses.
        /// </summary>
        /// <param name="collider0"></param>
        /// <param name="position0"></param>
        /// <param name="rotation0"></param>
        /// <param name="collider1"></param>
        /// <param name="position1"></param>
        /// <param name="rotation1"></param>
        /// <param name="direction">Direction along which the translation required to separate the colliders apart is minimal.</param>
        /// <param name="distance">The distance along direction that is required to separate the colliders apart.</param>
        /// <returns>True if the colliders were overlapping.</returns>
        public bool ComputePenetration(
            ksICollider collider0, ksVector3 position0, ksQuaternion rotation0,
            ksICollider collider1, ksVector3 position1, ksQuaternion rotation1,
            out ksVector3 direction, out float distance)
        {
            direction = ksVector3.Zero;
            distance = 0;
            if (collider0 == null || collider1 == null)
            {
                return false;
            }

            position0 = ApplyReactorColliderScale(collider0, position0, rotation0);
            position1 = ApplyReactorColliderScale(collider1, position1, rotation1);

            Vector3 unityDir;
            bool result = Physics.ComputePenetration(
                (collider0 as ksIUnityCollider).Collider, position0, rotation0,
                (collider1 as ksIUnityCollider).Collider, position1, rotation1,
                out unityDir, out distance);
            direction = unityDir;
            return result;
        }

        /// <summary>
        /// Get the closest point on a collider to another point.
        /// Currently supported colliders: box, sphere, capsule, convexmesh.
        /// </summary>
        /// <param name="point">Point to measure from.</param>
        /// <param name="collider">Collider to get the closest point from.</param>
        /// <returns>Closest point on the collider.</returns>
        public ksVector3 GetClosestPoint(ksVector3 point, ksICollider collider)
        {
            if (collider == null)
            {
                return point;
            }
            return GetClosestPoint(point, collider, collider.Entity.Position, collider.Entity.Rotation);
        }

        /// <summary>
        /// Get the closest point on a collider to another point.
        /// Currently supported colliders: box, sphere, capsule, convexmesh.
        /// </summary>
        /// <param name="point">Point to measure from.</param>
        /// <param name="collider">Collider to get the closest point from.</param>
        /// <param name="position">Collider position</param>
        /// <param name="rotation">Collider rotation</param>
        /// <returns>Closest point on the collider.</returns>
        public ksVector3 GetClosestPoint(ksVector3 point, ksICollider collider, ksVector3 position, ksQuaternion rotation)
        {
            if (collider == null)
            {
                return point;
            }
            position = ApplyReactorColliderScale(collider, position, rotation);
            return Physics.ClosestPoint(point, (collider as ksIUnityCollider).Collider, position, rotation);
        }

        /// <summary>
        /// Apply scale and offset from a Reactor cone and cylinder colliders to a collider position.
        /// </summary>
        /// <param name="collider">Reactor cone or cylinder collider</param>
        /// <param name="position">Postion to adjust</param>
        /// <param name="rotation">Rotation to apply to position adjustment</param>
        /// <returns>Position adjusted by the collider offset.</returns>
        private ksVector3 ApplyReactorColliderScale(ksICollider collider, ksVector3 position, ksQuaternion rotation)
        {
            if (collider is ksCylinderCollider)
            {
                return position + rotation * ksVector3.Scale((collider as ksCylinderCollider).Center, collider.Entity.Scale);
            }
            else if (collider is ksConeCollider)
            {
                return position + rotation * ksVector3.Scale((collider as ksConeCollider).Center, collider.Entity.Scale);
            }
            return position;
        }

        /// <summary>Gets an <see cref="ksICollisionShape"/> from a <see cref="ksShape"/>.</summary>
        /// <param name="shape">Shape</param>
        /// <returns>Collision shape</returns>
        private ksICollisionShape GetCollisionShape(ksShape shape)
        {
            ksICollisionShape collisionShape = null;
            switch (((ksShape)shape).Type)
            {
                case ksShape.ShapeTypes.BOX:
                    ksBox box = shape as ksBox;
                    collisionShape = ksShapeUtils.ApproximateShape(box.HalfExtents);
                    LogShapeApproximation(collisionShape);
                    break;
                case ksShape.ShapeTypes.SPHERE:
                    ksSphere sphere = shape as ksSphere;
                    collisionShape = new CollisionSphere(sphere.Radius, ksVector3.Zero);
                    break;
                case ksShape.ShapeTypes.CAPSULE:
                    ksCapsule capsule = shape as ksCapsule;
                    collisionShape = new CollisionCapsule(capsule.Radius, capsule.Height,
                        ksVector3.Zero, ksShapeUtils.AxisToRotationOffset(capsule.Alignment));
                    break;
            }
            return collisionShape;
        }

        /// <summary>
        /// Gets the <see cref="ksEntity"/> for a collider if the collider's game object has a 
        /// <see cref="ksEntityComponent"/> and the collider mask allows collisions.
        /// </summary>
        /// <param name="collider">Collider attached to the entity we are looking for.</param>
        /// <param name="groupMask">If zero, collides with everything. If non-zero, the entity must 
        /// belong to at least one group in the mask.</param>
        /// <param name="filter">Flags used for filtering results.</param>
        /// <param name="excludeEntity">Do not collide with this entity.</param>
        /// <param name="entity">Is assigned the entity found on the collider.</param>
        /// <param name="unityCollider">Is assigned the <see cref="ksIUnityCollider"/> found on the entity.</param>
        /// <returns>True if an entity and collider was found.</returns>
        private bool GetCollidingEntityAndCollider(
            Collider unityCollider,
            uint groupMask,
            ksPhysicsQueryFlags filter,
            ksEntity excludeEntity,
            out ksEntity entity,
            out ksIUnityCollider collider)
        {
            entity = null;
            collider = null;

            // Validate collider and game object
            if (unityCollider == null || unityCollider.gameObject == null)
            {
                return false;
            }

            ksEntityComponent entityComponent = null;
            ksIUnityCollider iCollider = null;
            TryGetKSCollider(unityCollider, out iCollider, out entityComponent);

            if (entityComponent == null || entityComponent.Entity == excludeEntity)
            {
                return false;
            }

            // Ignore static objects if the static filter flag is not set.
            if (entityComponent.gameObject.isStatic && (filter & ksPhysicsQueryFlags.STATIC) == 0)
            {
                return false;
            }

            // Ignore dynamic objects if the dynamic filter flag is not set.
            if (!entityComponent.gameObject.isStatic && (filter & ksPhysicsQueryFlags.DYNAMIC) == 0)
            {
                return false;
            }

            ksCollisionFilter collisionFilter = iCollider.CollisionFilter;
            if (iCollider.IsQueryCollider && ((collisionFilter.Group & groupMask) != 0 || groupMask == 0))
            {
                entity = entityComponent.Entity;
                collider = iCollider;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gets the <see cref="ksIUnityCollider"/> and <see cref="ksEntityComponent"/> for a collider.
        /// </summary>
        /// <param name="collider">Collider attached to the entity we are looking for.</param>
        /// <param name="unityCollider">Is assigned the <see cref="ksIUnityCollider"/> found on the entity.</param>
        /// <param name="entityComponent">Is assigned the <see cref="ksEntityComponent"/> found on the entity.</param>
        public static void TryGetKSCollider(
            Collider unityCollider,
            out ksIUnityCollider ksCollider,
            out ksEntityComponent entityComponent)
        {
            ksCollider = null;
            entityComponent = unityCollider.GetComponent<ksEntityComponent>();
            if (entityComponent == null)
            {
                ksEntity.Indicator indicator = unityCollider.GetComponent<ksEntity.Indicator>();
                if (indicator != null)
                {
                    entityComponent = unityCollider.GetComponentInParent<ksEntityComponent>();
                    ksCollider = indicator.Collider;
                }
            }
            else
            {
                ksCollider = new ksUnityCollider(unityCollider);
            }
        }

        /// <summary>
        /// Logs a warning that we're using a shape to approximate another shape because Unity does not support
        /// sweeping the other shape.
        /// </summary>
        /// <param name="shape">Shape we're using to approximate another.</param>
        private void LogShapeApproximation(ksICollisionShape shape)
        {
            ksLog.Warning(this, "Scene queries in Unity are only supported with spheres and capsules. " +
                "Using a " + shape.Description + " to approximate collisions.");
        }
    }
}