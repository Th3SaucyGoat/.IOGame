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

using KS.Unity.Editor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace KS.Reactor.Client.Unity.Editor
{
    /// <summary>Menu for displaying local server logs.</summary>
    public class ksServerLogMenu : ScriptableObject, ksIMenu
    {
        private const float BUTTON_WIDTH = 100f;
        private const float BUTTON_HEIGHT = 16f;

        // EditorPref keys
        private const string SHOW_TYPE_KEY = "com.kinematicsoup.log_type";
        private const string SHOW_DATE_KEY = "com.kinematicsoup.log_date";
        private const string SHOW_TIME_KEY = "com.kinematicsoup.log_time";

        // Refresh time in seconds
        private readonly TimeSpan FILE_REFRESH_DELAY = new TimeSpan(0, 0, 5);
        private DateTime m_fileRefeshTime;

        /// <summary>Log file and process information.</summary>
        [Serializable]
        private class Log : IComparable<Log>
        {
            /// <summary>File</summary>
            public string File;
            /// <summary>Datetime</summary>
            public DateTime Datetime;
            /// <summary>Server</summary>
            public ksLocalServer Server;

            /// <summary>Sort first by running servers then by file creation time.</summary>
            /// <param name="other">Log to compare.</param>
            /// <returns>sorting value. <see cref="IComparable"/></returns>
            public int CompareTo(Log other)
            {
                if (Server != null && Server.IsRunning && (other.Server == null || !other.Server.IsRunning))
                {
                    return -1;
                }
                if (other.Server != null && other.Server.IsRunning && (Server == null || !Server.IsRunning))
                {
                    return 1;
                }

                return -Datetime.CompareTo(other.Datetime);
            }
        }

        /// <summary>Log Message.</summary>
        private class LogMessage
        {
            // Parse log messages into date, time, type, and message parts
            private static Regex regex = 
                new Regex("(^\\d{4}-\\d{2}-\\d{2}) (\\d{2}:\\d{2}:\\d{2}\\.\\d{4}) (\\[[^\\[\\]]+\\]) (.*$)");

            /// <summary>Level</summary>
            public ksLog.Level Level;
            /// <summary>Date</summary>
            public string Date = "";
            /// <summary>Time</summary>
            public string Time = "";
            /// <summary>Type</summary>
            public string Type = "";
            /// <summary>Message</summary>
            public string Message = "";

            /// <summary>Create a new log message and split the message into parts.</summary>
            /// <param name="message">Log message</param>
            public LogMessage(string message)
            {
                Match match = regex.Match(message);
                if (match.Success)
                {
                    Date = match.Groups[1].Value;
                    Time = match.Groups[2].Value;
                    Type = match.Groups[3].Value;
                    Message = match.Groups[4].Value;
                }
                else
                {
                    Message = message;
                }
            }

            /// <summary>Get the displayed message string based on visible parts.</summary>
            /// <param name="showDate">Show the message date.</param>
            /// <param name="showTime">Show the message time.</param>
            /// <param name="showType">Show the message type.</param>
            /// <returns>Displayed message</returns>
            public string GetDisplayMessage(bool showDate = true, bool showTime = true, bool showType = true)
            {
                string displayMessage = Message;
                if (showType) displayMessage = Type + " " + displayMessage;
                if (showTime) displayMessage = Time + " " + displayMessage;
                if (showDate) displayMessage = Date + " " + displayMessage;
                return displayMessage;
            }
        }

        /// <summary>Track UI selection of <see cref="LogMessage"/>s.</summary>
        private struct LogSelection
        {
            public int MessageIndex;
            public int StringIndex;

            /// <summary>Construct a new selection.</summary>
            /// <param name="messageIndex">Index of the message in the log message list.</param>
            /// <param name="stringIndex">Character index in the message string.</param>
            public LogSelection(int messageIndex, int stringIndex)
            {
                MessageIndex = messageIndex;
                StringIndex = stringIndex;
            }
        }

        // Serialized state
        [SerializeField]
        private List<Log> m_logs = new List<Log>();
        [SerializeField]
        private Log m_currentLog = null;
        [SerializeField]
        private List<string> m_GUILogList = new List<string>();
        [SerializeField]
        private int m_GUILogIndex = 0;

        // Initialization
        [NonSerialized]
        private bool m_initialized = false;
        private Dictionary<ksLog.Level, GUIStyle> m_styles = new Dictionary<ksLog.Level, GUIStyle>();

        // Selection
        [NonSerialized]
        private bool m_selecting = false;
        private LogSelection m_selectionStart = new LogSelection(-1, -1);
        private LogSelection m_selectionEnd = new LogSelection(-1, -1);
        
        // Log Messages
        private List<LogMessage> m_logMessages = new List<LogMessage>();
        private FileStream m_fileStream = null;
        private StreamReader m_fileReader = null;
        private ksLog.Level m_lastLogLevel = ksLog.Level.INFO;
        [NonSerialized]
        private long m_logFileSize = 0;

        // Display flags
        private bool m_showDate = true;
        private bool m_showTime = true;
        private bool m_showType = true;

        // GUI control
        private ksWindow m_window = null;
        private Vector3 m_scrollValue = Vector2.zero;
        private bool m_autoScroll = false;
        private Rect m_vScrollRect;
        private Rect m_hScrollRect;
        private Rect m_consoleRect;
        private float m_consoleWidth = 0.0f;

        /// <summary>Icon to display.</summary>
        public Texture Icon
        {
            get { return ksTextures.Logo; }
        }

        /// <summary>Unity on enable.</summary>
        private void OnEnable()
        {
            hideFlags = HideFlags.HideAndDontSave;
        }

        /// <summary>Destroy the serialized object when closing the menu.</summary>
        public bool DestroyOnClose
        {
            get { return true; }
        }

        /// <summary>Called when the menu is opened.</summary>
        /// <param name="window">GUI window</param>
        public void OnOpen(ksWindow window)
        {
            m_window = window;
            m_fileRefeshTime = DateTime.Now;
            EditorApplication.update += Update;
        }
        
        /// <summary>Called when the menu is closed.</summary>
        /// <param name="window">GUI window</param>
        public void OnClose(ksWindow window)
        {
            EditorApplication.update -= Update;
            CloseLog();
            m_window = null;
        }

        /// <summary>Initialize GUI styles and log file list.</summary>
        public void Initialize()
        {
            UpdateFileList();

            m_showType = EditorPrefs.GetBool(SHOW_TYPE_KEY, true);
            m_showDate = EditorPrefs.GetBool(SHOW_DATE_KEY, true);
            m_showTime = EditorPrefs.GetBool(SHOW_TIME_KEY, true);

            Color infoColor = new Color(0.8f, 0.8f, 0.8f);
            GUIStyle infoStyle = new GUIStyle(GUIStyle.none);
            infoStyle.wordWrap = false;
            infoStyle.fontStyle = FontStyle.Bold;
            infoStyle.fontSize = 13;
            infoStyle.padding = new RectOffset(5, 5, 0, 0);
            infoStyle.normal.textColor = infoColor;
            infoStyle.focused.textColor = infoColor;
            m_styles.Add(ksLog.Level.INFO, infoStyle);
            
            Color debugColor = new Color(0.0f, 0.85f, 0.85f);
            GUIStyle debugStyle = new GUIStyle(infoStyle);
            debugStyle.normal.textColor = debugColor;
            debugStyle.focused.textColor = debugColor;
            m_styles.Add(ksLog.Level.DEBUG, debugStyle);

            Color warningColor = new Color(1.0f, 0.7f, 0.2f);
            GUIStyle warningStyle = new GUIStyle(infoStyle);
            warningStyle.normal.textColor = warningColor;
            warningStyle.focused.textColor = warningColor;
            m_styles.Add(ksLog.Level.WARNING, warningStyle);

            Color errorColor = new Color(1.0f, 0.0f, 0.0f);
            GUIStyle errorStyle = new GUIStyle(infoStyle);
            errorStyle.normal.textColor = errorColor;
            errorStyle.focused.textColor = errorColor;
            m_styles.Add(ksLog.Level.ERROR, errorStyle);
            m_styles.Add(ksLog.Level.FATAL, errorStyle);
            
            m_initialized = true;
        }

        /// <summary>Draw the GUI.</summary>
        /// <param name="window">GUI window</param>
        public void Draw(ksWindow window)
        {
            // Initialization
            if (!m_initialized)
            {
                Initialize();
            }

            EditorGUILayout.BeginHorizontal();

            // Log File List
            if (m_GUILogList.Count > 0)
            {
                m_GUILogIndex = EditorGUILayout.Popup(m_GUILogIndex, m_GUILogList.ToArray());
                if (m_currentLog != m_logs[m_GUILogIndex])
                {
                    m_currentLog = m_logs[m_GUILogIndex];
                    OpenLog();
                }
            }
            else
            {
                EditorGUILayout.Popup(0, new string[] { "No Log File" });
                m_currentLog = null;
            }

            // Status (running/stopped)
            if (m_currentLog != null && m_currentLog.Server != null && m_currentLog.Server.IsRunning)
            {
                GUILayout.Label(new GUIContent(ksTextures.Online, "Server running"), GUILayout.Width(16));
            }
            else
            {
                GUILayout.Label(new GUIContent(ksTextures.Offline, "Server stopped"), GUILayout.Width(16));
            }

            // Actions
            if (EditorGUILayout.DropdownButton(new GUIContent("Action"), FocusType.Keyboard,
                GUILayout.Width(BUTTON_WIDTH), GUILayout.Height(BUTTON_HEIGHT)))
            {
                GenericMenu menu = new GenericMenu();
                GenericMenu.MenuFunction stop = null;
                GenericMenu.MenuFunction delete = DeleteLog;
                if (m_currentLog != null && m_currentLog.Server != null && m_currentLog.Server.IsRunning)
                {
                    stop = delegate(){ m_currentLog.Server.Stop(); };
                    delete = null;
                }
                menu.AddItem(new GUIContent("Stop Server"), false, stop);
                menu.AddItem(new GUIContent("Delete Log"), false, delete);
                menu.AddItem(new GUIContent("Open Log Directory"), false, OpenLogDirectory);

                menu.ShowAsContext();
            }

            // Display Style
            if (EditorGUILayout.DropdownButton(new GUIContent("Display"), FocusType.Keyboard,
                GUILayout.Width(BUTTON_WIDTH), GUILayout.Height(BUTTON_HEIGHT)))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Date"), m_showDate, () => 
                {
                    m_showDate = !m_showDate;
                    EditorPrefs.SetBool(SHOW_DATE_KEY, m_showDate);
                });
                menu.AddItem(new GUIContent("Time"), m_showTime, () => 
                {
                    m_showTime = !m_showTime;
                    EditorPrefs.SetBool(SHOW_TIME_KEY, m_showTime);
                });
                menu.AddItem(new GUIContent("Type"), m_showType, () => 
                {
                    m_showType = !m_showType;
                    EditorPrefs.SetBool(SHOW_TYPE_KEY, m_showType);
                });
                menu.ShowAsContext();
            }
            EditorGUILayout.EndHorizontal();

            if (m_currentLog == null)
            {
                return;
            }

            // Get console window size
            Rect rect = EditorGUILayout.GetControlRect(false);
            if (Event.current.type == EventType.Repaint)
            {
                m_consoleRect = rect;
                m_consoleRect.y -= 4;
                //m_consoleRect.height = m_window.position.height - m_consoleRect.y - 18;
                m_consoleRect.height = m_window.position.height - m_consoleRect.y - 36;
                m_consoleRect.width -= 15;

                m_hScrollRect = new Rect(m_consoleRect);
                m_hScrollRect.y = m_consoleRect.yMax;
                m_hScrollRect.width = m_consoleRect.width;
                if (m_consoleWidth > m_consoleRect.width)
                {
                    m_hScrollRect.height = 18;
                }
                else
                {
                    m_hScrollRect.height = 0;
                    m_consoleRect.height += 18;
                }

                m_vScrollRect = new Rect(m_consoleRect);
                m_vScrollRect.width = 18;
                m_vScrollRect.x = m_consoleRect.xMax;
                m_vScrollRect.height = m_consoleRect.height;
            }

            //  If this window is not focused or the event is used then 
            //  clear text selection before rendering the console
            if (EditorWindow.focusedWindow != window || Event.current.type == EventType.Used)
            {
                m_selectionStart = new LogSelection(-1, -1);
                m_selectionEnd = new LogSelection(-1, -1);
            }

            // Vertical scroll bar
            float rowCount = m_consoleRect.height / 15.0f;
            float maxScrollValue = ((float)m_logMessages.Count > rowCount) ? (float)m_logMessages.Count: rowCount;
            if (m_autoScroll)
            {
                m_scrollValue.y = maxScrollValue - Mathf.Floor(rowCount);
            }
            if (Event.current.type == EventType.ScrollWheel && m_consoleRect.Contains(Event.current.mousePosition))
            {
                m_scrollValue.y += Mathf.Round(Event.current.delta.y / 2f);
                m_scrollValue.y = Math.Max(0, m_scrollValue.y);
                m_scrollValue.y = Math.Min(maxScrollValue, m_scrollValue.y);
                window.Repaint();
            }
            m_scrollValue.y = GUI.VerticalScrollbar(m_vScrollRect, m_scrollValue.y, Mathf.Floor(rowCount), 0, maxScrollValue);
            m_autoScroll = (m_scrollValue.y == maxScrollValue - Mathf.Floor(rowCount));

            // Console region
            EditorGUI.DrawRect(m_consoleRect, Color.black);
            Color tint = GUI.backgroundColor;
            GUI.backgroundColor = Color.black;
            GUI.BeginGroup(m_consoleRect);

            // Log Messages
            rect = EditorGUILayout.GetControlRect(false);
            rect.y = m_consoleRect.height;
            Vector2 size;
            LogMessage msg;
            GUIStyle style;
            GUIContent content;
            int index = 0;
            for (int i = (int)rowCount-1; i >= 0; i--)
            {
                index = (int)m_scrollValue.y + i;
                if (index < m_logMessages.Count)
                {
                    msg = m_logMessages[index];
                    style = m_styles[msg.Level];
                    content = new GUIContent(msg.GetDisplayMessage(m_showDate, m_showTime, m_showType));
                    size = style.CalcSize(content);
                    rect.x = -m_scrollValue.x;
                    rect.y -= rect.height;
                    rect.width = m_consoleWidth;
                    rect.height = size.y - 1;

                    DrawLogMessage(m_window, style, content, index, rect);
                    if (rect.y < 0)
                    {
                        break;
                    }
                }
            }
            GUI.EndGroup();
            GUI.backgroundColor = tint;

            // Horizontal scroll bar
            if (m_hScrollRect.height > 0)
            {
                maxScrollValue = (m_consoleWidth < m_consoleRect.width) ? 200.0f : (m_consoleWidth - m_consoleRect.width + 200.0f);
                m_scrollValue.x = GUI.HorizontalScrollbar(m_hScrollRect, m_scrollValue.x, 200.0f, 0.0f, maxScrollValue);
            }

            // Copy selected text to clipboard
            if (Event.current.type == EventType.ValidateCommand && Event.current.commandName == "Copy"
                && m_selectionStart.MessageIndex != -1 && m_selectionEnd.MessageIndex != -1)
            {
                GUIUtility.systemCopyBuffer = GetSelectionText();
                Event.current.Use();
                return;
            }
        }

        /// <summary>Draw and process events for log messages.</summary>
        /// <param name="window">Window</param>
        /// <param name="style">Draw style</param>
        /// <param name="content">Draw content</param>
        /// <param name="messageIndex">Message index in the message list.</param>
        /// <param name="rect">Draw region</param>
        public void DrawLogMessage(ksWindow window, GUIStyle style, GUIContent content, int messageIndex, Rect rect)
        {
            int id = GUIUtility.GetControlID(content, FocusType.Keyboard);
            EventType controlEvent = Event.current.GetTypeForControl(id);

            if (controlEvent == EventType.Repaint)
            {
                // Selection
                int selectionStart = 0;
                int selectionEnd = 0;
                GetSelection(messageIndex, content.text.Length, out selectionStart, out selectionEnd);

                // Control ID
                int oldId = GUIUtility.keyboardControl;
                GUIUtility.keyboardControl = id;
                if (selectionStart < 0 || selectionEnd < 0)
                {
                    style.Draw(rect, content, id);
                }
                else
                {
                    style.DrawWithTextSelection(rect, content, id, selectionStart, selectionEnd);
                }
                GUIUtility.keyboardControl = oldId;
            }
            else if (controlEvent == EventType.MouseDown && Event.current.button == 0)
            {
                GUIUtility.keyboardControl = id;
                if (rect.Contains(Event.current.mousePosition))
                {
                    int stringIndex = style.GetCursorStringIndex(rect, content, Event.current.mousePosition);
                    if (!Event.current.shift || m_selectionStart.MessageIndex == -1)
                    {
                        m_selectionStart = new LogSelection(messageIndex, stringIndex);
                        m_selectionEnd = new LogSelection(messageIndex, stringIndex);
                    }
                    else
                    {
                        m_selectionEnd = new LogSelection(messageIndex, stringIndex);
                    }
                    m_selecting = true;
                    window.Repaint();
                }
            }
            else if (controlEvent == EventType.MouseDrag && Event.current.button == 0 && m_selecting)
            {
                if (rect.Contains(Event.current.mousePosition))
                {
                    int stringIndex = style.GetCursorStringIndex(rect, content, Event.current.mousePosition);
                    m_selectionEnd = new LogSelection(messageIndex, stringIndex);
                    window.Repaint();
                }
            }
            else if (controlEvent == EventType.MouseUp && Event.current.button == 0 && m_selecting)
            {
                m_selecting = false;
            }
        }

        /// <summary>Get the start and end selection positions for a displayed log message.</summary>
        /// <param name="messageIndex">Message index in the message list.</param>
        /// <param name="length">Message character length.</param>
        /// <param name="start">Selection start position.</param>
        /// <param name="end">Selection end position.</param>
        private void GetSelection(int messageIndex, int length, out int start, out int end)
        {
            start = -1;
            end = -1;

            LogSelection s1 = m_selectionStart;
            LogSelection s2 = m_selectionEnd;
            if (s2.MessageIndex < s1.MessageIndex || (s2.MessageIndex == s1.MessageIndex && s2.StringIndex < s1.StringIndex))
            {
                s1 = m_selectionEnd;
                s2 = m_selectionStart;
            }

            if (messageIndex >= s1.MessageIndex && messageIndex <= s2.MessageIndex)
            {
                start = (messageIndex == s1.MessageIndex) ? s1.StringIndex : 0;
                end = (messageIndex == s2.MessageIndex) ? s2.StringIndex : length;
            }
        }

        /// <summary>Construct a selection string from all messages included in the selection range.</summary>
        /// <returns>Selection text</returns>
        private string GetSelectionText()
        {
            if (m_selectionStart.MessageIndex == -1 || m_selectionEnd.MessageIndex == -1)
            {
                return "";
            }

            LogSelection s1 = m_selectionStart;
            LogSelection s2 = m_selectionEnd;
            if (s2.MessageIndex < s1.MessageIndex || (s2.MessageIndex == s1.MessageIndex && s2.StringIndex < s1.StringIndex))
            {
                s1 = m_selectionEnd;
                s2 = m_selectionStart;
            }

            string selectionText = "";
            string msgText;
            for (int i = s1.MessageIndex; i <= s2.MessageIndex; i++)
            {
                msgText = m_logMessages[i].GetDisplayMessage(m_showDate, m_showTime);

                if (i == s1.MessageIndex && i == s2.MessageIndex)
                {
                    return msgText.Substring(s1.StringIndex, s2.StringIndex - s1.StringIndex);
                }

                if (i == s1.MessageIndex)
                {
                    selectionText += msgText.Substring(s1.StringIndex);
                }
                else if (i == s2.MessageIndex)
                {
                    selectionText += "\n" + msgText.Substring(0, s2.StringIndex);
                }
                else
                {
                    selectionText += "\n" + msgText;
                }
            }

            return selectionText;
        }

        /// <summary>Delete the current log file.</summary>
        private void DeleteLog()
        {
            if (m_currentLog == null)
            {
                return;
            }

            CloseLog();
            if (File.Exists(m_currentLog.File))
            {
                File.Delete(m_currentLog.File);
            }
            m_GUILogIndex = 0;
            m_currentLog = null;
            UpdateFileList();
        }

        /// <summary>Open a file explorer to the server log directory.</summary>
        private void OpenLogDirectory()
        {
            Application.OpenURL(Path.Combine(ksPaths.ProjectRoot, ksPaths.LogDir));
        }

        /// <summary>
        /// If the menu has been initialized then read the log messages from the current log file if
        /// the file size has changed.
        /// </summary>
        public void Update()
        {
            if (m_fileRefeshTime <= DateTime.Now)
            {
                m_fileRefeshTime += FILE_REFRESH_DELAY;
                UpdateFileList();
            }

            if (m_currentLog == null || !m_initialized)
            {
                return;
            }

            // Open the current log file if it is not open
            if (m_fileReader == null && !OpenLog())
            {
                return;
            }

            // Check log file size and read the log
            if (m_logFileSize == 0)
            {
                ReadLog();
                m_logFileSize = new System.IO.FileInfo(m_currentLog.File).Length;
            }
            else
            {
                long size = new System.IO.FileInfo(m_currentLog.File).Length;
                if (size != m_logFileSize)
                {
                    ReadLog();
                    m_logFileSize = size;
                }
            }
        }

        /// <summary>Attempt to open a log file.</summary>
        /// <returns>True if the log file was opened successfully.</returns>
        private bool OpenLog()
        {
            CloseLog();
            m_logMessages.Clear();
            m_logFileSize = 0;
            m_consoleWidth = 0;

            try
            {
                if (File.Exists(m_currentLog.File))
                {
                    m_fileStream = File.Open(m_currentLog.File, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    m_fileReader = new StreamReader(m_fileStream);
                    return true;
                }
            }
            catch (Exception ex)
            {
                ksLog.Error(this, "Error opening log file " + m_currentLog.File, ex);
            }
            return false;
        }

        /// <summary>Close any open log file.</summary>
        private void CloseLog()
        {
            try
            {
                if (m_fileReader != null)
                {
                    m_fileReader.Close();
                    m_fileReader = null;
                }

                if (m_fileStream != null)
                { 
                    m_fileStream = null;
                }
            }
            catch(Exception ex)
            {
                ksLog.Error(this, "Error closing log file " + m_currentLog.File, ex);
            }
        }

        /// <summary>Read and parse the lines from a log file and store them as log messages.</summary>
        private void ReadLog()
        {
            if (m_fileReader == null || !m_fileStream.CanRead)
            {
                return;
            }
            
            string line = null;
            while ((line = m_fileReader.ReadLine()) != null)
            {
                LogMessage msg = new LogMessage(line);

                if (msg.Type.Contains("DEBUG"))
                {
                    msg.Level = ksLog.Level.DEBUG;
                }
                else if (msg.Type.Contains("WARNING"))
                {
                    msg.Level = ksLog.Level.WARNING;
                }
                else if (msg.Type.Contains("ERROR"))
                {
                    msg.Level = ksLog.Level.ERROR;
                }
                else if (msg.Type.Contains("FATAL"))
                {
                    msg.Level = ksLog.Level.FATAL;
                }
                else if(msg.Type.Contains("INFO"))
                {
                    msg.Level = ksLog.Level.INFO;
                }
                else
                {
                    msg.Level = m_lastLogLevel;
                }
                m_lastLogLevel = msg.Level;
                m_logMessages.Add(msg);

                Vector2 size = m_styles[msg.Level].CalcSize(new GUIContent(line));
                if (size.x > m_consoleWidth)
                {
                    m_consoleWidth = size.x;
                }
            }

            if (m_window != null)
            {
                m_window.Repaint();
            }
        }

        /// <summary>Update the log file list and set a new current log.</summary>
        /// <param name="filename">file path</param>
        public void SelectLog(string filename)
        {
            UpdateFileList();
            foreach(Log log in m_logs)
            {
                if (filename == log.File)
                {
                    m_currentLog = log;
                    m_GUILogIndex = 0;
                    m_autoScroll = true;
                    OpenLog();
                    return;
                }
            }
        }

        /// <summary>Rebuild the list of log files and match them to running local server instances.</summary>
        public void UpdateFileList()
        {
            // Validate server directory
            if (!Directory.Exists(ksPaths.LogDir))
            {
                return;
            }

            // Server log from local server instances
            m_logs.Clear();
            List<string> files = new List<string>(Directory.GetFiles(ksPaths.LogDir));
            ksLocalServer.Manager.Servers.Iterate((ksLocalServer server) =>
            {
                if (string.IsNullOrEmpty(server.LogFile))
                {
                    return;
                }
                if (server.IsRunning)
                {
                    if (files.Contains(server.LogFile))
                    {
                        files.Remove(server.LogFile);
                    }

                    Log log = new Log();
                    log.File = server.LogFile;
                    log.Datetime = File.GetCreationTime(log.File);
                    log.Server = server;
                    m_logs.Add(log);
                }
            });

            // Older logs from
            foreach(string file in files)
            {
                if (file.EndsWith(".log"))
                {
                    Log log = new Log();
                    log.File = file;
                    log.Datetime = File.GetCreationTime(log.File);
                    m_logs.Add(log);
                }
            }
            m_logs.Sort();

            // Delete older logs in excess of the reactor config history size
            while (m_logs.Count > ksReactorConfig.Instance.Server.LogHistorySize)
            {
                Log log = m_logs.Last();
                if (log.Server == null || !log.Server.IsRunning)
                {
                    try
                    {
                        File.Delete(log.File);
                    }
                    catch { }
                }
                m_logs.Remove(log);
            }

            // Create the gui log list
            m_GUILogList.Clear();
            m_GUILogIndex = 0;
            foreach (Log log in m_logs)
            {
                string filename = log.File.Substring(log.File.LastIndexOf('/') + 1);
                if (log.Server != null && log.Server.IsRunning)
                {
                    filename += "*";
                }
                m_GUILogList.Add(filename);
                if (m_currentLog != null && log.File == m_currentLog.File)
                {
                    m_GUILogIndex = m_GUILogList.Count - 1;
                }
            }

            if (m_GUILogList.Count > m_GUILogIndex)
            {
                m_currentLog = m_logs[m_GUILogIndex];
            }

            if (m_window)
            {
                m_window.Repaint();
            }
        }
    }
}
