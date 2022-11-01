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
using System.Reflection;
using UnityEngine;
using UnityEditor;
using KS.Unity.Editor;

namespace KS.Reactor.Client.Unity.Editor
{
    /// <summary>Load icons and associates them with Reactor scripts.</summary>
    public class ksIconManager
    {
        private const string ENTITY = "KSEntity";
        private const string ROOM_TYPE = "KSRoomType";
        private const string PREDICTOR = "KSPredictor";
        private const string CONE_COLLIDER = "ConeCollider";
        private const string CYLINDER_COLLIDER = "CylinderCollider";
        private const string CLIENT_ENTITY_SCRIPT = "KSClientEntityScript";
        private const string CLIENT_ROOM_SCRIPT = "KSClientRoomScript";
        private const string CLIENT_PLAYER_SCRIPT = "KSClientPlayerScript";
        private const string SERVER_ENTITY_SCRIPT = "KSServerEntityScript";
        private const string SERVER_ROOM_SCRIPT = "KSServerRoomScript";
        private const string SERVER_PLAYER_SCRIPT = "KSServerPlayerScript";
        private const string SCRIPT_ASSET = "KSScriptAsset";
        private const string COLLISION_FILTER = "KSCollisionFilter";
        private const string PLAYER_CONTROLLER = "KSPlayerController";

        /// <summary>Iterates all Reactor scripts using reflection and sets their icons.</summary>
        public void SetScriptIcons()
        {
            if (EditorApplication.isPlaying)
            {
                // We'll get a errors doing this in play mode, because we need to attach a script to game object to set
                // it's icon, and in play mode the script's awake method will be called which can cause unwanted side
                // effects.
                return;
            }
            Assembly gameAssembly;
            try
            {
                gameAssembly = Assembly.Load("Assembly-CSharp");
            }
            catch (Exception)
            {
                // Don't log anything. This happens when new projects are created and Unity hasn't generated the assembly yet.
                return;
            }
            ksIconUtility util = new ksIconUtility();

            SetIcon<ksEntityScript>(util, gameAssembly, CLIENT_ENTITY_SCRIPT);
            SetIcon<ksRoomScript>(util, gameAssembly, CLIENT_ROOM_SCRIPT);
            SetIcon<ksPlayerScript>(util, gameAssembly, CLIENT_PLAYER_SCRIPT);
            SetIcon<ksProxyEntityScript>(util, gameAssembly, SERVER_ENTITY_SCRIPT);
            SetIcon<ksProxyRoomScript>(util, gameAssembly, SERVER_ROOM_SCRIPT);
            SetIcon<ksProxyPlayerScript>(util, gameAssembly, SERVER_PLAYER_SCRIPT);
            SetIcon<ksProxyScriptAsset>(util, gameAssembly, SCRIPT_ASSET);
            SetIcon<ksPlayerControllerAsset>(util, gameAssembly, PLAYER_CONTROLLER);
            SetIcon<ksPredictor>(util, gameAssembly, PREDICTOR);

            util.SetIcon<ksEntityComponent>(LoadIcon(ENTITY));
            util.SetIcon<ksRoomType>(LoadIcon(ROOM_TYPE));
            util.SetIcon<ksPhysicsSettings>(LoadIcon(ROOM_TYPE));
            util.SetIcon<ksConeCollider>(LoadIcon(CONE_COLLIDER));
            util.SetIcon<ksCylinderCollider>(LoadIcon(CYLINDER_COLLIDER));
            util.SetIcon<ksCollisionFilterAsset>(LoadIcon(COLLISION_FILTER));
            util.SetIcon<ksPlayerControllerAsset>(LoadIcon(PLAYER_CONTROLLER));
            util.SetIcon<ksNewClientEntityScript>(LoadIcon(CLIENT_ENTITY_SCRIPT));
            util.SetIcon<ksNewClientRoomScript>(LoadIcon(CLIENT_ROOM_SCRIPT));
            util.SetIcon<ksNewClientPlayerScript>(LoadIcon(CLIENT_PLAYER_SCRIPT));
            util.SetIcon<ksNewServerEntityScript>(LoadIcon(SERVER_ENTITY_SCRIPT));
            util.SetIcon<ksNewServerRoomScript>(LoadIcon(SERVER_ROOM_SCRIPT));
            util.SetIcon<ksNewServerPlayerScript>(LoadIcon(SERVER_PLAYER_SCRIPT));
            util.SetIcon<ksNewPredictor>(LoadIcon(PREDICTOR));

            util.CleanUp();
        }

        /// <summary>Sets the icon for all scripts that inherit from Type <typeparamref name="ScriptType"/>.</summary>
        /// <typeparam name="ScriptType"></typeparam>
        /// <param name="util"></param>
        /// <param name="gameAssembly">Game assembly to search for scripts in via reflection.</param>
        /// <param name="icon">Icon name</param>
        private void SetIcon<ScriptType>(ksIconUtility util, Assembly gameAssembly, string icon)
        {
            Texture2D texture = LoadIcon(icon);
            if (texture == null)
            {
                return;
            }
            IEnumerable<Type> types = gameAssembly.GetTypes().Where(type => typeof(ScriptType).IsAssignableFrom(type));
            foreach (Type type in types)
            {
                util.SetIcon(type, texture);
            }
        }

        /// <summary>Loads an icon as a <see cref="Texture2D"/>.</summary>
        /// <param name="icon">Icon name.</param>
        /// <returns>2D Texture</returns>
        private Texture2D LoadIcon(string icon)
        {
            string path = ksPaths.Textures + icon + ".png";
            Texture2D texture = (Texture2D)AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D));
            if (texture == null)
            {
                ksLog.Error(this, "Unable to load icon at " + path);
            }
            return texture;
        }
    }
}
