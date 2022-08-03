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

namespace KS.Reactor.Client.Unity
{
    /// <summary>Collision shape utils.</summary>
    public class ksShapeUtils
    {
        /// <summary>
        /// Converts enum <see cref="ksShape.Axis"/> to a ksQuaternion that would rotate a y-axis aligned shape
        /// to the given axis.
        /// </summary>
        /// <param name="axis">Axis alignment</param>
        /// <returns>Quaternion representing axis rotated from the identity rotation.</returns>
        public static ksQuaternion AxisToRotationOffset(ksShape.Axis axis)
        {
            switch (axis)
            {
                case ksShape.Axis.X:
                    return ksQuaternion.FromVectorDelta(ksVector3.Up, ksVector3.Right);
                case ksShape.Axis.Y:
                default:
                    return ksQuaternion.Identity;
                case ksShape.Axis.Z:
                    return ksQuaternion.FromVectorDelta(ksVector3.Up, ksVector3.Forward);
            }
        }

        /// <summary>
        /// Creates a capsule or sphere to approximate box extents. A capsule will be created
        /// if the largest extent is at least twice as long as the next largest.
        /// </summary>
        /// <param name="extents">Extents to create an approximate shape for.</param>
        /// <returns>Approximate shape.</returns>
        public static ksICollisionShape ApproximateShape(ksVector3 extents)
        {
            float radius;
            if (extents.X >= 2 * Math.Max(extents.Y, extents.Z))
            {
                radius = (extents.Y + extents.Z) / 2;
                return new CollisionCapsule(
                    radius,
                    extents.X * 2,
                    ksVector3.Zero,
                    AxisToRotationOffset(ksShape.Axis.X));
            }
            if (extents.Y >= 2 * Math.Max(extents.X, extents.Z))
            {
                radius = (extents.X + extents.Z) / 2;
                return new CollisionCapsule(
                    radius,
                    extents.Y * 2,
                    ksVector3.Zero,
                    AxisToRotationOffset(ksShape.Axis.Y));
            }
            if (extents.Z >= 2 * Math.Max(extents.X, extents.Y))
            {
                radius = (extents.X + extents.Y) / 2;
                return new CollisionCapsule(
                    radius,
                    extents.Z * 2,
                    ksVector3.Zero,
                    AxisToRotationOffset(ksShape.Axis.Z));
            }
            radius = (extents.X + extents.Y + extents.Z) / 3;
            return new CollisionSphere(radius, ksVector3.Zero);
        }
    }
}
