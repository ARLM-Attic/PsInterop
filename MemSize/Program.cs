using System;
using System.Collections.Generic;
using System.Text;

using InSolve.dmach.PsInterop;

namespace MemSize
{
    class Program
    {
        static int Main(string[] args)
        {
            bool reqHelp = false;
            bool reqFree = false;
            bool reqTotal = false;

            foreach (string arg in args)
            {
                string p = arg.ToLower().Replace("-", "").Replace("/", "").Trim();
                if (p == "?" || p == "help" || p == "h")
                {
                    reqHelp = true;
                    break;
                }
                else if (p == "t" || p == "total")
                {
                    reqTotal = true;
                }
                else if (p == "f" || p == "free")
                {
                    reqFree = true;
                }
                else
                {
                    Console.WriteLine("Unknown argument - '" + p + "', try --help for help");
                    return -1;
                }
            }

            if (reqHelp)
            {
                Console.WriteLine(@"MemSize.exe (base on PsInterop - http://www.codeplex.com/PsInterop)
-help -h -?    --- this help
-t -total      --- total in mbytes
-f -free       --- free in mbytes

default arguments: -f -t
");

                return 0;
            }

            if (reqTotal == false && reqFree == false)
            {
                reqTotal = true;
                reqFree = true;
            }

            ulong memTotal;
            ulong memFree;

            try
            {
                Win32Wrapper.GetMemorySize(out memTotal, out memFree);
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.ToString());
                return -1;
            }

            if (reqFree)
            {
                Console.Write(memFree / 1024 / 1024);
                if (reqTotal)
                    Console.Write("/");
            }

            if (reqTotal)
                Console.Write(memTotal / 1024 / 1024);

            Console.WriteLine();

            return 0;
        }
    }
}
