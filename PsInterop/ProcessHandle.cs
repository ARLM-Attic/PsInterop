using System;

using HANDLE = System.IntPtr;

namespace InSolve.dmach.PsInterop
{
    /// <summary>
    /// Process Handle with cache process information
    /// </summary>
    public struct ProcessHandle
    {
        /// <summary>
        /// Process Handle
        /// </summary>
        public HANDLE Handle;
        /// <summary>
        /// Process ID
        /// </summary>
        public uint ProcessId;
        /// <summary>
        /// Process Platform
        /// </summary>
        public ProcessPlatformInfo Platform;
        /// <summary>
        /// Access flags
        /// </summary>
        public PROCESS_ACCESS Access;

        internal IntPtr PEBAddress;
        internal IntPtr PEBInfoBlockAddress;
        internal UNICODE_STRING CurrentDirectory;
        internal UNICODE_STRING CommandLine;
        internal UNICODE_STRING ImagePathName;
        internal IntPtr EnvironmentBlock;

        /// <summary>
        /// Process platform supported for some methods
        /// </summary>
        public bool IsSupported
        {
            get
            {
                return ((Platform & ProcessPlatformInfo.NotSupported) != ProcessPlatformInfo.NotSupported);
            }
        }
    }
}
