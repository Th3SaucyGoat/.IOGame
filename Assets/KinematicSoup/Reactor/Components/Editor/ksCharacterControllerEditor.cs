using UnityEditor;
using UnityEngine;

namespace KS.Reactor.Client.Unity.Editor
{
    /// <summary>
    /// A class that used to override Unity character controller editor. This class appends extra collider data
    /// used by Reactor to the Unity character controller in the inspector.
    /// </summary>
    [CustomEditor(typeof(CharacterController))]
    [CanEditMultipleObjects]
    public class ksCharacterControllerEditor : ksOverrideEditor
    {
        private static GUIContent m_foldoutLabel;
        private bool m_expanded = true;

        protected override void OnEnable() { LoadBaseEditor("CharacterControllerEditor"); }

        /// <summary>
        /// Draw the base inspector without changes and append inspector GUI elements for the <see cref="ksColliderData"/>
        /// associated with the target collider.
        /// </summary>
        public override void OnInspectorGUI()
        {
            if (m_baseEditor != null)
            {
                m_baseEditor.OnInspectorGUI();
            }

            Component collider = target as Component;
            ksEntityComponent entityComponent = collider.GetComponent<ksEntityComponent>();
            if (entityComponent == null || !entityComponent.enabled)
            {
                return;
            }

            if (m_foldoutLabel == null)
            {
                m_foldoutLabel = new GUIContent(" Reactor Collider Data", ksTextures.Logo);
            }

            SerializedObject soEntity = new SerializedObject(entityComponent);
            ksEntityComponentEditor.CheckColliderData(soEntity);
            SerializedProperty iterator = ksEntityComponentEditor.GetColliderDataProperty(collider, soEntity);
            if (iterator != null)
            {
                // Draw the foldout.
                m_expanded = EditorGUILayout.BeginFoldoutHeaderGroup(m_expanded, m_foldoutLabel);
                if (m_expanded)
                {
                    EditorGUI.indentLevel++;
                    if (serializedObject.isEditingMultipleObjects)
                    {
                        EditorGUILayout.HelpBox(
                            "Cannot edit Reactor collider data for multiple game objects at once.",
                            MessageType.Warning);
                    }
                    else
                    {
                        bool enterChildren = true;
                        while (iterator.NextVisible(enterChildren))
                        {
                            if (iterator.name == "Filter")
                            {

                                EditorGUILayout.PropertyField(iterator);
                                break;
                            }
                            enterChildren = iterator.hasVisibleChildren && iterator.isExpanded;
                        }
                    }
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
            }
            soEntity.ApplyModifiedProperties();
        }
    }
}
