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

using UnityEngine;

namespace KS.Reactor.Client.Unity
{
    /// <summary>
    /// Factory for creating game objects for entities. Use this class to implement your own entity factory
    /// and assign it to <see cref="ksRoom.EntityFactory"/>.
    /// </summary>
    public class ksEntityFactory
    {
        /// <summary>
        /// Gets the game object for an entity. The returned game object must have a <see cref="ksEntityComponent"/> 
        /// and must not be assigned to another non-destroyed entity.
        /// </summary>
        /// <param name="assetId">
        /// Identifies the prefab object for the entity. If a prefab contains multiple child objects with entity 
        /// components, each child object will have a different asset ID. With asset ID and entity type you can 
        /// retrieve an entity prefab using <see cref="ksReactor.PrefabCache"/>.
        /// </param>
        /// <param name="entityType">Entity type</param>
        /// <param name="prefab">Prefab associated with the asset Id.</param>
        /// <returns>Game object representing the Entity.</returns>
        public virtual GameObject GetGameObject(uint assetId, string entityType, GameObject prefab)
        {
            GameObject gameObject = GameObject.Instantiate<GameObject>(prefab);
            gameObject.name = prefab.name;
            return gameObject;
        }

        /// <summary>
        /// Called when an entity is constructed.
        /// Override this to implement your own initialization logic.
        /// </summary>
        /// <param name="entity">Entity to initialize.</param>
        public virtual void InitializeEntity(ksEntity entity)
        {

        }

        /// <summary>
        /// Called when an entity is destroyed and <see cref="ksEntity.DestroyWithServer"/> is true.
        /// Override this to implement your own destruction logic.
        /// </summary>
        /// <param name="entity">Entity to release the game object from.</param>
        public virtual void ReleaseGameObject(ksEntity entity)
        {
            GameObject.Destroy(entity.GameObject);
        }
    }
}
