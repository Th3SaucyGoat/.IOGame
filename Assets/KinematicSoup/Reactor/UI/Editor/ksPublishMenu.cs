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

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using KS.Unity.Editor;

namespace KS.Reactor.Client.Unity.Editor
{
    /// <summary>
    /// Reactor publishing GUI. Allows for project selection, image lists, and new/updated image publishing.
    /// </summary>
    public class ksPublishMenu : ksAuthenticatedMenu
    {
        //Layout Constants
        private const int COLUMN_M = 100;
        private const int COLUMN_L = 150;
        private const int LABEL_WIDTH = 75;
        private const int FIELD_WIDTH = 175;
        private const int LINE_HEIGHT = 18;
        private const int PADDING = 4;
        private const int COL_PADDING = 20;

        // GUI Layout
        private static GUILayoutOption m_mediumWidth = GUILayout.Width(COLUMN_M);
        private static GUILayoutOption m_largeWidth = GUILayout.Width(COLUMN_L);
        private static GUILayoutOption m_inputWidth = GUILayout.Width(LABEL_WIDTH + FIELD_WIDTH);
        private static GUILayoutOption m_buttonWidth = GUILayout.Width(FIELD_WIDTH);
        
        private ksPublishService m_service = null;
        private ksWindow m_window = null;
        private Vector2 m_scrollPosition = Vector2.zero;

        // Update UI
        private bool m_updateProjects = false;
        private bool m_updateImages = false;
        private bool m_updatePublish = false;

        //private int m_projectIndex = -1;
        private string m_imageName = "";
        private string m_imageVersion = "";

        private ksPublishService.ProjectInfo m_project = null;
        private List<ksPublishService.ProjectInfo> m_projects = new List<ksPublishService.ProjectInfo>();
        private List<ksPublishService.ImageInfo> m_GUIImages = new List<ksPublishService.ImageInfo>();

        private string m_layoutMessage = null;
        private MessageType m_layoutMessageType = MessageType.None;
        private string m_GUILayoutMessage = null;
        private MessageType m_GUILayoutMessageType = MessageType.None;
        private string m_publishMessage = null;
        private MessageType m_publishMessageType = MessageType.None;
        private string m_GUIPublishMessage = null;
        private MessageType m_GUIPublishMessageType = MessageType.None;
        private string m_imagesMessage = null;
        private MessageType m_imagesMessageType = MessageType.None;
        private string m_GUIImagesMessage = null;
        private MessageType m_GUIImagesMessageType = MessageType.None;

        [SerializeField]
        private bool m_publishOnEnable = false;

        /// <summary>Icon to display.</summary>
        public override Texture Icon
        {
            get { return ksTextures.Logo; }
        }

        /// <summary>Unity on enable.</summary>
        private void OnEnable()
        {
            SetLoginMenu(typeof(ksLoginMenu));
            hideFlags = HideFlags.HideAndDontSave;
            if (m_publishOnEnable)
            {
                // We're publishing now that the new proxy scripts were loaded.
                m_publishOnEnable = false;
                // We can't publish yet because Unity won't let us open a scene yet...
                EditorApplication.delayCall += Publish;
            }
        }

        /// <summary>Called when the menu is opened.</summary>
        /// <param name="window">GUI window</param>
        public override void OnOpen(ksWindow window)
        {
            m_window = window;
            m_service = ksPublishService.Get();
            m_service.OnUpdateProjects += OnUpdateProjects;
            m_service.OnSelectProject += OnSelectProject;
            m_service.OnUpdateImages += OnUpdateImages;
            m_service.OnPublish += OnPublish;
            m_service.Refresh();
        }

        /// <summary>Called when the menu is closed.</summary>
        /// <param name="window">GUI window</param>
        public override void OnClose(ksWindow window)
        {
            m_service.OnUpdateProjects -= OnUpdateProjects;
            m_service.OnSelectProject -= OnSelectProject;
            m_service.OnUpdateImages -= OnUpdateImages;
            m_service.OnPublish -= OnPublish;
        }

        /// <summary>Draw the GUI.</summary>
        /// <param name="window">GUI window</param>
        public override void OnDraw(ksWindow window)
        {
            // Check compiling
            if (EditorApplication.isCompiling)
            {
                EditorGUILayout.HelpBox("Compiling scripts...", MessageType.Warning);
                ClearStates();
                return;
            }

            // Update GUI
            CheckLayout();
            EditorGUILayout.Space();
            DrawProjectSelector(window);
            if (!string.IsNullOrEmpty(m_GUILayoutMessage))
            {
                EditorGUILayout.HelpBox(m_GUILayoutMessage, m_GUILayoutMessageType);
                return;
            }

            DrawPublishForm();
            DrawImageList();
        }

        /// <summary>Draws the email/logout and project selector.</summary>
        /// <param name="window">GUI window</param>
        private void DrawProjectSelector(ksWindow window)
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(ksEditorWebService.Email))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Logout"), false, Logout, null);
                menu.ShowAsContext();
            }

            if (m_project != null)
            {
                if (GUILayout.Button(m_project.CompanyName + '/' + m_project.Name))
                {
                    GenericMenu menu = new GenericMenu();
#if UNITY_2019_3_OR_NEWER
                    menu.allowDuplicateNames = true;
#endif
                    foreach (ksPublishService.ProjectInfo project in m_projects)
                    {
                        menu.AddItem(
                            new GUIContent(project.CompanyName + '/' + project.Name),
                            false,
                            SelectProject,
                            project);
                    }
                    menu.ShowAsContext();
                }
            }
            else
            {
                GUILayout.Button("--");
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>Logout of the service and reset the menu.</summary>
        /// <param name="state">Generic menu item state.</param>
        private void Logout(object state)
        {
            ksEditorWebService.Logout(ksReactorConfig.Instance.Urls.Publishing + "/v3/logout");
            m_window.Repaint();
        }

        /// <summary>Select a project from the project list.</summary>
        /// <param name="state">selected <see cref="ksPublishService.ProjectInfo"/></param>
        private void SelectProject(object state)
        {
            m_project = state as ksPublishService.ProjectInfo;
            m_service.SelectedProjectId = m_project.Id;
        }

        /// <summary>Publishes the project.</summary>
        private void Publish()
        {
            ksConfigWriter configWriter = new ksConfigWriter();
            if (ksServerProjectWatcher.Get().ServerProjectDirty)
            {
                // Build the local DLL to regenerate the proxy scripts.
                if (!configWriter.Build(false, true))
                {
                    return;
                }
                ksServerProjectWatcher.Get().ServerProjectDirty = false;
                // If Unity is compiling, that means proxy scripts changed and we have to wait for Unity to finish
                // compiling before we can continue publishing.
                if (EditorApplication.isCompiling)
                {
                    m_publishOnEnable = true;
                    return;
                }
            }
            if (configWriter.Build(true, true, false, true))
            {
                ksLog.Info("Build complete." + configWriter.Summary);

                // If the scene summary contain no scenes and/or rooms, then stop publshing.
                if (configWriter.SceneSummary.Count == 0)
                {
                    OnPublish(null, "A Reactor project must contain at least one scene with a gameobject that has a ksRoomType component.");
                }
                else
                {
                    if (m_service.PublishImage(m_imageName, m_imageVersion, configWriter.SceneSummary))
                    {
                        m_publishMessage = "Publishing...";
                        m_publishMessageType = MessageType.Info;
                        m_updatePublish = true;
                    }
                }
            }
        }

        /// <summary>Draw publishing input form.</summary>
        private void DrawPublishForm()
        {
            EditorGUIUtility.labelWidth = LABEL_WIDTH;
            EditorGUILayout.LabelField("Publish Image", EditorStyles.boldLabel);
            m_imageName = EditorGUILayout.TextField("Image", m_imageName, m_inputWidth);
            m_imageVersion = EditorGUILayout.TextField("Version", m_imageVersion, m_inputWidth);

            GUILayout.BeginHorizontal();
            GUILayout.Space(LABEL_WIDTH + PADDING);
            EditorGUI.BeginDisabledGroup(!m_service.CanPublish);
            if (GUILayout.Button("Publish", m_buttonWidth))
            {
                Publish();
            }
            GUILayout.EndHorizontal();
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.Space();

            if (!string.IsNullOrEmpty(m_GUIPublishMessage))
            {
                EditorGUILayout.HelpBox(m_GUIPublishMessage, m_GUIPublishMessageType);
                EditorGUILayout.Space();
            }
        }

        /// <summary>Draw the list of images.</summary>
        private void DrawImageList()
        {
            m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition);
            // List Header
            Rect area = EditorGUILayout.BeginHorizontal();
            area.width = COLUMN_L;
            EditorGUILayout.LabelField("Image", EditorStyles.boldLabel, m_largeWidth);
            if (GUI.Button(area, GUIContent.none, GUI.skin.label))
            {
                m_service.SetSorting(ksPublishService.SortKey.NAME, ksPublishService.SortKey.NAME_REVERSE);
            }
            area.x += COL_PADDING + area.width;
            area.width = COLUMN_M;
            GUILayout.Space(COL_PADDING);
            EditorGUILayout.LabelField("Version", EditorStyles.boldLabel, m_mediumWidth);
            if (GUI.Button(area, GUIContent.none, GUI.skin.label))
            {
                m_service.SetSorting(ksPublishService.SortKey.VERSION, ksPublishService.SortKey.VERSION_REVERSE);
            }
            area.x += COL_PADDING + area.width;
            area.width = COLUMN_L;
            GUILayout.Space(COL_PADDING);
            EditorGUILayout.LabelField("Timestamp", EditorStyles.boldLabel, m_largeWidth);
            if (GUI.Button(area, GUIContent.none, GUI.skin.label))
            {
                m_service.SetSorting(ksPublishService.SortKey.NEWEST, ksPublishService.SortKey.OLDEST);
            }
            area.x += COL_PADDING + area.width;
            area.width = COLUMN_M;
            GUILayout.Space(COL_PADDING);
            EditorGUILayout.LabelField("Publisher", EditorStyles.boldLabel, m_mediumWidth);
            if (GUI.Button(area, GUIContent.none, GUI.skin.label))
            {
                m_service.SetSorting(ksPublishService.SortKey.PUBLISHER, ksPublishService.SortKey.PUBLISHER_REVERSE);
            }
            GUILayout.Space(COL_PADDING);
            EditorGUILayout.LabelField("Action", EditorStyles.boldLabel, m_mediumWidth);
            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(m_GUIImagesMessage))
            {
                EditorGUILayout.HelpBox(m_GUIImagesMessage, m_GUIImagesMessageType);
            }
            else
            {
                // Image List
                if (m_GUIImages != null && m_GUIImages.Count > 0)
                {
                    for (int i = 0; i < m_GUIImages.Count; i++)
                    {
                        ksPublishService.ImageInfo image = m_GUIImages[i];
                        Rect row = EditorGUILayout.BeginHorizontal(GUILayout.Height(LINE_HEIGHT));
                        Rect expandArea = row;
                        expandArea.width = 2 * COLUMN_M + 2 * COLUMN_L + PADDING + 3 * COL_PADDING;
                        if (GUI.Button(expandArea, GUIContent.none, GUI.skin.label))
                        {
                            GUI.FocusControl("");
                            m_imageName = image.Name;
                            m_imageVersion = image.Version;
                        }
                        GUIStyle style = new GUIStyle(GUI.skin.label);
                        if (m_service.SelectedProjectId > 0 && ksReactorConfig.Instance.Build.ImageBinding ==
                            m_service.SelectedProject.CompanyId + "." + m_service.SelectedProjectId + "." + image.Id)
                        {
                            style.normal.textColor = EditorGUIUtility.isProSkin ? 
                                new Color(.9f, .6f, .2f) : new Color(.1f, .4f, .8f);
                        }
                        if (i % 2 == 1)
                        {
                            EditorGUI.DrawRect(row, new Color32(0, 0, 0, 16));
                        }
                        GUILayout.Space(PADDING);
                        EditorGUILayout.LabelField(image.Name, style, m_largeWidth);
                        GUILayout.Space(COL_PADDING);
                        EditorGUILayout.LabelField(image.Version, style, m_mediumWidth);
                        GUILayout.Space(COL_PADDING);
                        EditorGUILayout.LabelField(image.Time, style, m_largeWidth);
                        GUILayout.Space(COL_PADDING);
                        EditorGUILayout.LabelField(image.Publisher, style,  m_mediumWidth);
                        GUILayout.Space(COL_PADDING);

                        if (EditorGUILayout.DropdownButton(new GUIContent("Action"), FocusType.Keyboard, m_mediumWidth, GUILayout.Height(16)))
                        {
                            GenericMenu menu = new GenericMenu();
                            menu.AddItem(new GUIContent("Bind"), false, BindImage, image);
                            menu.ShowAsContext();
                        }

                        EditorGUILayout.EndHorizontal();
                    }
                }
            }
            EditorGUILayout.EndScrollView();
        }

        /// <summary>Set the image binding for this project.</summary>
        /// <param name="value">Published image data of type <see cref="ksJSON"/>.</param>
        private void BindImage(object value)
        {
            ksPublishService.ImageInfo image = (ksPublishService.ImageInfo)value;
            if (m_service.SelectedProjectId > 0)
            {
                ksReactorConfig.Instance.Build.ImageBinding =
                    m_service.SelectedProject.CompanyId + "." + m_service.SelectedProjectId + "." + image.Id;
                EditorUtility.SetDirty(ksReactorConfig.Instance);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        /// <summary>Handle project list updates.</summary>
        /// <param name="error">Error message</param>
        private void OnUpdateProjects(string error)
        {
            if (!string.IsNullOrEmpty(error))
            {
                m_layoutMessage = error;
                m_layoutMessageType = MessageType.Error;
            }
            else
            {
                m_layoutMessage = null;
                m_layoutMessageType = MessageType.None;
            }
            m_updateProjects = true;
            m_window.Repaint();
        }

        /// <summary>Handle project selection changes.</summary>
        /// <param name="selectedProjectId">Selected project ID.</param>
        /// <param name="error">Error message</param>
        private void OnSelectProject(int selectedProjectId, string error)
        {
            if (!string.IsNullOrEmpty(error))
            {
                m_layoutMessage = error;
                m_layoutMessageType = MessageType.Error;
            }
            else
            {
                m_layoutMessage = null;
                m_layoutMessageType = MessageType.None;
                m_project = null;
                foreach (ksPublishService.ProjectInfo project in m_projects)
                {
                    if (project.Id == m_service.SelectedProjectId)
                    {
                        m_project = project;
                    }
                }
            }
            m_updateProjects = true;
            m_window.Repaint();
        }

        /// <summary>Handle image list updates.</summary>
        /// <param name="error">Error message</param>
        private void OnUpdateImages(string error)
        {
            if (!string.IsNullOrEmpty(error))
            {
                m_imagesMessage = error;
                m_imagesMessageType = MessageType.Error;
            }
            else
            {
                m_imagesMessage = null;
                m_imagesMessageType = MessageType.None;
            }
            m_updateImages = true;
            m_window.Repaint();
        }

        /// <summary>Handle image is publish events.</summary>
        /// <param name="id">Image ID</param>
        /// <param name="error">Error message</param>
        private void OnPublish(string id, string error)
        {
            if (!string.IsNullOrEmpty(error))
            {
                m_publishMessage = error;
                m_publishMessageType = MessageType.Error;
            }
            else
            {
                m_publishMessage = null;
                m_publishMessageType = MessageType.None;

                // Set the image binding for this project
                if (m_service.SelectedProjectId > 0)
                {
                    ksReactorConfig.Instance.Build.ImageBinding =
                        m_service.SelectedProject.CompanyId + "." + m_service.SelectedProjectId + "." + id;
                    EditorUtility.SetDirty(ksReactorConfig.Instance);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
            }
            m_updatePublish = true;
            m_window.Repaint();
        }

        /// <summary>Check and apply updates that affect the GUI layout.</summary>
        private void CheckLayout()
        {
            if (Event.current.type == EventType.Layout)
            {
                List<ksPublishService.ProjectInfo> projects = m_service.Projects;

                // Update layout message
                if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    m_GUILayoutMessage = "Cannot publish images while the editor is in play mode.";
                    m_GUILayoutMessageType = MessageType.Warning;
                }
                else if (projects == null)
                {
                    m_GUILayoutMessage = "Fetching projects...";
                    m_GUILayoutMessageType = MessageType.Info;
                }
                else if (m_layoutMessage != null)
                {
                    m_GUILayoutMessage = m_layoutMessage;
                    m_GUILayoutMessageType = m_layoutMessageType;
                }
                else if (projects.Count == 0)
                {
                    m_GUILayoutMessage = "You are not an editor of any project. You must be added to a project " +
                            "before you can publish images.";
                    m_GUILayoutMessageType = MessageType.Warning;
                }
                else
                {
                    m_GUILayoutMessage = null;
                    m_GUILayoutMessageType = MessageType.None;
                }

                // Update project GUI data
                if (m_updateProjects)
                {
                    m_updateProjects = false;
                    m_projects = projects;
                    m_project = null;
                    foreach (ksPublishService.ProjectInfo project in m_projects)
                    {
                        if (project.Id == m_service.SelectedProjectId)
                        {
                            m_project = project;
                        }
                    }
                }

                // Update image list GUI data
                if (m_updateImages)
                {
                    m_updateImages = false;
                    m_GUIImagesMessage = m_imagesMessage;
                    m_GUIImagesMessageType = m_imagesMessageType;
                    m_GUIImages = m_service.Images;
                }

                // Update publish messages
                if (m_updatePublish)
                {
                    m_updatePublish = false;
                    m_GUIPublishMessage = m_publishMessage;
                    m_GUIPublishMessageType = m_publishMessageType;
                }
            }
        }

        /// <summary>Clear message and fetch states.</summary>
        private void ClearStates()
        {
            m_imagesMessage = null;
            m_imagesMessageType = MessageType.None;
            m_publishMessage = null;
            m_publishMessageType = MessageType.None;
            m_layoutMessage = null;
            m_layoutMessageType = MessageType.None;
        }
    }
}
