using System.Reflection;
using UnityEditor;
using UnityEngine;
using KS.Unity.Editor;

namespace KS.Reactor.Client.Unity.Editor
{
    /// <summary>
    /// Override editor for the Unity RigidbodyEditor. Editor appends GUIs for extra physics data used in Reactor
    /// <see cref="ksRigidBody"/> scripts from the <see cref="ksEntityComponent"/>.
    /// </summary>
    [CanEditMultipleObjects]
    [CustomEditor(typeof(Rigidbody))]
    public class ksRigidBodyEditor : ksOverrideEditor
    {
        protected static GUIContent m_foldoutLabel;
        private static bool m_foldout = false;
        private static ksReflectionObject m_reflectionObj;


        /// <summary>
        /// Load the Unity RigidbodyEditor as the base editor when this editor is enabled.
        /// </summary>
        protected override void OnEnable() { LoadBaseEditor("RigidbodyEditor"); }

        /// <summary>
        /// Draw the base inspector and append the GUI to include <see cref="ksPhysicsSettings"/>
        /// properties used by <see cref="ksRigidBody"/> scripts.
        /// </summary>
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            DrawRigidBodyData(serializedObject, target as Component);
        }

        /// <summary>
        /// Draw <see cref="ksPhysicsSettings"/> property GUIs used by <see cref="ksRigidBody"/> scripts when a 
        /// <see cref="ksEntityComponent"/> is also attached to the game object.
        /// </summary>
        /// <param name="serializedObject">Inspected game object</param>
        /// <param name="collider">Collider component</param>
        private static void DrawRigidBodyData(SerializedObject serializedObject, Component collider)
        {
            ksEntityComponent entityComponent = collider.GetComponent<ksEntityComponent>();
            if (entityComponent == null || !entityComponent.enabled)
            {
                return;
            }

            if (m_foldoutLabel == null)
            {
                m_foldoutLabel = new GUIContent(" Reactor RigidBody Data", ksTextures.Logo);
            }

            if (m_reflectionObj == null)
            {
                m_reflectionObj = new ksReflectionObject(new ksEntityPhysicsSettings());
            }

            SerializedObject soEntity = new SerializedObject(entityComponent);
            SerializedProperty spRigidBodyData = soEntity.FindProperty("PhysicsOverrides");
            if (spRigidBodyData != null)
            {
                bool enterChildren = true;
                int depth = -1;
                m_foldout = EditorGUILayout.BeginFoldoutHeaderGroup(m_foldout, m_foldoutLabel);
                if (m_foldout)
                {
                    if (serializedObject.isEditingMultipleObjects)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.HelpBox(
                            "Cannot edit Reactor rigid body data for multiple game objects at once.",
                            MessageType.Warning);
                        EditorGUI.indentLevel--;
                    }
                    else
                    {
                        while (spRigidBodyData.NextVisible(enterChildren))
                        {
                            if (depth == -1)
                            {
                                enterChildren = false;
                                depth = spRigidBodyData.depth;
                            }
                            else if (depth != spRigidBodyData.depth)
                            {
                                break;
                            }
                            if (spRigidBodyData.name != "ContactOffset")
                            {
                                EditorGUI.indentLevel++;
                                EditorGUILayout.BeginHorizontal();
                                EditorGUI.BeginChangeCheck();
                                // Get the tooltip using reflection because of Unity bug that causes the tooltip to be null
                                // on the SerializedProperty.
                                ksReflectionObject m_field = m_reflectionObj.GetField(spRigidBodyData.name);
                                TooltipAttribute tooltipAttribute = m_field.FieldInfo == null ?
                                    null : m_field.FieldInfo.GetCustomAttribute<TooltipAttribute>();
                                string tooltip = tooltipAttribute == null ? null : tooltipAttribute.tooltip;
                                // Display a checkbox to override the property. The checkbox is unchecked if the value is negative.
                                bool value = EditorGUILayout.Toggle(
                                    new GUIContent(spRigidBodyData.displayName, tooltip),
                                    spRigidBodyData.propertyType == SerializedPropertyType.Float ?
                                    spRigidBodyData.floatValue >= 0 : spRigidBodyData.intValue >= 0);
                                if (EditorGUI.EndChangeCheck())
                                {
                                    // Change the toggle value.
                                    if (!value)
                                    {
                                        if (spRigidBodyData.propertyType == SerializedPropertyType.Float)
                                        {
                                            spRigidBodyData.floatValue = -1f;
                                        }
                                        else
                                        {
                                            spRigidBodyData.intValue = -1;
                                        }
                                    }
                                    else
                                    {
                                        // If the toggle just became checked, set the value to the default value.
                                        if (spRigidBodyData.propertyType == SerializedPropertyType.Float)
                                        {
                                            spRigidBodyData.floatValue = (float)m_field.GetValue();
                                        }
                                        else
                                        {
                                            spRigidBodyData.intValue = (int)m_field.GetValue();
                                        }
                                    }
                                }
                                EditorGUI.indentLevel--;
                                // Display the property if the toggle is checked.
                                if (value)
                                {
                                    EditorGUILayout.PropertyField(spRigidBodyData, new GUIContent(""));
                                }
                                EditorGUILayout.EndHorizontal();
                            }
                        }
                    }
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
            }
            soEntity.ApplyModifiedProperties();
        }
    }
}