using System;
using System.Collections.Generic;
using System.Text;

namespace InSolve.dmach.PsInterop
{
    /// <summary>
    /// Represent platform info
    /// </summary>
    [Flags]
    public enum ProcessPlatformInfo : int
    {
        
        /// <summary>
        /// Pure Win32 environment
        /// </summary>
        Win32 = 1,
        /// <summary>
        /// Pure Win64 environment
        /// </summary>
        Win64 = 2,
        /// <summary>
        /// Win32 process on Win64 environment
        /// </summary>
        Wow64 = Win32 | Win64,
        
/*
        /// <summary>
        /// Windows NT Virtual DOS Machine
        /// </summary>
        NTVDM = 4,
        /// <summary>
        /// 16-bit Windows On Windows subsystem
        /// </summary>
        WOWEXEC = 8,
        /// <summary>
        /// POSIX subsystem
        /// </summary>
        POSIX = 16,
*/

        /// <summary>
        /// Not supported platform
        /// </summary>
        NotSupported = 32
    }
}
