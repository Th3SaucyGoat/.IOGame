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
using UnityEditor;
using KS.Unity.Editor;

namespace KS.Reactor.Client.Unity.Editor
{
    /// <summary>
    /// This can be used by property drawers and custom inspectors to store per-property custom state that persists
    /// through Unity serializations. The state is not saved to disk so it only persists as long as Unity is open. A
    /// property is indentified by its name and the name of the script is belongs to.
    /// 
    /// This class inherits from <see cref="ksSingleton{T}"/>, so it should be accessed using the singleton accessor
    /// <see cref="ksSingleton{T}.Get()"/>.
    /// </summary>
    /// <typeparam name="State">State type to store with the property.</typeparam>
    /// <typeparam name="Self">The singleton type. Should be the type that inherits from class.</typeparam>
    public class ksInspectorStateMap<State, Self> : ksSingleton<Self> where Self : ScriptableObject
    {
        [ksEditable]
        private ksSerializableDictionary<string, State> m_map;

        /// <summary>The default state to return if there is no state in the map.</summary>
        public virtual State DefaultState
        {
            get { return default(State); }
        }

        /// <summary>Gets the state associated with a property.</summary>
        /// <param name="property">Property to get state for.</param>
        /// <returns>
        /// The state for the property, or <see cref="DefaultState"/> if there was no state for the property.
        /// </returns>
        public State Get(SerializedProperty property)
        {
            State state;
            if (m_map != null && m_map.TryGetValue(GetScriptPropertyName(property), out state))
            {
                return state;
            }
            return DefaultState;
        }

        /// <summary>Sets the state associated with a property.</summary>
        /// <param name="property">Property to set state for.</param>
        /// <param name="state">State to set.</param>
        public void Set(SerializedProperty property, State state)
        {
            // If the state is equal to the default state, remove the property from the map.
            if (EqualityComparer<State>.Default.Equals(state, DefaultState))
            {
                if (m_map != null)
                {
                    m_map.Remove(GetScriptPropertyName(property));
                }
            }
            else
            {
                if (m_map == null)
                {
                    m_map = new ksSerializableDictionary<string, State>();
                }
                m_map[GetScriptPropertyName(property)] = state;
            }
        }

        /// <summary>Gets the script name and propety name from a property seperated by a '.'.</summary>
        /// <param name="property">Property</param>
        /// <returns>[Script Name].[Property Name]</returns>
        private string GetScriptPropertyName(SerializedProperty property)
        {
            return property.serializedObject.targetObject.GetType().Name + "." + property.name;
        }
    }
}
