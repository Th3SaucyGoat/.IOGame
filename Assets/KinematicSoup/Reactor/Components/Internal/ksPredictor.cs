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
    /// Base class for developers to write custom predictors for smoothing object movement and property data. Attach
    /// a script derived from this to an entity game object to use it. If <see cref="RequiresController"/> is true, it
    /// will be used only if the entity has a player controller with
    /// <see cref="ksPlayerController.UseInputPrediction"/> set to true. If <see cref="RequiresController"/> is false,
    /// it will only be used for entities that do not have a controller with 
    /// <see cref="ksPlayerController.UseInputPrediction"/> set to true. You can also attach it to the
    /// <see cref="ksRoomType"/> game object to make it the default predictor.
    /// </summary>
    public abstract class ksPredictor : MonoBehaviour, ksIPredictor
    {
        /// <summary>Enum for determing when to use this predictor.</summary>
        public enum UseModes
        {
            /// <summary>Use with entities that don't have a controller with input prediction enabled.</summary>
            NO_CONTROLLER = 0,
            /// <summary>Use with entities that have a controller with input prediction enabled.</summary>
            CONTROLLER = 1,
            /// <summary>Use with entities with and without a controller.</summary>
            ALL = 2
        }

        /// <summary>Determines if the predictor is used with entities with or without controllers, or both.</summary>
        public UseModes UseWith
        {
            get { return RequiresController ? UseModes.CONTROLLER : m_useWith; }
            set
            {
                if (RequiresController && value != UseModes.CONTROLLER)
                {
                    ksLog.Warning(this, "Cannot set UseWith when RequiresController is true.");
                }
                else
                {
                    m_useWith = value;
                }
            }
        }
        [ksEnum]
        [SerializeField]
        [Tooltip("Should this predictor be used for entities with controllers, without, or both?")]
        private UseModes m_useWith;

        /// <summary>The room.</summary>
        public ksRoom Room
        {
            get { return m_room; }
        }
        private ksRoom m_room;

        /// <summary>The entity we are predicting movement for. Null if predicting room or player properties.</summary>
        public ksEntity Entity
        {
            get { return m_entity; }
        }
        private ksEntity m_entity;

        /// <summary>Server and local time data.</summary>
        public ksClientTime Time
        {
            get { return m_room == null ? ksClientTime.Zero : m_room.Time; }
        }

        /// <summary>
        /// The player controller for the <see cref="Entity"/>. Null if the entity has no controller or the controller
        /// has <see cref="ksPlayerController.UseInputPrediction"/> set to false.
        /// </summary>
        public ksPlayerController Controller
        {
            get 
            { 
                return m_entity == null || m_entity.PlayerController == null || 
                    !m_entity.PlayerController.UseInputPrediction ? null : m_entity.PlayerController; 
            }
        }

        /// <summary>Does the predictor require a player controller?</summary>
        public virtual bool RequiresController
        {
            get { return false; }
        }

        /// <summary>Constructor</summary>
        public ksPredictor()
        {
            UseWith = RequiresController ? UseModes.CONTROLLER : UseModes.NO_CONTROLLER;
        }

        /// <summary>
        /// Does nothing. Declaring this method allows the predictor to be disabled in the inspector.
        /// </summary>
        private void Start()
        {
            
        }

        /// <summary>Initializes the predictor.</summary>
        /// <param name="room">The room object.</param>
        /// <param name="entity">
        /// Entity the predictor is smoothing. Null if the predictor is smoothing room or player properties.
        /// </param>
        /// <returns>False if the predictor could not be initialized and needs to be removed.</returns>
        public bool Initialize(ksBaseRoom room, ksBaseEntity entity)
        {
            m_room = (ksRoom)room;
            m_entity = (ksEntity)entity;
            Initialize();
            return true;
        }

        /// <summary>Initializes the predictor.</summary>
        /// <returns>False if the predictor could not be initialized and needs to be removed.</returns>
        public virtual bool Initialize()
        {
            return true;
        }

        /// <summary>Performs clean up.</summary>
        public virtual void CleanUp()
        {
            
        }

        /// <summary>
        /// Called once per property that will be smoothed. Do any initialization required to smooth a property here.
        /// If the <see cref="ksPredictionBehaviour"/> for the property is important for your predictor, you should
        /// use <paramref name="smoothingData"/> to determine how to smooth the property here.
        /// </summary>
        /// <param name="propertyId">Id of property that will be smoothed.</param>
        /// <param name="value">Property value.</param>
        /// <param name="smoothingData">
        /// Smoothing data for property prediction describing how the property should be predicted.
        /// </param>
        public virtual void AddProperty(uint propertyId, ksMultiType value, ksPredictionBehaviour smoothingData)
        {

        }

        /// <summary>
        /// Called once per server frame. Perform any relevant calculations with server frame data here, and/or store
        /// the relevant server frame data to be used in calculating future client state in
        /// <see cref="ClientUpdate(ksPhysicsState, Dictionary{uint, ksMultiType})"/>.
        /// </summary>
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
        public virtual bool ServerUpdate(ksPhysicsState state, Dictionary<uint, ksMultiType> properties, bool teleport, bool idle)
        {
            return false;
        }

        /// <summary>
        /// Called once per client render frame to update the client transform and smoothed properties.
        /// </summary>
        /// <param name="state">
        /// Client transform and velocity to update with new values. Null if not smoothing a transform.
        /// </param>
        /// <param name="properties">
        /// Smoothed client properties to update with new values. Null if there are no smoothed properties.
        /// </param>
        /// <returns>
        /// If false, this function will not be called again until the server transform or a server property changes.
        /// </returns>
        public virtual bool ClientUpdate(ksPhysicsState state, Dictionary<uint, ksMultiType> properties)
        {
            return false;
        }

        /// <summary>
        /// Called when a new frame of input is generated. Only called for entities with player controllers.
        /// </summary>
        /// <param name="input">Input</param>
        public virtual void InputUpdate(ksInput input)
        {
            input.CleanUp();
        }
    }
}