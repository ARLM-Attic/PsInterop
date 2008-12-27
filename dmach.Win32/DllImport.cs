using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Security;

using HANDLE = System.IntPtr;

namespace InSolve.dmach.Win32
{
    static class DllImport
    {
        [DllImport("advapi32", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool LookupAccountSid(
            string lpSystemName, // name of local or remote computer
            IntPtr pSid, // security identifier
            IntPtr pAccount, // account name buffer
            ref int cchAccount, // size of account name buffer
            IntPtr pDomainName, // domain name
            ref int cchDomainName, // size of domain name buffer
            out int peUse // SID type
            );

        [DllImport("wtsapi32", CharSet = CharSet.Auto, SetLastError = true),
        SuppressUnmanagedCodeSecurityAttribute]
        public static extern bool WTSEnumerateProcesses(
            HANDLE ProcessHandle, // handle to process (from WTSOpenServer)
            int Reserved, // dmust be 0
            uint Version, // must be 1
            ref IntPtr ppProcessInfo, // pointer to pointer to Processinfo
            ref uint pCount // no. of processes
            );

        [DllImport("wtsapi32", SetLastError = true),
        SuppressUnmanagedCodeSecurityAttribute]
        public static extern IntPtr WTSOpenServer(
            string ServerName // Server name (NETBIOS)
            );

        [DllImport("wtsapi32", SetLastError = true),
        SuppressUnmanagedCodeSecurityAttribute]
        public static extern void WTSCloseServer(
            HANDLE hServer // Handle obtained by WTSOpenServer
            );

        [DllImport("wtsapi32", SetLastError = true),
        SuppressUnmanagedCodeSecurityAttribute]
        public static extern void WTSFreeMemory(
            IntPtr pMemory
            );

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetProcessTimes(
            HANDLE hProcess,
            out long lpCreationTime,
            out long lpExitTime,
            out long lpKernelTime,
            out long lpUserTime
            );

        [DllImport("psapi.dll", SetLastError = true)]
        public static extern bool GetProcessMemoryInfo(
            HANDLE hProcess,
            out PROCESS_MEMORY_COUNTERS counters,
            int size
            );

        [DllImport("kernel32.dll")] //LastError not required
        public static extern bool TerminateProcess(
            HANDLE hProcess,
            int uExitCode
            );

        [DllImport("kernel32.dll")] //LastError not required
        public static extern bool CloseHandle(
            IntPtr hObject
            );

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool IsWow64Process(
            HANDLE hProcess,
            out bool Wow64Process
            );

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern void GetSystemInfo(
            out SYSTEM_INFO lpSystemInfo
            );

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern void GetNativeSystemInfo(
            out SYSTEM_INFO lpSystemInfo
            );

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadProcessMemory(
            HANDLE hProcess,
            IntPtr BaseAddress,
            IntPtr Buffer,
            int Size,
            out int NumberOfBytesRead
         );

        [DllImport("ntdll.dll", SetLastError = true)]
        public static extern int NtQueryInformationProcess(
            IntPtr hProcess,
            PROCESSINFOCLASS pic,
            ref PROCESS_BASIC_INFORMATION pbi,
            int cb,
            out int pSize
            );

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(
            PROCESS_ACCESS dwDesiredAccess,
            bool bInheritHandle,
            uint dwProcessId
            );
    }
}
