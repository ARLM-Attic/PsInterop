using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

using HANDLE = System.IntPtr;

namespace InSolve.dmach.PsInterop
{
    class DllImportWrapper
    {
        public static string ReadProcessMemoryUnicodeString(ProcessHandle p, UNICODE_STRING source, MemoryBuffer buffer)
        {
            int readLength = (source.Length < buffer.Size) ?
                source.Length : buffer.Size;
            if (!DllImport.ReadProcessMemory(p.Handle, source.Address, buffer.Address, readLength, out buffer.BytesRead))
                throw new ApplicationException("ReadProcessMemory(" + p.ProcessId + ") [Unicode at 0x"+source.Address.ToString("X")+"] error: " + Marshal.GetLastWin32Error());

            Marshal.Copy(buffer.Address, buffer.Array, 0, buffer.BytesRead);
            return Encoding.Unicode.GetString(buffer.Array, 0, buffer.BytesRead);
        }

        public static void ReadProcessMemoryPEB(ref ProcessHandle p, MemoryBuffer buffer)
        {
            if (buffer.Size < StrSize.PEB)
                throw new ApplicationException("Buffer too small for PEB structure");
            if (buffer.Size < StrSize.PROCESS_PARAMETERS)
                throw new ApplicationException("Buffer too small for PROCESS_PARAMETERS structure");

            //1. Get PEB address
            PROCESS_BASIC_INFORMATION pbi = new PROCESS_BASIC_INFORMATION();
            int nt_status = DllImport.NtQueryInformationProcess(
                p.Handle,
                PROCESSINFOCLASS.ProcessBasicInformation,
                ref pbi,
                StrSize.PROCESS_BASIC_INFORMATION,
                out buffer.BytesRead
                );
            if (!DllImportWrapper.NT_SUCCESS(nt_status))
                throw new ApplicationException("NtQueryInformationProcess("+p.ProcessId+") error: "+Marshal.GetLastWin32Error()+" NTSTATUS: 0x" + nt_status.ToString("X"));

            p.PEBAddress = pbi.PebBaseAddress;

            //2. Read PEB
            if (!DllImport.ReadProcessMemory(p.Handle, p.PEBAddress, buffer.Address, StrSize.PEB, out buffer.BytesRead))
                throw new ApplicationException("ReadProcessMemory(" + p.ProcessId + ") [PEB] error: " + Marshal.GetLastWin32Error());

            PEB peb = (PEB)Marshal.PtrToStructure(buffer.Address, typeof(PEB));
            p.PEBInfoBlockAddress = peb.ProcessParameters;

            //3. Read InfoBlock from PEB
            if (!DllImport.ReadProcessMemory(p.Handle, p.PEBInfoBlockAddress, buffer.Address, StrSize.PROCESS_PARAMETERS, out buffer.BytesRead))
                throw new ApplicationException("ReadProcessMemory(" + p.ProcessId + ") [InfoBlock] error: " + Marshal.GetLastWin32Error());

            PROCESS_PARAMETERS infoBlock = (PROCESS_PARAMETERS)Marshal.PtrToStructure(buffer.Address, typeof(PROCESS_PARAMETERS));
            p.CurrentDirectory = infoBlock.CurrentDirectory;
            p.CommandLine = infoBlock.CommandLine;
            p.ImagePathName = infoBlock.ImagePathName;
            p.EnvironmentBlock = infoBlock.EnvironmentBlock;

            return;
        }

        public static bool NT_SUCCESS(int status)
        {
            // http://msdn2.microsoft.com/en-us/library/aa489609.aspx
            return (status >= 0 && status <= 0x7FFFFFFF);
        }

        public static double FiletimeToMilliseconds(long fileTime)
        {
            return fileTime / 10000;
        }

        public static ProcessPlatformInfo GetProcessPlatform(ProcessHandle p)
        {
            PROCESSOR_ARCHITECTURE processor = Win32Wrapper.GetNativeProcessorInfo();
            ProcessPlatformInfo platform;
            switch (processor)
            {
                case PROCESSOR_ARCHITECTURE.PROCESSOR_ARCHITECTURE_INTEL:
                    {
                        platform = ProcessPlatformInfo.Win32;
                        break;
                    }
                case PROCESSOR_ARCHITECTURE.PROCESSOR_ARCHITECTURE_AMD64:
                    {
                        bool isWow64Process;
                        if (!DllImport.IsWow64Process(p.Handle, out isWow64Process))
                            throw new ApplicationException("IsWow64Process(" + p.ProcessId + ") error: " + Marshal.GetLastWin32Error());

                        if (!isWow64Process)
                        {
                            platform = ProcessPlatformInfo.Win64;
                            if (Win32Wrapper.GetProcessorInfo() == PROCESSOR_ARCHITECTURE.PROCESSOR_ARCHITECTURE_INTEL)
                                platform = platform | ProcessPlatformInfo.NotSupported;
                        }
                        else
                            platform = ProcessPlatformInfo.Wow64;

                        break;
                    }
                case PROCESSOR_ARCHITECTURE.PROCESSOR_ARCHITECTURE_IA64:
                case PROCESSOR_ARCHITECTURE.PROCESSOR_ARCHITECTURE_UNKNOWN:
                    {
                        platform = ProcessPlatformInfo.NotSupported;
                        break;
                    }
                default:
                    throw new NotSupportedException("Unknown processor platform value - 0x" + ((int)processor).ToString("X"));
            }

            return platform;
        }
    }
}
