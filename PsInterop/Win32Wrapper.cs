using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;

using HANDLE = System.IntPtr;


namespace InSolve.dmach.PsInterop
{
    /// <summary>
    /// Win32 Wrapper
    /// </summary>
    public static class Win32Wrapper
    {
        // Cached variables

        static PROCESSOR_ARCHITECTURE ProcessorInfo;
        static bool ProcessorInfoAssigned = false;
        static PROCESSOR_ARCHITECTURE ProcessorInfoNative;
        static bool ProcessorInfoNativeAssigned = false;

        /// <summary>
        /// Retrieves information about the current system to an application running under WOW64. If the function is called from a 64-bit application, it is equivalent to the GetProcessorInfo method
        /// </summary>
        /// <returns></returns>
        public static PROCESSOR_ARCHITECTURE GetNativeProcessorInfo()
        {
            if (!ProcessorInfoNativeAssigned)
            {
                SYSTEM_INFO sysInfo = new SYSTEM_INFO();
                DllImport.GetNativeSystemInfo(out sysInfo);

                ProcessorInfoNative = (PROCESSOR_ARCHITECTURE)sysInfo.uProcessorInfo.wProcessorArchitecture;
                ProcessorInfoNativeAssigned = true;
            }

            return ProcessorInfoNative;
        }

        /// <summary>
        /// Retrieves information about the current system. To retrieve accurate information for an application running on WOW64, call the GetNativeProcessorInfo method
        /// </summary>
        /// <returns></returns>
        public static PROCESSOR_ARCHITECTURE GetProcessorInfo()
        {
            if (!ProcessorInfoAssigned)
            {
                SYSTEM_INFO sysInfo = new SYSTEM_INFO();
                DllImport.GetSystemInfo(out sysInfo);

                ProcessorInfo = (PROCESSOR_ARCHITECTURE)sysInfo.uProcessorInfo.wProcessorArchitecture;
                ProcessorInfoAssigned = true;
            }

            return ProcessorInfo;
        }

        /// <summary>
        /// Retrieves process environment
        /// </summary>
        /// <param name="p">Handle to the process</param>
        /// <param name="buffer">Helper memory buffer</param>
        /// <returns></returns>
        public static string GetEnvironment(ref ProcessHandle p, MemoryBuffer buffer)
        {
            if (p.EnvironmentBlock == IntPtr.Zero)
                DllImportWrapper.ReadProcessMemoryPEB(ref p, buffer);

            buffer.Text.Length = 0;
            int memReadSize = buffer.Size;
            int memReadSizeDecreaseCount = 0;
            int memReadSizeDecreaseMaxCount = 10;
            IntPtr memAddress = p.EnvironmentBlock;

            int strZeroCount = 0;
            int strHelperArrayPos = 0;
            int strSymbolPos = 0;
            int strMaxBytes = 1024 * 100;   //100kb
            int strReadBytes = 0;

            for (; ; )
            {
                if (memReadSizeDecreaseCount > memReadSizeDecreaseMaxCount || memReadSize == 0)
                    throw new ApplicationException("ReadProcessMemory(" + p.ProcessId + ") [Environment] error: " + Marshal.GetLastWin32Error());
                if (strReadBytes > strMaxBytes)
                    throw new ApplicationException("GetEnvironment(" + p.ProcessId + ") error: process environment block is too long (greater than " + strMaxBytes + " bytes).");

                if (!DllImport.ReadProcessMemory(p.Handle, memAddress, buffer.Address, memReadSize, out buffer.BytesRead))
                {
                    //memory read fail, decrease read bytes count and try again
                    memReadSize = memReadSize / 2;
                    memReadSizeDecreaseCount += 1;
                    continue;
                }

                Marshal.Copy(buffer.Address, buffer.Array, 0, buffer.BytesRead);

                //find string end (\0) in memory block. \0\0 is end of environment block
                for (int i = 0; i < buffer.BytesRead; i++, strHelperArrayPos++, strSymbolPos++)
                {
                    if (strHelperArrayPos >= buffer.HelperArray.Length)
                    {
                        buffer.Text.Append(Encoding.Unicode.GetString(buffer.HelperArray));
                        strHelperArrayPos = 0;
                    }
                    if (strSymbolPos > 1)
                        strSymbolPos = 0;

                    byte b = buffer.Array[i];
                    buffer.HelperArray[strHelperArrayPos] = b;
                    strReadBytes++;

                    strZeroCount = (b == 0 && (strSymbolPos == 0 || strZeroCount > 0))?
                        strZeroCount + 1 : 0;

                    if (strZeroCount == 2)
                    {
                        buffer.Text.Append(Encoding.Unicode.GetString(buffer.HelperArray, 0, strHelperArrayPos - 1));
                        buffer.Text.Append(Environment.NewLine);
                        strHelperArrayPos = -1;
                    }
                    else if (strZeroCount == 4)
                        break;
                }

                if (strZeroCount == 4)
                    break;

                memAddress = new IntPtr(memAddress.ToInt64() + buffer.BytesRead);
            }

            return buffer.Text.ToString();
        }

        /// <summary>
        /// Opens an existing local process object
        /// </summary>
        /// <param name="processId">The identifier of the local process to be opened</param>
        /// <returns></returns>
        public static ProcessHandle OpenProcessHandle(uint processId)
        {
            return OpenProcessHandle(
                processId,
                PROCESS_ACCESS.QueryInformation | PROCESS_ACCESS.VMRead | PROCESS_ACCESS.Terminate
            );
        }

        /// <summary>
        /// Opens an existing local process object
        /// </summary>
        /// <param name="processId">The identifier of the local process to be opened</param>
        /// <param name="access">The access to the process object</param>
        /// <returns></returns>
        public static ProcessHandle OpenProcessHandle(uint processId, PROCESS_ACCESS access)
        {
            HANDLE hProcess = DllImport.OpenProcess(access, false, processId);
            if (hProcess == HANDLE.Zero)
                throw new ApplicationException("OpenProcess(" + processId + ") error: " + Marshal.GetLastWin32Error());

            ProcessHandle p = new ProcessHandle();
            p.Handle = hProcess;
            p.ProcessId = processId;
            p.Access = access;

            try
            {
                p.Platform = DllImportWrapper.GetProcessPlatform(p);
            }
            catch (Exception exc)
            {
                DllImport.CloseHandle(hProcess);
                throw exc;
            }

            return p;
        }

        /// <summary>
        /// Closes an open process handle
        /// </summary>
        /// <param name="p">A valid handle to an open process</param>
        public static void CloseProcessHandle(ProcessHandle p)
        {
            if (p.Handle != IntPtr.Zero)
            {
                DllImport.CloseHandle(p.Handle);
                p.Handle = IntPtr.Zero;
            }
        }

        /// <summary>
        /// Retrieves process command line
        /// </summary>
        /// <param name="p">Handle to the process</param>
        /// <param name="buffer">Helper memory buffer</param>
        /// <returns></returns>
        public static string GetCommandLine(ref ProcessHandle p, MemoryBuffer buffer)
        {
            if (p.CommandLine.Address == IntPtr.Zero)
                DllImportWrapper.ReadProcessMemoryPEB(ref p, buffer);

            return DllImportWrapper.ReadProcessMemoryUnicodeString(p, p.CommandLine, buffer);
        }

        /// <summary>
        /// Retrieves process command line without process name
        /// </summary>
        /// <param name="p">Handle to the process</param>
        /// <param name="buffer">Helper memory buffer</param>
        /// <param name="processImage">Process Image file name</param>
        /// <returns></returns>
        public static string GetCommandLine(ref ProcessHandle p, MemoryBuffer buffer, string processImage)
        {
            string cmd = GetCommandLine(ref p, buffer);

            if (processImage != null)
            {
                if (cmd.StartsWith(processImage))
                    cmd = cmd.Substring(processImage.Length).Trim();
                else if (cmd.StartsWith('"' + processImage + '"'))
                    cmd = cmd.Substring(processImage.Length + 2).Trim();
            }

            return cmd;
        }

        /// <summary>
        /// Retrieves process name from process image
        /// </summary>
        /// <param name="processImage">Process Image</param>
        /// <returns></returns>
        public static string GetProcessName(string processImage)
        {
            if (processImage == null)
                throw new ArgumentNullException("processImage");
            int indexOfSeparator = processImage.LastIndexOf(Path.DirectorySeparatorChar);
            return (indexOfSeparator != -1 && indexOfSeparator != (processImage.Length - 1)) ?
                processImage.Substring(indexOfSeparator + 1) : processImage;
        }

        /// <summary>
        /// Retrieves process current directory
        /// </summary>
        /// <param name="p">Handle to the process</param>
        /// <param name="buffer">Helper memory buffer</param>
        /// <returns></returns>
        public static string GetCurrentDirectory(ref ProcessHandle p, MemoryBuffer buffer)
        {
            if (p.CurrentDirectory.Address == IntPtr.Zero)
                DllImportWrapper.ReadProcessMemoryPEB(ref p, buffer);

            return DllImportWrapper.ReadProcessMemoryUnicodeString(p, p.CurrentDirectory, buffer);
        }

        /// <summary>
        /// Retrieves process image file name
        /// </summary>
        /// <param name="p">Handle to the process</param>
        /// <param name="buffer">Helper memory buffer</param>
        /// <returns></returns>
        public static string GetImagePathName(ref ProcessHandle p, MemoryBuffer buffer)
        {
            if (p.ImagePathName.Address == IntPtr.Zero)
                DllImportWrapper.ReadProcessMemoryPEB(ref p, buffer);

            string processImage = DllImportWrapper.ReadProcessMemoryUnicodeString(p, p.ImagePathName, buffer);
            if (processImage.StartsWith(@"\??\"))
                processImage = processImage.Substring(4);

            return processImage;
        }

        /// <summary>
        /// Retrieves information about the memory usage of the specified process
        /// </summary>
        /// <param name="p">Handle to the process</param>
        /// <returns></returns>
        public static long GetVirtualMemorySize(ProcessHandle p)
        {
            PROCESS_MEMORY_COUNTERS pmc = new PROCESS_MEMORY_COUNTERS();
            if (!DllImport.GetProcessMemoryInfo(p.Handle, out pmc, StrSize.PROCESS_MEMORY_COUNTERS))
                throw new ApplicationException("GetProcessMemoryInfo(" + p.ProcessId + ") error: " + Marshal.GetLastWin32Error());

            return pmc.PagefileUsage.ToInt64();
        }

        /// <summary>
        /// Retrieves timing information for the specified process
        /// </summary>
        /// <param name="p">Handle to the process</param>
        /// <param name="cpuTime">Amount of time that the process has executed in kernel and user mode (in milliseconds)</param>
        /// <param name="execTime">Creation time of the process (in milliseconds)</param>
        public static void GetProcessTimes(ProcessHandle p, out double cpuTime, out double execTime)
        {
            double cpuUserTime;
            double cpuKernelTime;

            GetProcessTimes(p, out cpuUserTime, out cpuKernelTime, out execTime);
            cpuTime = cpuUserTime + cpuKernelTime;

            return;
        }

        /// <summary>
        /// Retrieves timing information for the specified process
        /// </summary>
        /// <param name="p">Handle to the process</param>
        /// <param name="cpuUserTime">Amount of time that the process has executed in user mode (in milliseconds)</param>
        /// <param name="cpuKernelTime">Amount of time that the process has executed in kernel mode (in milliseconds)</param>
        /// <param name="execTime">Creation time of the process (in milliseconds)</param>
        public static void GetProcessTimes(ProcessHandle p, out double cpuUserTime, out double cpuKernelTime, out double execTime)
        {
            long ftCreation, ftExit, ftKernel, ftUser;
            if (!DllImport.GetProcessTimes(p.Handle, out ftCreation, out ftExit, out ftKernel, out ftUser))
                throw new ApplicationException("GetProcessTimes(" + p.ProcessId + ") error: " + Marshal.GetLastWin32Error());

            cpuUserTime = DllImportWrapper.FiletimeToMilliseconds(ftUser);
            cpuKernelTime = DllImportWrapper.FiletimeToMilliseconds(ftKernel);
            execTime = (DateTime.Now.ToUniversalTime() - DateTime.FromFileTimeUtc(ftCreation)).TotalMilliseconds;

            return;
        }

        /// <summary>
        /// Retrieves the name of the account for this SID and the name of the first domain on which this SID is found
        /// </summary>
        /// <param name="sid">Pointer to the SID to look up</param>
        /// <param name="buffer">Helper memory buffer</param>
        /// <returns></returns>
        public static string LookupAccountSid(IntPtr sid, MemoryBuffer buffer)
        {
            IntPtr Domain;
            IntPtr Account;
            int cchDomain;
            int cchAccount;
            buffer.Text.Length = 0;
            int addressOffset;

            //first attempt - random buffer allocation
            addressOffset = buffer.Size / 2;
            if (addressOffset < 4)
                throw new ApplicationException("LookupAccountSid() - MemoryBuffer instance is too small");
            Domain = buffer.Address;
            Account = new IntPtr(Domain.ToInt64() + addressOffset);
            cchDomain = addressOffset / 2;
            cchAccount = cchDomain;

            // Lookup account name on local system
            if (!DllImport.LookupAccountSid(null, sid, Account, ref cchAccount, Domain, ref cchDomain, out buffer.BytesRead))
            {
                int error = Marshal.GetLastWin32Error();
                if (error == Win32Error.ERROR_NONE_MAPPED)
                    return "<unknown user>";
                if (error != Win32Error.ERROR_INSUFFICIENT_BUFFER)
                    throw new ApplicationException("LookupAccountSid() error: " + error);
                if ((cchDomain + cchAccount) * 2 > buffer.Size)
                    throw new ApplicationException("LookupAccountSid() - MemoryBuffer instance is too small");

                //second attempt (only if function return ERROR_INSUFFICIENT_BUFFER) - accuracy buffer allocation
                addressOffset = cchDomain * 2;
                Account = new IntPtr(Domain.ToInt64() + addressOffset);
                if (!DllImport.LookupAccountSid(null, sid, Account, ref cchAccount, Domain, ref cchDomain, out buffer.BytesRead))
                    throw new ApplicationException("LookupAccountSid() error: " + error);
            }

            Marshal.Copy(Domain, buffer.Array, 0, cchDomain * 2);
            buffer.Text.Append(Encoding.Unicode.GetString(buffer.Array, 0, cchDomain * 2));
            buffer.Text.Append(@"\");
            Marshal.Copy(Account, buffer.Array, 0, cchAccount * 2);
            buffer.Text.Append(Encoding.Unicode.GetString(buffer.Array, 0, cchAccount * 2));

            return buffer.Text.ToString();
        }

        /// <summary>
        /// Terminates the specified process and all of its threads
        /// </summary>
        /// <param name="p">Handle to the process</param>
        public static void TerminateProcess(ProcessHandle p)
        {
            DllImport.TerminateProcess(p.Handle, -1);
        }
    }
}
