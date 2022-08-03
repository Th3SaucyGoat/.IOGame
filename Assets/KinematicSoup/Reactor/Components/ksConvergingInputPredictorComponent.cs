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
using System.Linq;
using System.Text;
using UnityEngine;

namespace KS.Reactor.Client.Unity
{
    /// <summary>
    /// Wraps a <see cref="ksConvergingInputPredictor"/>, allowing it to be attached to a game object for an entity or
    /// room type and have its parameters edited in the inspector.
    /// </summary>
    public class ksConvergingInputPredictorComponent : ksPredictorWrapper<ksConvergingInputPredictor>, ksICloneableScript
    {
        /// <summary>Does the predictor require a player controller?</summary>
        public override bool RequiresController
        {
            get { return true; }
        }

        /// <summary>Describes how to predict a property.</summary>
        [Serializable]
        public struct PropertyBehaviour
        {
            [Tooltip("Property id")]
            /// <summary>Property id.</summary>
            public uint Property;

            [Tooltip("The prediction behaviour of the property.")]
            /// <summary>Prediction behaviour.</summary>
            public ksPredictionBehaviour Type;

            // Properties after the first two are not drawn in the inspector if UseDefaultConfig is true.

            [Tooltip("If the difference between predicted and server property change rates exceeds the tolerance, inputs " +
            "will be replayed using the new server property value and change rate to get a more accurate prediction.")]
            /// <summary>
            /// If the difference between predicted and server property change rates exceeds this, inputs will be replayed using
            /// the new server property value and change rate to get a more accurate prediction. <= 0 for no limit.
            /// </summary>
            public float Tolerance;
        }

        [Tooltip(
            "Should this predictor use the default config defined in ksConvergingInputPredictor.ConfigData.Default?")]
        /// <summary>Should the predictor use <see cref="ksConvergingInputPredictor.ConfigData.Default"/>?</summary>
        public bool UseDefaultConfig = true;

        [Tooltip("Lower values make the predictor try to converge more aggressively")]
        /// <summary>Lower values mean we try to converge more aggressively.</summary>
        public float ConvergeMultiplier;

        [Tooltip("Maximum time step for the client over a single frame. Limiting the maximum time step can improve " +
            "behaviour when there are dips in frame rate.")]
        /// <summary>
        /// Maximum time step for the client over a single frame. If a frame's time delta is larger than this, this
        /// is used as the time delta instead. The predictor behaves poorly with large time steps, and limiting the
        /// maximum time step can improve the behaviour when there are dips in frame rate.
        /// </summary>
        public float MaxDeltaTime;

        [Tooltip("Latency in seconds to use in calculations before the real latency is determined.")]
        /// <summary>Latency in seconds to use before latency is determined.</summary>
        public float DefaultLatency;

        [Tooltip("If true, converging prediction is not run on position and the player controller alone determines " +
            "the client position.")]
        /// <summary>
        /// If true, converging prediction is not run on the position. Instead the player controller alone
        /// determines the client position.
        /// </summary>
        public bool UseClientOnlyPosition;

        [Tooltip("If true, converging prediction is not run on rotation and the player controller alone determines " +
            "the client rotation.")]
        /// <summary>
        /// If true, converging prediction is not run on the rotation. Instead the player controller alone
        /// determines the client rotation.
        /// </summary>
        public bool UseClientOnlyRotation;

        [Tooltip("If true, converging prediction is not run on scale and the player controller alone determines " +
            "the client scale.")]
        /// <summary>
        /// If true, converging prediction is not run on the scale. Instead the player controller alone determines
        /// the client scale.
        /// </summary>
        public bool UseClientOnlyScale;

        [Tooltip("Maximum difference between server and predicted velocity before inputs are replayed. Less than or " +
            "equal to zero for no limit.")]
        /// <summary>
        /// Maximum difference between server and predicted velocity before inputs are replayed. Less than or equal to
        /// zero for no limit.
        /// </summary>
        public float VelocityTolerance;

        [Tooltip("Maximum difference between server and predicted angular velocity before inputs are replayed. Less " +
            "than or equal to zero for no limit.")]
        /// <summary>
        /// Maximum difference between server and predicted angular velocity before inputs are replayed. Less than or
        /// equal to zero for no limit.
        /// </summary>
        public float AngularVelocityTolerance;

        [Tooltip("Which properties to predict and how to predict them.")]
        /// <summary>Which properties to predict and how to predict them.</summary>
        public PropertyBehaviour[] PredictedProperties;

        /// <summary>Constructor</summary>
        public ksConvergingInputPredictorComponent()
        {
            // Get the default values from the default config.
            ksConvergingInputPredictor.ConfigData config = ksConvergingInputPredictor.ConfigData.Default;
            ConvergeMultiplier = config.ConvergeMultiplier;
            MaxDeltaTime = config.MaxDeltaTime;
            DefaultLatency = config.DefaultLatency;
            UseClientOnlyPosition = config.UseClientOnlyPosition;
            UseClientOnlyRotation = config.UseClientOnlyRotation;
            UseClientOnlyScale = config.UseClientOnlyScale;
            VelocityTolerance = config.VelocityTolerance;
            AngularVelocityTolerance = config.AngularVelocityTolerance;
        }

        /// <summary>Creates the predictor.</summary>
        private void Awake()
        {
            m_predictor = ksConvergingInputPredictor.Create();
        }

        /// <summary>Initializes the predictor with script values.</summary>
        public override bool Initialize()
        {
            ksConvergingInputPredictor.ConfigData config = null;
            if (!UseDefaultConfig)
            {
                config = new ksConvergingInputPredictor.ConfigData();
                config.ConvergeMultiplier = ConvergeMultiplier;
                config.MaxDeltaTime = MaxDeltaTime;
                config.DefaultLatency = DefaultLatency;
                config.UseClientOnlyPosition = UseClientOnlyPosition;
                config.UseClientOnlyRotation = UseClientOnlyRotation;
                config.UseClientOnlyScale = UseClientOnlyScale;
                config.VelocityTolerance = VelocityTolerance;
                config.AngularVelocityTolerance = AngularVelocityTolerance;
                m_predictor.Config = config;
            }
            if (PredictedProperties != null)
            {
                ksEntityComponent entityComponent = GetComponent<ksEntityComponent>();
                ksEntity entity = entityComponent == null ? null : entityComponent.Entity;
                if (entity != null)
                {
                    foreach (PropertyBehaviour prop in PredictedProperties)
                    {
                        entity.SetPredictionBehaviour(prop.Property, prop.Type);
                        if (prop.Tolerance > 0f && config != null)
                        {
                            config.SetPropertyChangeTolerance(prop.Property, prop.Tolerance);
                        }
                    }
                }
            }
            return base.Initialize();
        }

        /// <summary>Copies the values of this script onto <paramref name="script"/>.</summary>
        /// <param name="script">Script to copy to.</param>
        public void CopyTo(Component script)
        {
            ksConvergingInputPredictorComponent clone = (ksConvergingInputPredictorComponent)script;
            clone.UseDefaultConfig = UseDefaultConfig;
            if (!UseDefaultConfig)
            {
                clone.ConvergeMultiplier = ConvergeMultiplier;
                clone.MaxDeltaTime = MaxDeltaTime;
                clone.DefaultLatency = DefaultLatency;
                clone.UseClientOnlyPosition = UseClientOnlyPosition;
                clone.UseClientOnlyRotation = UseClientOnlyRotation;
                clone.UseClientOnlyScale = UseClientOnlyScale;
                clone.VelocityTolerance = VelocityTolerance;
                clone.AngularVelocityTolerance = AngularVelocityTolerance;
                clone.PredictedProperties = PredictedProperties;
            }
        }

        /// <summary>
        /// Calculates the velocity tolerance for an entity by finding the largest collider size along the x or z axis.
        /// Returns one if the entity has no colliders.
        /// </summary>
        /// <param name="baseEntity">Entity to calculate velocity tolerance for.</param>
        /// <returns>Velocity tolerance for the entity.</returns>
        public static float CalculateVelocityTolerance(ksBaseEntity baseEntity)
        {
            ksEntity entity = (ksEntity)baseEntity;
            float maxSize = 0f;
            if (entity.GameObject != null)
            {
                foreach (ksIUnityCollider collider in entity.GameObject.GetComponent<ksEntityComponent>().GetColliders())
                {
                    if (collider.IsEnabled)
                    {
                        maxSize = Math.Max(maxSize, GetMaxSizeXZ(collider));
                    }
                }
            }
            return maxSize <= 0f ? 1f : maxSize;
        }

        /// <summary>
        /// Gets the size of the collider in either along either the x or z axis; whichever is larger.
        /// </summary>
        /// <param name="collider">Collider to get size from.</param>
        /// <returns>The the size of the collider along the x or z axis; whichever is larger.</returns>
        public static float GetMaxSizeXZ(ksIUnityCollider collider)
        {
            if (collider is ksUnityCollider)
            {
                Collider ucollider = (Collider)collider.Component;
                return Math.Max(ucollider.bounds.size.x, ucollider.bounds.size.z);
            }
            ksCylinderCollider cylinder = collider as ksCylinderCollider;
            if (cylinder != null)
            {
                return cylinder.Radius * 2f * Math.Max(cylinder.transform.localScale.x, cylinder.transform.localScale.z);
            }
            ksConeCollider cone = collider as ksConeCollider;
            if (cone != null)
            {
                return cone.Radius * 2f * Math.Max(cone.transform.localScale.x, cone.transform.localScale.z);
            }
            return 0f;
        }
    }
}
