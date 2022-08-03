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
    /// Wraps a <see cref="ksLinearPredictor"/>, allowing it to be attached to a game object for an entity or room
    /// type and have its parameters edited in the inspector.
    /// </summary>
    public class ksLinearPredictorComponent : ksPredictorWrapper<ksLinearPredictor>, ksICloneableScript
    {
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

            [Tooltip("Property-correction interpolation rate per second. If less than or equal to zero, " +
                "PositionCorrectionRate is used for linearly interpolated properties and RotationCorrectionRate is " +
                "used for spherically interpolated propreties. Or if it uses wrap float interpolation, the " +
                "difference between the min and max range values is used.")]
            /// <summary>Correction interpolation rate. If less than or equal to zero,
            /// <see cref="PositionCorrectionRate"/> will be used if it is linearly interpolated, and
            /// <see cref="RotationCorrectionRate"/> will be used if it is spherically interpolated. If the property
            /// uses wrap float interpolation and has no rate set, it will use the difference between the min and the
            /// max range values.</summary>
            public float CorrectionRate;
        }

        [Tooltip("Should this predictor use the default config defined in ksLinearPredictor.ConfigData.Default?")]
        /// <summary>Should the predictor use <see cref="ksLinearPredictor.ConfigData.Default"/>?</summary>
        public bool UseDefaultConfig = true;

        [Tooltip("Position-correction interpolation rate per second.")]
        /// <summary>Position-correction interpolation rate per second.</summary>
        public float PositionCorrectionRate = 10f;

        [Tooltip("Rotation-correction interpolation rate in degrees per second.")]
        /// <summary>Rotation-correction interpolation rate in degrees per second.</summary>
        public float RotationCorrectionRate = 360f;

        [Tooltip("Scale-correction interpolation rate per second.")]
        /// <summary>Scale-correction interpolation rate per second.</summary>
        public float ScaleCorrectionRate = 10f;

        [Tooltip("Which properties to predict and how to predict them.")]
        /// <summary>Which properties to predict and how to predict them.</summary>
        public PropertyBehaviour[] PredictedProperties;

        /// <summary>
        /// Constructor
        /// </summary>
        public ksLinearPredictorComponent()
        {
            // Get the default values from the default config.
            ksLinearPredictor.ConfigData config = ksLinearPredictor.ConfigData.Default;
            PositionCorrectionRate = config.PositionCorrectionRate;
            RotationCorrectionRate = config.RotationCorrectionRate;
            ScaleCorrectionRate = config.ScaleCorrectionRate;
        }

        /// <summary>Creates the predictor.</summary>
        private void Awake()
        {
            m_predictor = ksLinearPredictor.Create();
        }

        /// <summary>Initializes the predictor with script values.</summary>
        public override bool Initialize()
        {
            ksLinearPredictor.ConfigData config = null;
            if (!UseDefaultConfig)
            {
                config = new ksLinearPredictor.ConfigData();
                config.PositionCorrectionRate = PositionCorrectionRate;
                config.RotationCorrectionRate = RotationCorrectionRate;
                config.ScaleCorrectionRate = ScaleCorrectionRate;
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
                        if (prop.CorrectionRate >= 0f && config != null)
                        {
                            config.SetPropertyCorrectionRate(prop.Property, prop.CorrectionRate);
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
            ksLinearPredictorComponent clone = (ksLinearPredictorComponent)script;
            clone.UseDefaultConfig = UseDefaultConfig;
            if (!UseDefaultConfig)
            {
                clone.PositionCorrectionRate = PositionCorrectionRate;
                clone.RotationCorrectionRate = RotationCorrectionRate;
                clone.ScaleCorrectionRate = ScaleCorrectionRate;
                clone.PredictedProperties = PredictedProperties;
            }
        }
    }
}
