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
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace KS.Reactor.Client.Unity.Editor
{
    /// <summary>
    /// Provides a utility function for starting a minimized process window that does not steal focus on Windows using
    /// p-invoke. On other platforms, starts the process normally.
    /// <see href="https://docs.microsoft.com/en-us/windows/win32/procthread/creating-processes"/>
    /// </summary>
    public class ksProcessUtils
    {
#if UNITY_EDITOR_WIN
        /// <summary>Contains startup information.</summary>
        [StructLayout(LayoutKind.Sequential)]
        struct STARTUPINFO
        {
            public Int32 cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public Int32 dwX;
            public Int32 dwY;
            public Int32 dwXSize;
            public Int32 dwYSize;
            public Int32 dwXCountChars;
            public Int32 dwYCountChars;
            public Int32 dwFillAttribute;
            public Int32 dwFlags;
            public Int16 wShowWindow;
            public Int16 cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        /// <summary>Contains process information.</summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        /// <summary>External call to create a process.</summary>
        [DllImport("kernel32.dll")]
        static extern bool CreateProcess(
            string lpApplicationName,
            string lpCommandLine,
            IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            [In] ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation
        );

        /// <summary>External call to close a process handle.</summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseHandle(IntPtr hObject);

        // For a full list of values see 
        // https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-showwindow?redirectedfrom=MSDN
        const int STARTF_USESHOWWINDOW = 1;
        const int SW_SHOWMINNOACTIVE = 7;

        /// <summary>Starts a process as a minimized window that does not steal focus.</summary>
        /// <param name="fileName">Name of the file to execute.</param>
        /// <param name="arguments">Arguments to pass to the new process.</param>
        /// <param name="workingDirectory">Path of the working directory for the new process.</param>
        /// <returns>Process that was started.</returns>
        public static Process StartProcess(string fileName, string arguments, string workingDirectory)
        {
            try
            {
                STARTUPINFO si = new STARTUPINFO();
                si.cb = Marshal.SizeOf(si);
                si.dwFlags = STARTF_USESHOWWINDOW;
                si.wShowWindow = SW_SHOWMINNOACTIVE;

                PROCESS_INFORMATION pi = new PROCESS_INFORMATION();

                CreateProcess(null, fileName + " " + arguments, IntPtr.Zero, IntPtr.Zero, false,
                    0, IntPtr.Zero, workingDirectory, ref si, out pi);

                Process process = Process.GetProcessById(pi.dwProcessId);
                CloseHandle(pi.hProcess);
                CloseHandle(pi.hThread);
                return process;
            }
            catch (Exception e)
            {
                ksLog.Error(typeof(ksProcessUtils).ToString(), "Error starting process. Using fallback method.", e);

                ProcessStartInfo info = new ProcessStartInfo();
                info.FileName = fileName;
                info.Arguments = arguments;
                info.WorkingDirectory = workingDirectory;
                info.WindowStyle = ProcessWindowStyle.Minimized;
                Process process = new Process();
                process.StartInfo = info;
                process.Start();
                return process;
            }
        }
#else
        /// <summary>Starts a process.</summary>
        /// <param name="fileName">Name of the file to execute.</param>
        /// <param name="arguments">Arguments to pass to the new process.</param>
        /// <param name="workingDirectory">Path of the working directory for the new process.</param>
        /// <returns>Process that was started.</returns>
        public static Process StartProcess(string fileName, string arguments, string workingDirectory)
        {
            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = fileName;
            info.Arguments = arguments;
            info.WorkingDirectory = workingDirectory;
            info.WindowStyle = ProcessWindowStyle.Minimized;
            Process process = new Process();
            process.StartInfo = info;
            process.Start();
            return process;
        }
#endif
    }
}
