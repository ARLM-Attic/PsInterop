using System;
using System.Collections.Generic;
using System.Text;

namespace InSolve.dmach.PsInterop
{
    static class Win32Error
    {
        /// <summary>
        /// No mapping between account names and security IDs was done.
        /// </summary>
        public const int ERROR_NONE_MAPPED = 1332;
        /// <summary>
        /// The data area passed to a system call is too small.
        /// </summary>
        public const int ERROR_INSUFFICIENT_BUFFER = 122;
    }
}
