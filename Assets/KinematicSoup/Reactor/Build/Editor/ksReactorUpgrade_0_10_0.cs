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
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using KS.Unity.Editor;

namespace KS.Reactor.Client.Unity.Editor
{
    /// <summary>
    /// Contains all methods required to update earlier version of Reactor to 0.9.16 or above.
    /// </summary>
    public static class ksReactorUpgrade_0_10_0
    {
        // List of update messages
        private static List<string> m_changelog = new List<string>();

        /// <summary>
        /// Apply updates when upgrading to Reactor 0.10.0.
        /// </summary>
        public static void Upgrade()
        {
            UpdatePhysics();
            // Delete server runtime project and solution files to ensure they are regenerated.
            ksPathUtils.Delete(ksPaths.ServerRuntimeProject);
            ksPathUtils.Delete(ksPaths.ServerRuntimeSolution);
        }

        /// <summary>
        /// Converts <see cref="ksEntityPhysicsSettings.MaxAngularSpeed"/> from radians to degrees in all
        /// <see cref="ksEntityComponent"/>s and all <see cref="ksPhysicsSettings"/> in all prefabs and scenes.
        /// </summary>
        private static void UpdatePhysics()
        {
            // Process the prefabs
            foreach (ksBuildUtils.PrefabInfo prefabInfo in ksBuildUtils.IterateResourceAndAssetBundlePrefabs())
            {
                GameObject prefab = prefabInfo.GameObject;
                bool updated = false;

                // Update the prefab entities
                foreach (ksEntityComponent entity in prefab.GetComponentsInChildren<ksEntityComponent>())
                {
                    if (UpdateEntity(entity))
                    {
                        EditorUtility.SetDirty(entity);
                        m_changelog.Add("- Updated entity prefab " + AssetDatabase.GetAssetPath(prefab) + ": " +
                            ksBuildUtils.FullGameObjectName(entity.gameObject));
                        updated = true;
                    }
                }

                // Update the prefab physics settings
                foreach (ksPhysicsSettings settings in prefab.GetComponentsInChildren<ksPhysicsSettings>())
                {
                    if (UpdatePhysicsSettings(settings))
                    {
                        EditorUtility.SetDirty(settings);
                        m_changelog.Add("- Updated room type prefab " + AssetDatabase.GetAssetPath(prefab) + ": " +
                            ksBuildUtils.FullGameObjectName(settings.gameObject));
                        updated = true;
                    }
                }

                if (updated)
                {
                    // Save the changes.
                    PrefabUtility.SavePrefabAsset(prefab);
                }
            }

            // Process instance entities and physics settings.
            foreach (Component component in 
                ksBuildUtils.IterateSceneObjects(typeof(ksEntityComponent), typeof(ksPhysicsSettings)))
            {
                ksEntityComponent entity = component as ksEntityComponent;
                if (entity != null)
                {
                    if (UpdateEntity(entity))
                    {
                        EditorUtility.SetDirty(entity);
                        m_changelog.Add("- Updated entity instance " + SceneManager.GetActiveScene().name + ": " +
                            ksBuildUtils.FullGameObjectName(entity.gameObject));
                    }
                }
                else
                {
                    ksPhysicsSettings settings = component as ksPhysicsSettings;
                    if (settings != null && UpdatePhysicsSettings(settings))
                    {
                        EditorUtility.SetDirty(settings);
                        m_changelog.Add("- Updated room type instance " + SceneManager.GetActiveScene().name + ": " +
                            ksBuildUtils.FullGameObjectName(settings.gameObject));
                    }
                }
            }

            ksLog.Info("Reactor physics settings updates complete\n" + string.Join("\n", m_changelog));
        }

        /// <summary>
        /// Converts an entity's max angular speed from radians to degrees, and converts the old collider flags to the
        /// new format.
        /// </summary>
        /// <param name="entity">Entity to update.</param>
        /// <returns>True if the entity was modified.</returns>
        private static bool UpdateEntity(ksEntityComponent entity)
        {
            bool changed = false;
            SerializedObject serializedObj = null;
            if (PrefabUtility.GetCorrespondingObjectFromSource(entity) != null)
            {
                serializedObj = new SerializedObject(entity);
            }

            // Update collider flags.
            if (entity.ColliderData != null)
            {
                bool hasPrefabValue = false;
                SerializedProperty serializedProp = null;
                if (serializedObj != null)
                {
                    serializedProp = serializedObj.FindProperty("ColliderData");
                    hasPrefabValue = !serializedProp.prefabOverride;
                }
                // Do not modify if it uses the prefab value.
                if (!hasPrefabValue)
                {
                    for (int i = 0; i < entity.ColliderData.Count; i++)
                    {
                        if (serializedProp != null &&
                            !serializedProp.GetArrayElementAtIndex(i).FindPropertyRelative("Flag").prefabOverride)
                        {
                            // Do not modify if it uses the prefab value.
                            continue;
                        }
                        changed = true;
                        ksColliderData colliderData = entity.ColliderData[i];
                        // Disable obsolete warnings
#pragma warning disable CS0618
                        colliderData.IsQuery = colliderData.Flag != ksShape.ColliderFlags.SIMULATION;
                        colliderData.IsSimulation = colliderData.Flag != ksShape.ColliderFlags.SCENE_QUERY;
                        // Enable obsolete warnings
#pragma warning restore CS0618
                    }
                }
            }

            if (entity.PhysicsOverrides.MaxAngularSpeed <= 0)
            {
                return changed;
            }
            if (PrefabUtility.GetCorrespondingObjectFromSource(entity) != null)
            {
                SerializedProperty serializedProp = serializedObj.FindProperty("PhysicsOverrides.MaxAngularSpeed");
                if (!serializedProp.prefabOverride)
                {
                    // Do not modify if it uses the prefab value.
                    return changed;
                }
            }
            entity.PhysicsOverrides.MaxAngularSpeed *= ksMath.FRADIANS_TO_DEGREES;
            return true;
        }

        /// <summary>Converts a physics setting's max angular speed from radians to degrees.</summary>
        /// <param name="settings">Settings to update.</param>
        /// <returns>True if the entity was modified.</returns>
        private static bool UpdatePhysicsSettings(ksPhysicsSettings settings)
        {
            if (settings.DefaultEntityPhysics.MaxAngularSpeed <= 0)
            {
                return false;
            }
            if (PrefabUtility.GetCorrespondingObjectFromSource(settings) != null)
            {
                SerializedObject serializedObj = new SerializedObject(settings);
                SerializedProperty serializedProp = serializedObj.FindProperty("DefaultEntityPhysics.MaxAngularSpeed");
                if (!serializedProp.prefabOverride)
                {
                    // Do not modify if it uses the prefab value.
                    return false;
                }
            }
            settings.DefaultEntityPhysics.MaxAngularSpeed *= ksMath.FRADIANS_TO_DEGREES;
            return true;
        }
    }
}