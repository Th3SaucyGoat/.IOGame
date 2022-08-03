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

using UnityEditor;

namespace KS.Reactor.Client.Unity.Editor
{
    /// <summary>Inspector editor for <see cref="ksEntityScript"/>.</summary>
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ksEntityScript), true)]
    public class ksEntityScriptEditor : ksMonoScriptEditor<ksEntity, ksEntityScript>
    {
        /// <summary>
        /// Displays a warning if a ksEntityComponent is not found on the selected game object.
        /// </summary>
        protected override void Validate()
        {
            CheckComponent<ksEntityComponent>();
        }
    }
}
