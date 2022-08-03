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
using UnityEngine;
using UnityEditor;
using System.Reflection;
using UnityEditor.Experimental.SceneManagement;
using System.IO;
using UnityEditor.SceneManagement;

namespace KS.Reactor.Client.Unity.Editor
{
    /// <summary>Inspector editor for <see cref="ksEntityComponent"/>.</summary>
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ksEntityComponent))]
    public class ksEntityComponentEditor : UnityEditor.Editor
    {
        // Properties with these names will be skipped when rendering the inspector
        private readonly HashSet<string> m_skipProperties = new HashSet<string>() { 
            "m_Script", "m_editorData", "Type"
        };

        // Properties with these names will be disabled if the object is static
        private readonly HashSet<string> m_disableWhenStatic = new HashSet<string>() {
            "Mass", "IgnoreGravity", "IsKinematic", "Damping", "AngularDamping", "EntityPhysicsOverrides",
            "TranslationLock", "RotationLock"
        };

        // Properties with these names will be only shown if the object is static
        private readonly HashSet<string> m_showWhenStatic = new HashSet<string>() {
            "m_isPermanent"
        };

        // Properties with these names will be rendered in a debug group
        private readonly HashSet<string> m_debugProperties = new HashSet<string>() { 
            "ShowServerGhost", "OverrideServerGhostMaterial", "OverrideServerGhostColor",
            "AssetId", "EntityId", "CenterOfMass"
        };

        private static bool m_showDebug = false;

        /// <summary>Creates the inspector GUI.</summary>
        public override void OnInspectorGUI()
        {
            CheckEntityPrefabResourcePaths();

            serializedObject.Update();
            bool hasRoomType = false;
            bool isStatic = true;
            foreach (ksEntityComponent entity in targets)
            {
                hasRoomType = hasRoomType || entity.GetComponent<ksRoomType>() != null;
                isStatic = isStatic && entity.GetComponent<Rigidbody>() == null;
            }

            if (targets.Length == 1)
            {
                EditorGUILayout.LabelField("Type", GetEntityType());
            }

            DrawProperties(isStatic);

            if (hasRoomType)
            {
                EditorGUILayout.HelpBox("ksRoomType detected on this object. " +
                    "KSEntities are ignored for objects with KSRoomTypes.", MessageType.Error, true);
            }
        }

        /// <summary>Get the entity type.</summary>
        /// <returns>Entity type</returns>
        private string GetEntityType()
        {
            ksEntityComponent entity = targets[0] as ksEntityComponent;
            if (EditorApplication.isPlaying)
            {
                return entity.Type;
            }
            GameObject prefab = null;
            if (PrefabUtility.IsPartOfNonAssetPrefabInstance(entity))
            {
                prefab = PrefabUtility.GetCorrespondingObjectFromSource(entity.gameObject) as GameObject;
            }
            else if (PrefabUtility.IsPartOfPrefabAsset(entity))
            {
                prefab = entity.gameObject;
            }
            string assetPath;
            if (prefab == null)
            {
                PrefabStage prefabStage = PrefabStageUtility.GetPrefabStage(entity.gameObject);
                if (prefabStage == null)
                {
                    return "";
                }
#if UNITY_2020_1_OR_NEWER
                assetPath = prefabStage.assetPath;
#else
                assetPath = prefabStage.prefabAssetPath;
#endif
            }
            else
            {
                assetPath = AssetDatabase.GetAssetPath(prefab.GetInstanceID()).Replace('\\', '/');
            }
            string assetBundle = GetAssetBundle(assetPath);
            return ksPaths.GetEntityType(assetPath, assetBundle);
        }

        /// <summary>
        /// Gets the name of the asset bundle the asset at a path belongs to.
        /// </summary>
        /// <param name="assetPath">asset path to get asset bundle name for.</param>
        /// <returns>Name of the asset bundle, or null if the asset does not belong to an asset bundle.</returns>
        private string GetAssetBundle(string assetPath)
        {
            AssetImporter importer = AssetImporter.GetAtPath(assetPath);
            if (!string.IsNullOrEmpty(importer.assetBundleName))
            {
                if (!string.IsNullOrEmpty(importer.assetBundleVariant))
                {
                    return importer.assetBundleName + "." + importer.assetBundleVariant;
                }
                return importer.assetBundleName;
            }
            int index = assetPath.LastIndexOf('/');
            if (index < 0)
            {
                return null;
            }
            assetPath = assetPath.Substring(0, index);
            return assetPath == "Assets" ? null : GetAssetBundle(assetPath);
        }

        /// <summary>Draw editor widgets for the entity component properties.</summary>
        /// <param name="isStatic">Are the selected entities static?</param>
        private void DrawProperties(bool isStatic)
        {
            List<SerializedProperty> debugGroup = new List<SerializedProperty>();

            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (m_skipProperties.Contains(iterator.name))
                {
                    continue;
                }

                if (m_showWhenStatic.Contains(iterator.name) && !isStatic)
                {
                    continue;
                }

                if (m_debugProperties.Contains(iterator.name))
                {
                    debugGroup.Add(iterator.Copy());
                    continue;
                }

                EditorGUI.BeginDisabledGroup(m_disableWhenStatic.Contains(iterator.name) && isStatic);
                switch (iterator.name)
                {
                    case "ColliderData": break;
                    case "PhysicsOverrides": break;
                    default: EditorGUILayout.PropertyField(iterator); break;
                }
                EditorGUI.EndDisabledGroup();
            }

            DrawPropertyGroup("Debugging", debugGroup, ref m_showDebug);

            foreach (ksEntityComponent entity in targets)
            {
                entity.TransformPrecisionOverrides.Validate();
                entity.PhysicsOverrides.Validate();
            }

            if (GUI.changed)
            {
                serializedObject.ApplyModifiedProperties();

                foreach (UnityEngine.Object target in targets)
                {
                    ksEntityComponent entity = target as ksEntityComponent;
                    if (entity == null || entity.Entity == null)
                    {
                        continue;
                    }
                    entity.Entity.ApplyServerGhostMaterial();
                }
            }
        }

        /// <summary>Draw a list of serialized properties contained by a foldout.</summary>
        /// <param name="groupName">Foldout group name.</param>
        /// <param name="properties">List of serialized properties.</param>
        /// <param name="expanded">Expanded state of the group.</param>
        private void DrawPropertyGroup(string groupName, List<SerializedProperty> properties, ref bool expanded)
        {
            expanded = EditorGUILayout.Foldout(expanded, groupName);
            if (expanded)
            {
                EditorGUI.indentLevel = 1;
                foreach (SerializedProperty property in properties)
                {
                    EditorGUILayout.PropertyField(property);
                }
                EditorGUI.indentLevel = 0;
            }
        }

        /// <summary>
        /// Get the path of a prefab that owns an an entity which is not an instance.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public string GetPrefabPath(ksEntityComponent entity)
        {
            // If the object is in a prefab stage, then use the stage to get the path.
            PrefabStage prefabStage = PrefabStageUtility.GetPrefabStage(entity.gameObject);
            if (prefabStage != null)
            {
#if UNITY_2020_1_OR_NEWER
                return prefabStage.assetPath;
#else
                return prefabStage.prefabAssetPath;
#endif
            }

            // If the object is persitent (a prefab which is not an instance) then return the path.
            if (PrefabUtility.IsPartOfPrefabAsset(entity))
            {
                return AssetDatabase.GetAssetPath(entity);
            }
            return null;
        }

        /// <summary>
        /// Write warnings if the entity is a prefab and located outside of a resources folder.
        /// </summary>
        private void CheckEntityPrefabResourcePaths()
        {
            List<string> misplacedPrefabs = new List<string>();
            foreach (UnityEngine.Object target in targets)
            {
                ksEntityComponent entity = target as ksEntityComponent;
                string path = GetPrefabPath(entity);

                if (!string.IsNullOrEmpty(path) && !(
                    path.Contains(Path.DirectorySeparatorChar + "Resources" + Path.DirectorySeparatorChar) ||
                    path.Contains(Path.AltDirectorySeparatorChar + "Resources" + Path.AltDirectorySeparatorChar)))
                {
                    misplacedPrefabs.Add(path);
                }
            }
            if (misplacedPrefabs.Count > 0)
            {
                EditorGUILayout.HelpBox("Entity prefabs cannot be instantiated by the server unless " +
                    " they are located under a Resources folder.\n- " + string.Join("\n- ", misplacedPrefabs),
                    MessageType.Warning
                );
            }
        }

        /// <summary>
        /// Check that the <see cref="ksEntityComponent.ColliderData"/> is ordered to match the colliders on the game object.
        /// ColliderData is added for new colliders. ColliderData is removed for removed colliders.
        /// </summary>
        /// <param name="entity"><see cref="ksEntityComponent"/> to check the colliders on.</param>
        public static void CheckColliderData(SerializedObject soEntity)
        {
            if (soEntity == null || !(soEntity.targetObject is ksEntityComponent))
            {
                return;
            }
            SerializedProperty spColliderDataList = soEntity.FindProperty("ColliderData");
            ksEntityComponent entityComponent = soEntity.targetObject as ksEntityComponent;
            if (entityComponent == null || spColliderDataList == null)
            {
                return;
            }

            List<ksIUnityCollider> colliders = entityComponent.GetColliders(true);
            if (colliders == null)
            {
                return;
            }

            bool hadChanges = false;
            for (int i = 0; i < colliders.Count; ++i)
            {
                int spIndex = GetColliderDataIndex(colliders[i].Component, spColliderDataList);

                if (spIndex == -1)
                {
                    // Create a new property
                    spColliderDataList.InsertArrayElementAtIndex(i);
                    SerializedProperty sp = spColliderDataList.GetArrayElementAtIndex(i);

                    // When new property values are added the serialized array, the property has all fields initialized to default,
                    // which are different than the values assigned in the definition.  So we manually set those values whose
                    // assigned values are not equal to default to the expected values.;
                    sp.FindPropertyRelative("IsSimulation").boolValue = !colliders[i].IsTrigger;
                    sp.FindPropertyRelative("IsQuery").boolValue = true;
                    sp.FindPropertyRelative("Collider").objectReferenceValue = colliders[i].Component;
                    hadChanges = true;
                }
                else if (spIndex != i)
                {
                    // Move an existing property
                    spColliderDataList.MoveArrayElement(spIndex, i);
                    hadChanges = true;
                }
            }

            // Remove excess collider data
            if (spColliderDataList.arraySize > colliders.Count)
            {
                for (int i=colliders.Count; i < spColliderDataList.arraySize; ++i)
                {
                    spColliderDataList.DeleteArrayElementAtIndex(colliders.Count);
                    hadChanges = true;
                }
            }

            // Save changes
            if (hadChanges)
            {
                soEntity.ApplyModifiedProperties();
            }
        }

        /// <summary>
        /// Get the <see cref="ksColliderData"/> serialied property associated with a  Collider component
        /// that is tracked by a <see cref="ksEntityComponent"/> serialized object.
        /// </summary>
        /// <param name="collider">Collider component</param>
        /// <param name="soEntity">Serlized object of a <see cref="ksEntityComponent"/></param>
        /// <returns><see cref="ksColliderData"/> serialied property.</returns>
        public static SerializedProperty GetColliderDataProperty(Component collider, SerializedObject soEntity)
        {
            if (collider == null || soEntity == null || !(soEntity.targetObject is ksEntityComponent))
            {
                return null;
            }
            SerializedProperty spColliderDataList = soEntity.FindProperty("ColliderData");
            if (spColliderDataList == null)
            {
                return null;
            }
            int index = GetColliderDataIndex(collider, spColliderDataList);
            return (index >= 0) ? spColliderDataList.GetArrayElementAtIndex(index) : null;
        }

        /// <summary>
        /// Get the index for <see cref="ksColliderData"/> associated with a collider component
        /// from a serialized property list of <see cref="ksColliderData"/>.
        /// </summary>
        /// <param name="collider">Collider component</param>
        /// <param name="spColliderDataList"></param>
        /// <returns>
        ///     Index of the <see cref="ksColliderData"/> in the <paramref name="spColliderDataList"/>.
        ///     Returns -1 if nothing was found.
        /// </returns>
        public static int GetColliderDataIndex(Component collider, SerializedProperty spColliderDataList)
        {
            if (collider == null || spColliderDataList == null)
            {
                return -1;
            }
            for (int i = 0; i < spColliderDataList.arraySize; ++i)
            {
                SerializedProperty sp = spColliderDataList.GetArrayElementAtIndex(i);
                if (sp.FindPropertyRelative("Collider").objectReferenceValue == collider)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}