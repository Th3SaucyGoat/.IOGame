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

namespace KS.Reactor.Client.Unity.Editor
{
    /// <summary>Template for generating client scripts.</summary>
    public class ksClientScriptTemplate : ScriptableObject, ksIScriptTemplate
    {
        /// <summary>Script types that can be generated.</summary>
        public enum ScriptType
        {
            /// <summary>Create a <see cref="ksEntityScript"/></summary>
            ENTITY = 0,
            /// <summary>Create a <see cref="ksRoomScript"/></summary>
            ROOM = 1,
            /// <summary>Create a <see cref="ksPlayerScript"/></summary>
            PLAYER = 2,
            /// <summary>Create a <see cref="ksConnectScript"/></summary>
            CONNECT = 3
        }

        // Optional method IDs
        private const uint UPDATE = 0;
        private const uint PLAYER_JOIN_LEAVE = 1;
        private static readonly ksOptionalMethod[] OPTIONAL_METHODS = 
            new ksOptionalMethod[] { new ksOptionalMethod(UPDATE, "Update", true) };
        private static readonly ksOptionalMethod[] OPTIONAL_ROOM_METHODS = new ksOptionalMethod[]
        {
            new ksOptionalMethod(UPDATE, "Update", true),
            new ksOptionalMethod(PLAYER_JOIN_LEAVE, "OnPlayerJoin/Leave", false)
        };


        private ksScriptGenerator m_generator;

        [SerializeField]
        private ScriptType m_scriptType;

        [SerializeField]
        private GameObject[] m_gameObjects;

        /// <summary>Script generator</summary>
        public ksScriptGenerator Generator
        {
            get { return m_generator; }
            set { m_generator = value; }
        }

        /// <summary>Default file name for scripts generated from this template.</summary>
        public string DefaultFileName
        {
            get
            {
                switch (m_scriptType)
                {
                    case ScriptType.ENTITY:
                        return "ClientEntityScript";
                    case ScriptType.ROOM:
                        return "ClientRoomScript";
                    case ScriptType.PLAYER:
                        return "ClientPlayerScript";
                    case ScriptType.CONNECT:
                        return "ConnectScript";
                    default:
                        return "ClientScript";
                }
            }
        }

        ///<summary>Default path for scripts generated from this template.</summary>
        public string DefaultPath
        {
            get { return ksPaths.ClientScripts; }
        }

        /// <summary>Optional methods the template can generate.</summary>
        public ksOptionalMethod[] OptionalMethods
        {
            get { return m_scriptType == ScriptType.ROOM ? OPTIONAL_ROOM_METHODS : OPTIONAL_METHODS; }
        }

        /// <summary>Initialization</summary>
        /// <param name="scriptType">Script type to generate.</param>
        /// <param name="gameObjects">Game objects to attach the script to once generated.</param>
        /// <return>this</return>
        public ksClientScriptTemplate Initialize(ScriptType scriptType, GameObject[] gameObjects = null)
        {
            m_scriptType = scriptType;
            m_gameObjects = gameObjects;
            return this;
        }

        /// <summary>This function is called when the object becomes enabled and active.</summary>
        public void OnEnable()
        {
            hideFlags = HideFlags.HideAndDontSave;
        }

        /// <summary>Generates the contents of a script.</summary>
        /// <param name="path">Path the script will be written to.</param>
        /// <param name="className">Name of the generated class.</param>
        /// <param name="scriptNamespace">Namespace of the generated class.</param>
        /// <param name="optionalMethods">Contains ids of optional method stubs to generate.</param>
        /// <returns>Script contents</returns>
        public string Generate(string path, string className, string scriptNamespace, HashSet<uint> optionalMethods)
        {
            string baseClass;
            switch (m_scriptType)
            {
                case ScriptType.ENTITY:
                    baseClass = "ksEntityScript";
                    break;
                case ScriptType.ROOM:
                    baseClass = "ksRoomScript";
                    break;
                case ScriptType.PLAYER:
                    baseClass = "ksPlayerScript";
                    break;
                case ScriptType.CONNECT:
                    baseClass = "ksConnectScript";
                    break;
                default:
                    return "";
            }

            string template = @"using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using KS.Reactor.Client.Unity;
using KS.Reactor.Client;
using KS.Reactor;
";
            if (scriptNamespace != "")
            {
                template += Environment.NewLine + "namespace " + scriptNamespace + Environment.NewLine + "{";
            }

            string classBody = @"
public class " + className + " : " + baseClass + @"
{
    // Called after properties are initialized.
    public override void Initialize()
    {";
            if (optionalMethods.Contains(PLAYER_JOIN_LEAVE))
            {
                classBody += @"
        Room.OnPlayerJoin += PlayerJoin;
        Room.OnPlayerLeave += PlayerLeave;";
            }
            else
            {
                classBody += @"
        ";
            }
            classBody += @"
    }

    // Called when the script is detached.
    public override void Detached()
    {";
            if (optionalMethods.Contains(PLAYER_JOIN_LEAVE))
            {
                classBody += @"
        Room.OnPlayerJoin -= PlayerJoin;
        Room.OnPlayerLeave -= PlayerLeave;";
            }
            else
            {
                classBody += @"
        ";
            }
            classBody += @"
    }
";
            if (optionalMethods.Contains(UPDATE))
            {
                classBody += @"
    // Called every frame.
    private void Update()
    {
        
    }
";
            }
            if (optionalMethods.Contains(PLAYER_JOIN_LEAVE))
            {
                classBody += @"
    // Called when a player connects.
    private void PlayerJoin(ksPlayer player)
    {
        
    }

    // Called when a player disconnects.
    private void PlayerLeave(ksPlayer player)
    {
        
    }
";
            }
            if (scriptNamespace != "")
            {
                template += classBody.Replace("\n", "\n    ") + "}" + Environment.NewLine + "}";
            }
            else
            {
                template += classBody + "}";
            }

            return template;
        }

        /// <summary>Called after the script file is written.</summary>
        /// <param name="path">Path the script was written to.</param>
        /// <param name="className">Name of generated class.</param>
        /// <param name="scriptNamespace">Namespace of generated class.</param>
        public void HandleCreate(string path, string className, string scriptNamespace)
        {
            if (m_gameObjects != null)
            {
                m_generator.SaveAttachments(ksPaths.ClientScripts + className + ".cs", m_gameObjects);
            }
            AssetDatabase.Refresh();
        }
    }
}
