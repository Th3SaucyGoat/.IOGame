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

namespace KS.Reactor.Client.Unity.Editor
{
    /// <summary>Custom inspector for types derived from <see cref="ksPredictor"/>.</summary>
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ksPredictor), true)]
    public class ksPredictorEditor : UnityEditor.Editor
    {
        /// <summary>Creates the inspector GUI.</summary>
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
                EditorGUILayout.PropertyField(property);
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}