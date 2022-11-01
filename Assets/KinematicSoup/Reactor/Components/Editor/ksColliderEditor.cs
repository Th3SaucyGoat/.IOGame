using UnityEditor;
using UnityEngine;
using System.Reflection;
using KS.Unity.Editor;

namespace KS.Reactor.Client.Unity.Editor
{
    /// <summary>
    /// Base class used to override Unity collider editors. This class appends extra collider data
    /// used by Reactor to the Unity collider in the inspector.
    /// </summary>
    public class ksColliderEditor: ksOverrideEditor
    {
        protected static GUIContent m_foldoutLabel;
        private static bool m_foldout = false;
        private static string m_contactOffsetTooltip;

        /// <summary>
        /// Draw the base inspector without changes and append inspector GUI elements for the <see cref="ksColliderData"/>
        /// associated with the target collider.
        /// </summary>
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            DrawColliderData(serializedObject, target as Component);
        }

        /// <summary>
        /// Draw a ColliderData property when the viewed game object has an <see cref="ksEntityComponent"/>
        /// attached.
        /// </summary>
        /// <param name="serializedObject">Inspected game object</param>
        /// <param name="collider">Collider component</param>
        public static void DrawColliderData(SerializedObject serializedObject, Component collider)
        {
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
            SerializedProperty spColliderData = ksEntityComponentEditor.GetColliderDataProperty(collider, soEntity);
            if (spColliderData != null)
            {
                bool enterChildren = true;
                int depth = -1;
                m_foldout = EditorGUILayout.BeginFoldoutHeaderGroup(m_foldout, m_foldoutLabel);
                if (m_foldout)
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
                        while (spColliderData.NextVisible(enterChildren))
                        {
                            if (depth == -1)
                            {
                                enterChildren = false;
                                depth = spColliderData.depth;
                            }
                            else if (depth != spColliderData.depth)
                            {
                                break;
                            }
                            if (spColliderData.name == "ContactOffset")
                            {
                                if (m_contactOffsetTooltip == null)
                                {
                                    // Get the tooltip using reflection because of a Unity bug that causes the tooltip to
                                    // be null on the SerializedProperty.
                                    FieldInfo field = new ksReflectionObject(typeof(ksColliderData))
                                        .GetField("ContactOffset").FieldInfo;
                                    TooltipAttribute tooltipAttribute = field == null ?
                                        null : field.GetCustomAttribute<TooltipAttribute>();
                                    m_contactOffsetTooltip = tooltipAttribute == null ? "" : tooltipAttribute.tooltip;
                                }
                                ksStyle.NumericOverrideField(spColliderData, .01f, m_contactOffsetTooltip);
                            }
                            else
                            {
                                EditorGUILayout.PropertyField(spColliderData);
                            }
                        }
                    }
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
            }
            soEntity.ApplyModifiedProperties();
        }
    }

    [CustomEditor(typeof(SphereCollider))]
    [CanEditMultipleObjects]
    public class ksSphereColliderEditor : ksColliderEditor
    {
        protected override void OnEnable() { LoadBaseEditor("SphereColliderEditor"); }
    }

    [CustomEditor(typeof(BoxCollider))]
    [CanEditMultipleObjects]
    public class ksBoxColliderEditor : ksColliderEditor
    {
        protected override void OnEnable() { LoadBaseEditor("BoxColliderEditor"); }
    }

    [CustomEditor(typeof(CapsuleCollider))]
    [CanEditMultipleObjects]
    public class ksCapsuleColliderEditor : ksColliderEditor
    {
        protected override void OnEnable() { LoadBaseEditor("CapsuleColliderEditor"); }
    }

    [CustomEditor(typeof(MeshCollider))]
    [CanEditMultipleObjects]
    public class ksMeshColliderEditor : ksColliderEditor
    {
        protected override void OnEnable() { LoadBaseEditor("MeshColliderEditor"); }
    }

    [CustomEditor(typeof(TerrainCollider))]
    [CanEditMultipleObjects]
    public class ksTerrainColliderEditor : ksColliderEditor
    {
        protected override void OnEnable() { LoadBaseEditor("TerrainColliderEditor"); }
    }
}