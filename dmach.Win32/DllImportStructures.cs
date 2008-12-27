using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

using HANDLE = System.IntPtr;


namespace InSolve.dmach.Win32
{
    /// <summary>
    /// Cache native structure size calculation
    /// </summary>
    static class StrSize
    {
        public static readonly int UNICODE_STRING = Marshal.SizeOf(typeof(UNICODE_STRING));
        public static readonly int PROCESS_BASIC_INFORMATION = Marshal.SizeOf(typeof(PROCESS_BASIC_INFORMATION));
        public static readonly int PEB = Marshal.SizeOf(typeof(PEB));
        public static readonly int PROCESS_PARAMETERS = Marshal.SizeOf(typeof(PROCESS_PARAMETERS));
        public static readonly int PROCESS_MEMORY_COUNTERS = Marshal.SizeOf(typeof(PROCESS_MEMORY_COUNTERS));
        public static readonly int WTSProcessInfo = Marshal.SizeOf(typeof(WTSProcessInfo));
    }

    [StructLayout(LayoutKind.Sequential)]
    struct UNICODE_STRING
    {
        /// <summary>
        /// Specifies the length, in bytes, of the string pointed to by the Buffer member, 
        /// not including the terminating NULL character, if any.
        /// </summary>
        public ushort Length;
        /// <summary>
        /// Specifies the total size, in bytes, of memory allocated for Buffer. 
        /// Up to MaximumLength bytes may be written into the buffer without trampling memory.
        /// </summary>
        public ushort MaximumLength;
        /// <summary>
        /// Pointer to a wide-character string.
        /// </summary>
        public IntPtr Address;
    }

    /// <summary>
    /// Process Security and Access Rights
    /// </summary>
    [Flags]
    public enum PROCESS_ACCESS : uint
    {
        /// <summary>
        /// All possible access rights for a process object.
        /// </summary>
        All = 0x001F0FFF,               // 2035711
        /// <summary>
        /// Required to terminate a process
        /// </summary>
        Terminate = 0x00000001,         // 1
        /// <summary>
        /// Required to create a thread.
        /// </summary>
        CreateThread = 0x00000002,      // 2
        /// <summary>
        /// Required to perform an operation on the address space of a process
        /// </summary>
        VMOperation = 0x00000008,       // 8
        /// <summary>
        /// Required to read memory in a process
        /// </summary>
        VMRead = 0x00000010,            // 16
        /// <summary>
        /// Required to write to memory in a process
        /// </summary>
        VMWrite = 0x00000020,           // 32
        /// <summary>
        /// Required to duplicate a handle
        /// </summary>
        DupHandle = 0x00000040,         // 64
        /// <summary>
        /// Required to set certain information about a process, such as its priority class
        /// </summary>
        SetInformation = 0x00000200,    // 512
        /// <summary>
        /// Required to retrieve certain information about a process, such as its token, exit code, and priority class
        /// </summary>
        QueryInformation = 0x00000400,  // 1024
        /// <summary>
        /// Required to wait for the process to terminate 
        /// </summary>
        Synchronize = 0x00100000        // 1048576
    }

    [StructLayout(LayoutKind.Sequential)]
    struct PROCESS_BASIC_INFORMATION
    {
        public int ExitStatus;
        public IntPtr PebBaseAddress;
        public IntPtr AffinityMask;
        public IntPtr BasePriority;
        public IntPtr UniqueProcessId;
        public IntPtr InheritedFromUniqueProcessId;
    }

    enum PROCESSINFOCLASS : int
    {
        ProcessBasicInformation = 0,
        ProcessQuotaLimits,
        ProcessIoCounters,
        ProcessVmCounters,
        ProcessTimes,
        ProcessBasePriority,
        ProcessRaisePriority,
        ProcessDebugPort,
        ProcessExceptionPort,
        ProcessAccessToken,
        ProcessLdtInformation,
        ProcessLdtSize,
        ProcessDefaultHardErrorMode,
        ProcessIoPortHandlers, // Note: this is kernel mode only
        ProcessPooledUsageAndLimits,
        ProcessWorkingSetWatch,
        ProcessUserModeIOPL,
        ProcessEnableAlignmentFaultFixup,
        ProcessPriorityClass,
        ProcessWx86Information,
        ProcessHandleCount,
        ProcessAffinityMask,
        ProcessPriorityBoost,
        MaxProcessInfoClass,
        ProcessWow64Information = 26
    }

    [StructLayout(LayoutKind.Sequential)]
    struct PEB
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        byte[] Reserved1;
        byte BeingDebugged;
        byte Reserved2;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        IntPtr[] Reserved3;
        IntPtr Ldr;
        public IntPtr ProcessParameters;
    }

    //[StructLayout(LayoutKind.Sequential)]
    //struct PROCESS_PARAMETERS
    //{
    //    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
    //    byte[] Reserved1;
    //    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
    //    IntPtr[] Reserved2;
    //    public UNICODE_STRING ImagePathName;
    //    public UNICODE_STRING CommandLine;
    //    public IntPtr EnvironmentBlock;
    //} ;

    [StructLayout(LayoutKind.Sequential)]
    struct PROCESS_PARAMETERS
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        byte[] Reserved1;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        IntPtr[] Reserved2;
        public UNICODE_STRING CurrentDirectory;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        IntPtr[] Reserved3;
        public UNICODE_STRING ImagePathName;
        public UNICODE_STRING CommandLine;
        public IntPtr EnvironmentBlock;
    }

    /// <summary>
    /// Identifies the processor and bits-per-word of the platform targeted by an executable.
    /// </summary>
    public enum PROCESSOR_ARCHITECTURE : ushort
    {
        /// <summary>
        /// Unknown processor
        /// </summary>
        PROCESSOR_ARCHITECTURE_UNKNOWN = 0xfff,
        /// <summary>
        /// x86 processor
        /// </summary>
        PROCESSOR_ARCHITECTURE_INTEL = 0,
        /// <summary>
        /// Intel Itanium processor
        /// </summary>
        PROCESSOR_ARCHITECTURE_IA64 = 6,
        /// <summary>
        /// AMD64 or E64MT processor
        /// </summary>
        PROCESSOR_ARCHITECTURE_AMD64 = 9
    }

    [StructLayout(LayoutKind.Sequential)]
    struct SYSTEM_INFO
    {
        internal _PROCESSOR_INFO_UNION uProcessorInfo;
        public uint dwPageSize;
        public uint lpMinimumApplicationAddress;
        public uint lpMaximumApplicationAddress;
        public uint dwActiveProcessorMask;
        public uint dwNumberOfProcessors;
        public uint dwProcessorType;
        public uint dwAllocationGranularity;
        public ushort dwProcessorLevel;
        public ushort dwProcessorRevision;
    }

    [StructLayout(LayoutKind.Explicit)]
    struct _PROCESSOR_INFO_UNION
    {
        [FieldOffset(0)]
        internal uint dwOemId;
        [FieldOffset(0)]
        internal ushort wProcessorArchitecture;
        [FieldOffset(2)]
        internal ushort wReserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct PROCESS_MEMORY_COUNTERS
    {
        public int cb;
        public int PageFaultCount;
        public IntPtr PeakWorkingSetSize;
        public IntPtr WorkingSetSize;
        public IntPtr QuotaPeakPagedPoolUsage;
        public IntPtr QuotaPagedPoolUsage;
        public IntPtr QuotaPeakNonPagedPoolUsage;
        public IntPtr QuotaNonPagedPoolUsage;
        public IntPtr PagefileUsage;
        public IntPtr PeakPagefileUsage;
    }

    /// <summary>
    /// Contains information about a process running on a terminal server
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct WTSProcessInfo
    {
        /// <summary>
        /// Terminal Services session identifier for the session associated with the process.
        /// </summary>
        public uint SessionId;
        /// <summary>
        /// Process identifier that uniquely identifies the process on the terminal server.
        /// </summary>
        public uint ProcessId;
        /// <summary>
        /// Pointer to a null-terminated string containing the name of the executable file associated with the process.
        /// </summary>
        public IntPtr pProcessName;
        /// <summary>
        /// Pointer to the user SID in the process's primary access token.
        /// </summary>
        public IntPtr pUserSid;
    }

}
