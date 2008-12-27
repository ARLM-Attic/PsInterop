using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

using HANDLE = System.IntPtr;

namespace InSolve.dmach.Win32
{
    /// <summary>
    /// Represent Terminal Service api
    /// </summary>
    public class TerminalServices : IDisposable
    {
        HANDLE hServer;

        //переменные, обслуживающие список процессов
        IntPtr pInfo;
        IntPtr pInfoSave;
        uint count;
        int index;
        
        /// <summary>
        /// Opens the specified terminal server
        /// </summary>
        /// <param name="serverName">NetBIOS name of the terminal server</param>
        public void Open(string serverName)
        {
            //open server
            hServer = DllImport.WTSOpenServer(serverName);
            if (hServer == IntPtr.Zero)
                throw new ApplicationException("WTSOpenServer(" + serverName + ") error: " + Marshal.GetLastWin32Error());
        }

        /// <summary>
        /// Opens terminal server
        /// </summary>
        public void Open()
        {
            this.Open(System.Environment.MachineName);
        }

        /// <summary>
        /// Closes a terminal server
        /// </summary>
        public void Close()
        {
            FreeMemory();

            // Close server handle
            if (hServer != IntPtr.Zero)
            {
                DllImport.WTSCloseServer(hServer);
                hServer = IntPtr.Zero;
            }
        }

        /// <summary>
        /// Retrieves information about the active processes on a terminal server
        /// </summary>
        /// <returns></returns>
        public int EnumerateProcesses()
        {
            FreeMemory();

            if (!DllImport.WTSEnumerateProcesses(hServer, 0, 1, ref pInfo, ref count))
                throw new ApplicationException("WTSEnumerateProcesses() error: " + Marshal.GetLastWin32Error());

            pInfoSave = pInfo;
            return (int)count;
        }

        /// <summary>
        /// Retrieves next process from buffer
        /// </summary>
        /// <param name="WTSpi">process information</param>
        /// <returns></returns>
        public bool GetNextProcess(out WTSProcessInfo WTSpi)
        {
            if (pInfoSave == IntPtr.Zero)
                throw new ApplicationException("Require invoke CreateProcessList() before GetNextProcess()");

            if (index >= count)
            {
                WTSpi = new WTSProcessInfo();
                FreeMemory();
                return false;
            }

            WTSpi = (WTSProcessInfo)Marshal.PtrToStructure(pInfo, typeof(WTSProcessInfo));
            pInfo = new IntPtr(pInfo.ToInt64() + StrSize.WTSProcessInfo);
            index += 1;

            return true;
        }

        /// <summary>
        /// Frees memory allocated by a Terminal Services function
        /// </summary>
        public void FreeMemory()
        {
            index = 0;
            count = 0;

            // Free memory used by WTSEnumerateProcesses
            if (pInfoSave != IntPtr.Zero)
            {
                DllImport.WTSFreeMemory(pInfoSave);
                pInfoSave = IntPtr.Zero;
                pInfo = IntPtr.Zero;
            }
        }

        /// <summary>
        /// Retrieves process name from buffer
        /// </summary>
        /// <param name="info">process information</param>
        /// <returns></returns>
        public static string GetProcessName(WTSProcessInfo info)
        {
            return (info.pProcessName != IntPtr.Zero) ?
                Marshal.PtrToStringAuto(info.pProcessName) : null;
        }


        #region IDisposable Members

        /// <summary>
        /// Releases all resources used by the TerminalServices
        /// </summary>
        public void Dispose()
        {
            Close();
            //GC.SuppressFinalize(this);
        }

        #endregion
    }
}
