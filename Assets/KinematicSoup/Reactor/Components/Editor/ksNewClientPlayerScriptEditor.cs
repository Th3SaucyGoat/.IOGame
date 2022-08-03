﻿/*
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
using UnityEditor;

namespace KS.Reactor.Client.Unity.Editor
{
    /// <summary>
    /// Custom editor that opens a new client player script dialog and deletes the target scripts.
    /// </summary>
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ksNewClientPlayerScript))]
    public class ksNewClientPlayerScriptEditor : UnityEditor.Editor
    {
        /// <summary>
        /// Opens a new client player script dialog and deletes the target scripts.
        /// </summary>
        public void OnEnable()
        {
            ksNewScriptMenu.Open(ScriptableObject.CreateInstance<ksClientScriptTemplate>().Initialize(
                    ksClientScriptTemplate.ScriptType.PLAYER, Selection.gameObjects));
            foreach (ksNewClientPlayerScript script in targets)
            {
                DestroyImmediate(script, true);
            }
        }
    }
}
