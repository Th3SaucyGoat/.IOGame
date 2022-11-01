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
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace KS.Reactor.Client.Unity.Editor
{
    /// <summary>Template for generating player controllers.</summary>
    public class ksPlayerControllerTemplate : ScriptableObject, ksIScriptTemplate
    {
        /// <summary>Script generator</summary>
        private ksScriptGenerator m_generator;
        public ksScriptGenerator Generator
        {
            get { return m_generator; }
            set { m_generator = value; }
        }

        /// <summary>Default file name for scripts generated from this template.</summary>
        public string DefaultFileName
        {
            get { return "PlayerController"; }
        }

        /// <summary>Default path for scripts generated from this template.</summary>
        public string DefaultPath
        {
            get { return ksPaths.CommonScripts; }
        }

        /// <summary>Optional methods the template can generate.</summary>
        public ksOptionalMethod[] OptionalMethods
        {
            get { return null; }
        }

        /// <summary>Unity on enable</summary>
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
            ksServerProjectWatcher.Get().Ignore(path);
            string template = @"using System;
using System.Collections.Generic;
using System.Collections;
using KS.Reactor;
";
            if (scriptNamespace != "")
            {
                template += Environment.NewLine + "namespace " + scriptNamespace +  Environment.NewLine + "{";
            }

            string classBody = @"
public class " + className + @" : ksPlayerController
{
    // Unique non-zero identifier for this player controller class.
    public override uint Type
    {
        get { return %TYPE%; }
    }

    // Register all buttons and axes you will be using here.
    public override void RegisterInputs(ksInputRegistrar registrar)
    {
        
    }

    // Called after properties are initialized.
    public override void Initialize()
    {
        
    }

    // Called during the update cycle.
    public override void Update()
    {
        
    }
";
            if (scriptNamespace != "")
            {
                template += classBody.Replace("\n", "\n    ") + "}" + Environment.NewLine + "}";
            }
            else
            {
                template += classBody + "}";
            }

            uint type = 1;
            try
            {
                Assembly assembly = Assembly.Load("KSScripts-Common");
                ksPlayerControllerFactory factory = new ksPlayerControllerFactory();
                factory.RegisterFromAssembly(assembly);
                type = factory.NextUnusedType();
            }
            catch (FileNotFoundException)
            {
                // This happens when there are no common scripts.
            }
            catch (Exception e)
            {
                ksLog.Error(this, "Error loading player controller data from assembly 'KSScripts-Common'.", e);
            }
            template = template.Replace("%TYPE%", type.ToString());

            return template;
        }

        /// <summary>Called after the script file is written.</summary>
        /// <param name="path">path the script was written to.</param>
        /// <param name="className">Name of the generated class.</param>
        /// <param name="scriptNamespace">Namespace of the generated class.</param>
        public void HandleCreate(string path, string className, string scriptNamespace)
        {
            ksServerProjectUpdater.Instance.AddFileToProject(path);
            AssetDatabase.Refresh();
        }
    }
}
