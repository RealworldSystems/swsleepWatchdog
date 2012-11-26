using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Management;
using System.ComponentModel;

namespace swsleepWatchdog
{
    class Program
    {
        private static int watchdogTimeoutInSeconds = 60;

        static void Main(string[] args)
        {
            Report("version 0.4");
            Report("invoke, entering infinite loop (timeout={0} seconds)", watchdogTimeoutInSeconds);
            while (true)
            {
                foreach (Process p in Process.GetProcesses())
                {
                    String s = p.ProcessName;
                    DateTime endTime;
                    if (s == "swsleep")
                    {
                        Report("found a swsleep.exe process: PID=={0}, cmd=={1}",
                            p.Id, p.ProcessName);

                        float timeout = 0;
                        try
                        {
                            timeout = GetSwsleepTimeout(p);
                            endTime = p.StartTime + new TimeSpan(0, 0, (int)timeout);
                        }
                        catch (FormatException fe)
                        {
                            Report("FormatException: something went wrong retrieving the timeout: {0}",
                                fe.Message);
                            break;
                        }
                        catch (IndexOutOfRangeException)
                        {
                            Report("IndexOutOfRangeException: most likely the process already terminated");
                            break;
                        }
                        catch (InvalidOperationException)
                        {
                            Report("InvalidOperationException: most likely the process already terminated");
                            break;
                        }
                        Report("timeout: {0}", timeout);

                        Report("calculated end time {0}", endTime);

                        if (endTime < DateTime.Now)
                        {
                            Report("swsleep.exe should have been ended, killing it");
                            try
                            {
                                p.Kill();
                            }
                            catch (Win32Exception)
                            {
                                Report("Win32Exception: killing the process failed, most likely, the process is terminating (according to the MSDN docs");
                                break;
                            }
                        }
                        else
                        {
                            Report("swsleep.exe has an end time in the future, keeping it running");
                        }
                    }
                }

                Thread.Sleep(watchdogTimeoutInSeconds * 1000);

                Console.Write("."); Console.Out.Flush();
            }
        }

        private static void Report(string msg, params Object[] args)
        {
            Console.Write("{0}: ", DateTime.Now);
            Console.WriteLine("swsleepWatchdog: " + msg, args);
        }

        /// <summary>
        /// Retrieves the process timeout from swsleep.exe's argument list
        /// </summary>
        /// <param name="swSleepProcess">A swsleep.exe process</param>
        /// <returns>the time out of the swsleep.exe process</returns>
        private static float GetSwsleepTimeout(Process swSleepProcess)
        {
            string args = GetProcessArgs(swSleepProcess);
            string[] argv = args.Split(new char[] {' '}, StringSplitOptions.RemoveEmptyEntries);
            string arg1 = argv[1];
            float timeout = float.Parse(arg1);

            return timeout;
        }

        /// <summary>
        /// Retrieve the arguments of the give process.
        /// 
        /// <remarks>
        /// NOTE: Process.StartInfo.Arguments is always empty, this stackoverflow
        /// (<seealso cref="http://stackoverflow.com/questions/2633628/can-i-get-command-line-arguments-of-other-processes-from-net-c"/>)
        /// question advises to use WMI
        /// </remarks>
        /// </summary>
        /// <param name="p">Any process running</param>
        /// <returns>The entire argument list of the process, including arg0</returns>
        private static string GetProcessArgs(Process p)
        {
            string args = "";
            try
            {
                using (ManagementObjectSearcher searcher =
                    new ManagementObjectSearcher("SELECT CommandLine FROM Win32_Process WHERE ProcessId = " + p.Id))
                {
                    foreach (ManagementObject @object in searcher.Get())
                    {
                        args += @object["CommandLine"];
                    }
                }
            }
            catch (Win32Exception ex)
            {
                if ((uint)ex.ErrorCode != 0x80004005)
                {
                    throw;
                }
            }
            return args;
        }
    }
}
