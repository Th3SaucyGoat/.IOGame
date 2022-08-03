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
using UnityEditor;
using KS.Unity.Editor;

namespace KS.Reactor.Client.Unity.Editor
{
    /// <summary>Custom inspector for types derived from <see cref="ksPredictorWrapper{T}"/>.</summary>
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ksPredictorWrapper<>), true)]
    public class ksPredictorWrapperEditor : UnityEditor.Editor
    {
        private bool m_expandProperties = false;

        /// <summary>
        /// Creates the inspector GUI. Hides all fields after UseDefaultConfig except for PredictedProperties if
        /// UseDefaultConfig is true. Predictors with the UseDefaultConfig property have a Config class with 
        /// configurable parameters, and default static instance of this class. The predictor's config pointer will
        /// point to the default static instance if UseDefaultConfig is true.
        /// </summary>
        public override void OnInspectorGUI()
        {
            ksPredictor predictor = target as ksPredictor;
            if (predictor.GetComponent<ksRoomType>() == null && predictor.GetComponent<ksEntityComponent>() == null)
            {
                EditorGUILayout.HelpBox("No ksRoomType or ksEntityComponent found on this game object. " +
                    "You cannot use this script without a ksRoomType or ksEntityComponent.", MessageType.Warning);
            }

            SerializedProperty property = serializedObject.GetIterator();
            bool enterChildren = true;
            bool useDefaultConfig = false;
            while (property.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (property.name == "m_Script")
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.PropertyField(property);
                    EditorGUI.EndDisabledGroup();
                    continue;
                }
                if (property.name == "m_useWith" && predictor != null && predictor.RequiresController)
                {
                    // Don't allow editing UseWidth for predictors that always require a controller.
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.PropertyField(property);
                    EditorGUI.EndDisabledGroup();
                    continue;
                }
                if (property.name == "PredictedProperties")
                {
                    continue;
                }
                DrawProperty(property);
                if (property.name == "UseDefaultConfig" && (property.boolValue || property.hasMultipleDifferentValues))
                {
                    useDefaultConfig = true;
                    break;
                }
            }
            property = serializedObject.FindProperty("PredictedProperties");
            if (property != null)
            {
                DrawPredictedProperties(property, useDefaultConfig);
            }
            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>Creates the property field. Can be overridden in derived classes.</summary>
        /// <param name="property">Property to draw.</param>
        protected virtual void DrawProperty(SerializedProperty property)
        {
            EditorGUILayout.PropertyField(property);
        }

        /// <summary>
        /// Draws the predicted properties with buttons for adding or removing predicted properties. Draws only the
        /// first two fields of each predicted property (id and prediction behaviour) if
        /// <paramref name="useDefaultConfig"/> is true. This is because the subsequent fields are part of the Config
        /// object and are unused when using the default config.
        /// </summary>
        /// <param name="property">Predicted properties.</param>
        /// <param name="useDefaultConfig">
        /// Are we using the default config? If true, only draw the first two fields of each predicted property.
        /// </param>
        private void DrawPredictedProperties(SerializedProperty property, bool useDefaultConfig)
        {
            m_expandProperties = EditorGUILayout.Foldout(m_expandProperties,
                new GUIContent(property.displayName, property.tooltip));
            if (!m_expandProperties)
            {
                return;
            }
            EditorGUI.indentLevel++;
            SerializedProperty listProperty = property;
            property = property.Copy();
            property.NextVisible(true);
            int size = property.intValue;
            property.NextVisible(true); // data;
            int deleteIndex = -1;
            // Iterate the list.
            for (int i = 0; i < size; i++)
            {
                property.NextVisible(true);// Id
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(property);
                if (GUILayout.Button("X"))
                {
                    deleteIndex = i;
                }
                EditorGUILayout.EndHorizontal();
                property.NextVisible(true);// Type
                EditorGUILayout.PropertyField(property);

                // Iterate remaining fields.
                int depth = property.depth;
                while (property.NextVisible(false) && property.depth == depth)
                {
                    // Only show remaing fields if not using the default config.
                    if (!useDefaultConfig)
                    {
                        EditorGUILayout.PropertyField(property);
                    }
                }
                EditorGUILayout.Space();
            }
            // If the user pressed the delete button for a property, delete that property.
            if (deleteIndex >= 0)
            {
                listProperty.DeleteArrayElementAtIndex(deleteIndex);
            }
            // Create a button to add a new property.
            if (ksStyle.Button("+"))
            {
                listProperty.InsertArrayElementAtIndex(listProperty.arraySize);
            }
            EditorGUI.indentLevel--;
        }
    }
}