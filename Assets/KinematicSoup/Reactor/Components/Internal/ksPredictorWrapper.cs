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
    /// Base class for predictor scripts that wrap the built-in Reactor predictors, allowing them to be attached to
    /// game objects in Unity. It follows the same usage rules as <see cref="ksPredictor"/>.
    /// </summary>
    /// <typeparam name="T">Type of <see cref="ksIPredictor"/> to wrap.</typeparam>
    public abstract class ksPredictorWrapper<T> : ksPredictor
        where T : ksIPredictor
    {
        protected T m_predictor;

        /// <summary>Initializes the predictor.</summary>
        /// <returns>False if the predictor could not be initialized and needs to be removed.</returns>
        public override bool Initialize()
        {
            return m_predictor.Initialize(Room, Entity);
        }

        /// <summary>Performs clean up.</summary>
        public override void CleanUp()
        {
            m_predictor.CleanUp();
        }

        /// <summary>Initializes smoothing for a property. Called once per property that will be smoothed.</summary>
        /// <param name="propertyId">Id of property that will be smoothed.</param>
        /// <param name="value">Property value.</param>
        /// <param name="smoothingData">Smoothing data for property prediction.</param>
        public override void AddProperty(uint propertyId, ksMultiType value, ksPredictionBehaviour smoothingData)
        {
            m_predictor.AddProperty(propertyId, value, smoothingData);
        }

        /// <summary>Called once per server frame.</summary>
        /// <param name="state">
        /// Server transform and velocity data. Velocity data is currently not synced and is always zero. Null if not
        /// smoothing a transform.
        /// </param>
        /// <param name="properties">
        /// Smoothed properties whose values changed since the last frame. Null if no properties have changed.
        /// </param>
        /// <param name="teleport">Did the entity teleport?</param>
        /// <param name="idle">True if there were no transform or property changes since the last frame.</param>
        /// <returns>
        /// If false and <paramref name="idle"/> is true, this function will not be called again until the server
        /// transform or a server property changes.
        /// </returns>
        public override bool ServerUpdate(ksPhysicsState state, Dictionary<uint, ksMultiType> properties, bool teleport, bool idle)
        {
            return m_predictor.ServerUpdate(state, properties, teleport, idle);
        }

        /// <summary>Updates the client transform and properties.</summary>
        /// <param name="state">
        /// Client transform and velocity to update with new values. Null if not smoothing a transform.
        /// </param>
        /// <param name="properties">
        /// Smoothed client properties to update with new values. Null if there are no smoothed properties.
        /// </param>
        /// <returns>
        /// If false, this function will not be called again until the server transform or a server property changes.
        /// </returns>
        public override bool ClientUpdate(ksPhysicsState state, Dictionary<uint, ksMultiType> properties)
        {
            return m_predictor.ClientUpdate(state, properties);
        }

        /// <summary>
        /// Called when a new frame of input is generated. Only called for entities with player controllers.
        /// </summary>
        /// <param name="input">Input</param>
        public override void InputUpdate(ksInput input)
        {
            m_predictor.InputUpdate(input);
        }
    }
}
