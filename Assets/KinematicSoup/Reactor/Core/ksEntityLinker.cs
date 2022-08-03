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

namespace KS.Reactor.Client.Unity
{
    /// <summary>
    /// Maps entity IDs to game objects that existed in the scene before joining a room.
    /// </summary>
    public class ksEntityLinker
    {
        private Scene m_scene;
        private List<ksEntityComponent> m_permanentObjects = new List<ksEntityComponent>();
        private Dictionary<uint, GameObject> m_map = new Dictionary<uint, GameObject>();
        private GameObject m_container;

        /// <summary>Constructor</summary>
        /// <param name="scene">Unity scene this entity linker tracks objects for.</param>
        public ksEntityLinker(Scene scene)
        {
            m_scene = scene;
        }

        /// <summary>
        /// Creates a map of entity IDs to game objects with <see cref="ksEntityComponent"/> components.
        /// All entities are then added to a game object that tracks inactive entities in a scene.
        /// </summary>
        public void Initialize()
        {
            if (m_container != null || !m_scene.isLoaded)
            {
                return;
            }
            m_container = new GameObject("Inactive Reactor Entities");
            SceneManager.MoveGameObjectToScene(m_container, m_scene);
            foreach (ksEntityComponent entityComponent in UnityEngine.Object.FindObjectsOfType<ksEntityComponent>())
            {
                if (entityComponent.EntityId != 0)
                {
                    if (entityComponent.IsPermanent)
                    {
                        entityComponent.enabled = false;
                        m_permanentObjects.Add(entityComponent);
                        continue;
                    }
                    m_map[entityComponent.EntityId] = entityComponent.gameObject;
                }
                entityComponent.transform.SetParent(m_container.transform);
            }
            m_container.SetActive(false);
        }

        /// <summary>
        /// Get a list of entities that have been marked permanent. Permanent entities exist on both the client and
        /// server and will not move. These entities will never receive transform updates from the server.
        /// </summary>
        /// <returns>
        /// List of <see cref="ksEntityComponent"/> components whose <see cref="ksEntityComponent.IsPermanent"/>
        /// proeprty is true.
        /// </returns>
        public List<ksEntityComponent> GetPermanentObjects()
        {
            return m_permanentObjects;
        }

        /// <summary>Gets the game object with the given entity ID and removes it from the map.</summary>
        /// <param name="entityId">Entity ID</param>
        /// <returns>GameObject for the entity, or null if none was found.</returns>
        public GameObject GetGameObjectForEntity(uint entityId)
        {
            GameObject gameObject;
            if (m_map.TryGetValue(entityId, out gameObject))
            {
                m_map.Remove(entityId);
            }
            return gameObject;
        }

        /// <summary>Checks if a game object is a child of the linker's container object.</summary>
        /// <param name="gameObject"></param>
        /// <returns>True if the game object is a child of the container object.</returns>
        public bool Contains(GameObject gameObject)
        {
            return m_container != null && gameObject.transform.parent == m_container.transform;
        }
    }
}
