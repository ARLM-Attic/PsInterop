using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

using InSolve.dmach.PsInterop;

namespace pcgi
{
    class Program
    {
        /// <summary>
        /// Тестовая утилита для получаения Process CGI Value
        /// </summary>
        /// <param name="args"></param>
        static int Main(string[] args)
        {
            List<string> Names = new List<string>();
            List<string> Filters = new List<string>();
            int processId = -1;
            bool viewErrors = false;
            bool onlyUrl = false;
            bool screenWidth = false;
            bool kill = false;
            int currentId = System.Diagnostics.Process.GetCurrentProcess().Id;

            try
            {
                char[] paramPrefix = new char[] { '/', '-' };
                for (int i = 0; i < args.Length; i++)
                {
                    string str = args[i];
                    if (str == null)
                        continue;
                    str = str.Trim();
                    if (str.Length == 0)
                        continue;

                    if (str.IndexOfAny(paramPrefix, 0, 1) == 0)
                    {
                        str = str.Substring(1).Trim();

                        if (EqualsIgnoreCase(str, "e"))
                        {
                            viewErrors = true;
                        }
                        else if (EqualsIgnoreCase(str, "u"))
                        {
                            onlyUrl = true;
                        }
                        else if (EqualsIgnoreCase(str, "k"))
                        {
                            kill = true;
                        }
                        else if (EqualsIgnoreCase(str, "s"))
                        {
                            screenWidth = true;
                        }
                        else if (str.StartsWith("f:", StringComparison.CurrentCultureIgnoreCase))
                        {
                            str = str.Substring(2).Trim();
                            if (str.Length > 0)
                                Filters.Add(str);
                        }
                        else if (str == "?")
                        {
                            Console.WriteLine(@"
pcgi [-e] [-u] [-k] [-f:text] [[?]process_name] [-?]

process_name    with .exe postfix or not, prefix '?' for part of process name
-e              show errors
-u              only URL-value process
-f:text         filter
-k              kill found process
-s              crop by console window width
-?              this help

EXAMPLES:

pcgi -s
pcgi -f:dmach.ru -k
pcgi -u -s
pcgi ?php -f:apache -f:idle -k

Goodbye, World!
");
                            return 0;
                        }
                        else
                            throw new ApplicationException("Unknown parameter - '" + str + "'");

                        continue;
                    }

                    int id;
                    if (int.TryParse(str, out id))
                    {
                        processId = id;
                        continue;
                    }

                    if (str.IndexOf('?') != 0)
                        if (!str.EndsWith(".exe", StringComparison.CurrentCultureIgnoreCase))
                            str = str + ".exe";

                    Names.Add(str);
                }

                if (kill && !onlyUrl && (Filters.Count + Names.Count) == 0)
                    throw new ApplicationException("Can't execute -k parameter without other parameters");
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.Message);
                return 1;
            }

            using (MemoryBuffer buffer = new MemoryBuffer())
            using (TerminalServices ts = new TerminalServices())
            {
                try
                {
                    try
                    {
                        Process.EnterDebugMode();
                    }
                    catch (Exception exc)
                    {
                        if (viewErrors)
                            Console.WriteLine("EnterDebugMode() fail, {0}: {1}", exc.GetType().Name, exc.Message);
                    }

                    ts.EnumerateProcesses();

                    WTSProcessInfo WTSpi;
                    while (ts.GetNextProcess(out WTSpi))
                    {
                        if (WTSpi.ProcessId < 5)
                            continue;
                        string processName = TerminalServices.GetProcessName(WTSpi);
                        try
                        {
                            if (processId != -1)
                            {
                                if (WTSpi.ProcessId != processId)
                                    continue;
                            }
                            else
                            {
                                if (Names.Count > 0)
                                {
                                    bool requiredCheck = false;
                                    foreach (string pr in Names)
                                    {
                                        requiredCheck = (pr[0] == '?') ?
                                            (processName.IndexOf(pr.Substring(1), StringComparison.CurrentCultureIgnoreCase) != -1) :
                                            EqualsIgnoreCase(processName, pr);

                                        if (requiredCheck)
                                            break;
                                    }

                                    if (!requiredCheck)
                                        continue;
                                }
                            }

                            ProcessHandle p = Win32Wrapper.OpenProcessHandle(WTSpi.ProcessId);
                            if (!p.IsSupported)
                                continue;
                            string pValue = GetProcessValue(p, buffer);
                            if (onlyUrl && !pValue.StartsWith("URL"))
                                continue;
                            if (Filters.Count > 0)
                            {
                                bool exists = true;
                                foreach (string filter in Filters)
                                {
                                    if (pValue.IndexOf(filter, StringComparison.CurrentCultureIgnoreCase) == -1)
                                    {
                                        exists = false;
                                        break;
                                    }
                                }
                                if (!exists)
                                    continue;
                            }

                            string killMessage = "";
                            if (kill && p.ProcessId != currentId)
                            {
                                Win32Wrapper.TerminateProcess(p);
                                killMessage = "KILL ";
                            }

                            string message = string.Format("{0}{1} ({2}) {3}",
                                killMessage,
                                WTSpi.ProcessId,
                                processName,
                                pValue);

                            if (screenWidth && message.Length >= Console.WindowWidth)
                                message = message.Substring(0, Console.WindowWidth - 4) + "...";

                            Console.WriteLine(message);
                        }
                        catch (Exception exc)
                        {
                            if (viewErrors)
                            {
                                string message = string.Format("    {0} ({1}) EXC: {2}",
                                    WTSpi.ProcessId,
                                    processName,
                                    exc.Message);
                                if (screenWidth && message.Length >= Console.WindowWidth)
                                    message = message.Substring(0, Console.WindowWidth - 3) + "...";
                                Console.WriteLine(message);
                            }
                        }
                    }
                }
                catch (Exception exc)
                {
                    Console.WriteLine(exc);
                    return 2;
                }
            }

            return 0;
        }

        public static string GetProcessValue(ProcessHandle p, MemoryBuffer buffer)
        {
            string Environment = Win32Wrapper.GetEnvironment(ref p, buffer);
            string Directory = Win32Wrapper.GetCurrentDirectory(ref p, buffer);
            string ProcessImage = Win32Wrapper.GetImagePathName(ref p, buffer);
            string CommandLine = Win32Wrapper.GetCommandLine(ref p, buffer, ProcessImage);
            string result;

            if (Environment != null)
            {
                string envHttpHost = GetEnvironmentValue("HTTP_HOST", Environment);
                if (envHttpHost != null)
                {
                    string envRequestMethod = GetEnvironmentValue("REQUEST_METHOD", Environment);
                    string envScriptName = GetEnvironmentValue("SCRIPT_NAME", Environment);
                    string envQueryString = GetEnvironmentValue("QUERY_STRING", Environment);
                    string envQueryStringPrefix = (envQueryString == null || envQueryString.Length == 0) ? 
                        "" : "?";

                    result = string.Format(
                        "URL: {0} {1}{2}{3}{4}",
                        envRequestMethod,
                        envHttpHost,
                        envScriptName,
                        envQueryStringPrefix,
                        envQueryString
                    );
                }
                else
                    result = (CommandLine.Length != 0)? "CMD: " + CommandLine : "DIR: " + Directory;
            }
            else
                result = "Unknown error";

            return result;
        }

        static string GetEnvironmentValue(string name, string environment)
        {
            if (environment == null)
                throw new ArgumentNullException("environment");
            if (name == null)
                throw new ArgumentNullException("name");

            name = name + "=";
            int indexOfName;
            if (!environment.StartsWith(name, StringComparison.CurrentCultureIgnoreCase))
            {
                name = Environment.NewLine + name;
                indexOfName = environment.IndexOf(name, StringComparison.CurrentCultureIgnoreCase);
                if (indexOfName == -1)
                    return null;
            }
            else
                indexOfName = 0;

            int indexOfEnd = environment.IndexOf(Environment.NewLine, indexOfName + name.Length);
            if (indexOfEnd == -1)
                indexOfEnd = environment.Length - 1;

            string value = environment.Substring(
                indexOfName + name.Length,
                indexOfEnd - (indexOfName + name.Length)
                );
            return value.Trim();
        }

        static bool EqualsIgnoreCase(string s, string value)
        {
            if (s == null)
                throw new ArgumentNullException("s");

            return s.Equals(value, StringComparison.CurrentCultureIgnoreCase);
        }

    }
}
