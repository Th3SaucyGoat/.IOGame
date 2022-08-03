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
using UnityEngine;

namespace KS.Reactor.Client.Unity
{
    /// <summary>Base class for Reactor MonoBehaviour scripts.</summary>
    /// <typeparam name="Parent">Type the script can be attached to.</typeparam>
    /// <typeparam name="Script">Script type</typeparam>
    public abstract class ksMonoScript<Parent, Script> : MonoBehaviour, ksMonoScript<Parent, Script>.IInternals
        where Parent : class
        where Script : ksMonoScript<Parent, Script>
    {
        /// <summary>
        /// Internal interface. These members are hidden unless the class is cast to this interface.
        /// </summary>
        public interface IInternals
        {
            ksScriptsComponent<Parent, Script> Component { get; set; }
            Parent Parent { get; }
            bool AddToScriptList { get; }
            bool SetupComponent(bool checkRoomType);
        }

        /// <summary>Get the most derived type of this script.</summary>
        public Type InstanceType
        {
            get
            {
                if (m_type == null)
                {
                    m_type = GetType();
                }
                return m_type;
            }
        }
        private Type m_type;

        /// <summary>Room the script is in.</summary>
        public abstract ksRoom Room { get; }

        /// <summary>Server time and local frame delta.</summary>
        public ksClientTime Time
        {
            get { return Room == null ? ksClientTime.Zero : Room.Time; }
        }

        /// <summary>Loader for <see cref="ksScriptAsset"/>s in Resources or asset bundles.</summary>
        public ksBaseAssetLoader Assets
        {
            get { return ksScriptAsset.Assets; }
        }

        /// <summary>Component the script is attached to.</summary>
        ksScriptsComponent<Parent, Script> IInternals.Component
        {
            get { return m_component; }
            set { m_component = value; }
        }
        private ksScriptsComponent<Parent, Script> m_component;

        /// <summary>The parent (room, entity, or player) for this script.</summary>
        Parent IInternals.Parent
        {
            get
            {
                if (m_component != null)
                {
                    return m_component.ParentObject;
                }
                ((IInternals)this).SetupComponent(false);
                return m_component == null ? null : m_component.ParentObject;
            }
        }

        /// <summary>
        /// Should this script be added to the <see cref="ksScriptsComponent{Parent, Script}"/> list of scripts?
        /// </summary>
        bool IInternals.AddToScriptList
        {
            get { return m_addToScriptList; }
        }
        [SerializeField]
        [HideInInspector]
        private bool m_addToScriptList = true;

        /// <summary>
        /// Unity OnDestroy. Removes the script from the component and, if the script was is in the component script
        /// list, calls <see cref="Detached()"/>.
        /// </summary>
        private void OnDestroy()
        {
            if (m_component == null)
            {
                return;
            }
            if (m_component.IsInitialized && m_component.Scripts.Remove((Script)this))
            {
                Detached();
            }
        }

        /// <summary>Called after properties are initialized.</summary>
        public virtual void Initialize()
        {

        }

        /// <summary>
        /// Called when the script is detached. Remove event listeners and perform other clean up logic here.
        /// </summary>
        public virtual void Detached()
        {

        }

        /// <summary>
        /// Called when a script wakes. This method stores the initial enabled state of a script.  If the
        /// script was not enabled, then we set a value indicating that the script should not be included in the
        /// list of scripts on <see cref="ksScriptsComponent{Parent, Script}"/>
        /// </summary>
        private void Awake()
        {
            m_addToScriptList = enabled;
        }

        /// <summary>Finds the script component and performs initialization.</summary>
        /// <param name="checkRoomType">If true, will check for a <see cref="ksRoomType"/> and disable itself if found.</param>
        /// <returns>True if script component (or <see cref="ksRoomType"/>) for this script type was found.</returns>
        bool IInternals.SetupComponent(bool checkRoomType)
        {
            if (m_component != null)
            {
                return true;
            }
            if (checkRoomType && GetComponent<ksRoomType>() != null)
            {
                enabled = false;
                return true;
            }
            m_component = GetComponent<ksScriptsComponent<Parent, Script>>();
            if (m_component != null)
            {
                if (m_component.ParentObject == null)
                {
                    m_component = null;
                }
                else if (!m_component.Scripts.Contains((Script)this))
                {
                    m_component.Scripts.Add((Script)this);
                    if (m_component.IsInitialized)
                    {
                        ksRPCManager<ksRPCAttribute>.Instance.RegisterTypeRPCs(InstanceType);
                        Initialize();
                        enabled = true;
                    }
                    else
                    {
                        enabled = false;
                    }
                }
                return true;
            }
            return false;
        }
    }
}
