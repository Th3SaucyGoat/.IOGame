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
using System.Linq;
using System.Text;
using UnityEngine;

namespace KS.Reactor.Client.Unity
{
    /// <summary>Base class for classes that wrap a Unity <see cref="Rigidbody"/> for use with Reactor.</summary>
    public class ksBaseUnityRigidBody
    {
        /// <summary>
        /// Determines if rigid bodies are 3D or 2D by default when spawning prefabs or loading the scene.
        /// </summary>
        public static ksRigidBodyTypes DefaultType
        {
            get { return m_defaultType; }
            set { m_defaultType = value; }
        }
        private static ksRigidBodyTypes m_defaultType = ksRigidBodyTypes.RIGID_BODY_3D;

        protected Rigidbody m_rigidBody;
        private bool m_isKinematic;

        /// <summary>Constructor</summary>
        /// <param name="rigidBody">Unity rigid body.</param>
        public ksBaseUnityRigidBody(Rigidbody rigidBody)
        {
            if (rigidBody == null)
            {
                throw new ArgumentNullException("rigidBody");
            }
            m_rigidBody = rigidBody;
            m_isKinematic = rigidBody.isKinematic;
            // Set Unity's rigidbody to kinematic so Unity physics doesn't try move the object.
            rigidBody.isKinematic = true;
        }

        /// <summary>Mass of the rigid body.</summary>
        public float Mass
        {
            get { return m_rigidBody.mass; }
            set { m_rigidBody.mass = value; }
        }

        /// <summary>Damping.</summary>
        public float Drag
        {
            get { return m_rigidBody.drag; }
            set { m_rigidBody.drag = value; }
        }

        /// <summary>Angular damping.</summary>
        public float AngularDrag
        {
            get { return m_rigidBody.angularDrag; }
            set { m_rigidBody.angularDrag = value; }
        }

        /// <summary>If true, the entity will be affected by the scene gravity.</summary>
        public bool UseGravity
        {
            get { return m_rigidBody.useGravity; }
            set { m_rigidBody.useGravity = value; }
        }

        /// <summary>
        /// If true, the entity will not be effected by gravity or other impulses, but may be moved around by setting
        /// translation and rotation from scripts. This value is different from <see cref="Rigidbody.isKinematic"/>,
        /// which is set to false to prevent Unity physics from affecting the rigid body.
        /// </summary>
        public bool IsKinematic
        {
            get { return m_isKinematic; }
            set { m_isKinematic = value; }
        }

        /// <summary>The rigidbody's center of mass in local space.</summary>
        public ksVector3 CenterOfMass
        {
            get { return ksVector3.Scale(m_rigidBody.centerOfMass, InvScale); }
            set { m_rigidBody.centerOfMass = ksVector3.Scale(value, m_rigidBody.transform.localScale); }
        }

        /// <summary>
        /// The inverse scale of the transform. If a component of the scale is zero, it will also be zero in the
        /// inverse scale.
        /// </summary>
        internal ksVector3 InvScale
        {
            get
            {
                ksVector3 s = m_rigidBody.transform.localScale;
                return new ksVector3(s.X == 0f ? 0f : 1f / s.X, s.Y == 0f ? 0f : 1f / s.Y, s.Z == 0f ? 0f : 1f / s.Z);
            }
        }
    }
}
