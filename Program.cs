﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System;
using System.Management;
using System.ComponentModel;

namespace swsleepWatchdog
{
    class Program
    {
        private static int watchdogTimeoutInSeconds = 60;

        static void Main(string[] args)
        {
            Console.WriteLine("swsleepWatchdog: invoke, entering infinite loop");
            while (true)
            {
                foreach (Process p in Process.GetProcesses())
                {
                    String s = p.ProcessName;
                    if (s == "swsleep")
                    {
                        Console.WriteLine("swsleepWatchdog: found a swsleep.exe process: PID=={0}, cmd=={1}",
                            p.Id, p.ProcessName);

                        float timeout = 0;
                        try
                        {
                            timeout = GetSwsleepTimeout(p);
                        }
                        catch (FormatException)
                        {
                            System.Console.WriteLine("swsleepWatchdog: FormatException: something went wrong retrieving the timeout");
                        }
                        catch (IndexOutOfRangeException)
                        {
                            System.Console.WriteLine("swsleepWatchdog: IndexOutOfRangeException: most likely the process already terminated");
                        }
                        catch (InvalidOperationException)
                        {
                            System.Console.WriteLine("swsleepWatchdog: InvalidOperationException: most likely the process already terminated");
                        }
                        System.Console.WriteLine("swsleepWatchdog: timeout: {0}", timeout);

                        DateTime endTime = p.StartTime + new TimeSpan(0, 0, (int)timeout);
                        System.Console.WriteLine("swsleepWatchdog: calculated end time {0}", endTime);

                        if (endTime < DateTime.Now)
                        {
                            System.Console.WriteLine("swsleepWatchdog: swsleep.exe should have been ended, killing it");
                            p.Kill();
                        }
                        else
                        {
                            System.Console.WriteLine("swsleepWatchdog: swsleep.exe has an end time in the future, keeping it running");
                        }
                    }
                }

                Console.WriteLine("swsleepWatchdog: going to sleep (timeout: {0} s.", watchdogTimeoutInSeconds);
                Thread.Sleep(watchdogTimeoutInSeconds * 1000);

                Console.WriteLine("swsleepWatchdog: woken up again");
            }
        }

        /// <summary>
        /// Retrieves the process timeout from swsleep.exe's argument list
        /// </summary>
        /// <param name="swSleepProcess">A swsleep.exe process</param>
        /// <returns>the time out of the swsleep.exe process</returns>
        private static float GetSwsleepTimeout(Process swSleepProcess)
        {
            string arg1 = GetProcessArgs(swSleepProcess).Split(' ')[1];
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