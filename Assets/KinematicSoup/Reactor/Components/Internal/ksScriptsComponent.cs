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
    /// Component that manages ksMonoScripts. <see cref="ksMonoScript{Parent, Script}"/> will look for a 
    /// component when initialized.
    /// </summary>
    /// <typeparam name="Parent">The type the scripts are attached to.</typeparam>
    /// <typeparam name="Script">The script type.</typeparam>
    public class ksScriptsComponent<Parent, Script> : MonoBehaviour
        where Parent : class
        where Script : ksMonoScript<Parent, Script>
    {
        /// <summary>Parent the scripts are attached to.</summary>
        public Parent ParentObject
        {
            get { return m_object; }
            set { m_object = value; }
        }
        private Parent m_object;

        /// <summary>List of attached scripts.</summary>
        public ksLinkedList<Script> Scripts
        {
            get 
            {
                if (m_scripts == null)
                {
                    m_scripts = new ksLinkedList<Script>();
                    foreach (Script script in GetComponents<Script>())
                    {
                        if (((ksMonoScript<Parent, Script>.IInternals)script).AddToScriptList)
                        {
                            ((ksMonoScript<Parent, Script>.IInternals)script).Component = this;
                            m_scripts.Add(script);
                        }
                    }
                }
                return m_scripts;
            }
        }
        private ksLinkedList<Script> m_scripts;

        /// <summary>
        /// Are the scripts initialized? If true, new scripts will be initialized when they are attached.
        /// </summary>
        public bool IsInitialized
        {
            get { return m_initialized; }
        }
        private bool m_initialized = false;

        /// <summary>Initializes scripts.</summary>
        public void InitializeScripts()
        {
            if (!m_initialized)
            {
                m_initialized = true;
                foreach (Script script in Scripts)
                {
                    ksRPCManager<ksRPCAttribute>.Instance.RegisterTypeRPCs(script.InstanceType);
                    script.Initialize();
                    script.enabled = true;
                }
            }
        }

        /// <summary>Resets the component to its uninitialized state.</summary>
        public void CleanUp()
        {
            m_object = null;
            m_scripts = null;
            m_initialized = false;
        }
    }
}
