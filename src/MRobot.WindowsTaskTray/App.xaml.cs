using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using log4net;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]

namespace MRobot.WindowsTaskTray
{
    using System.Runtime.InteropServices;
    using System.Threading;
    using Windows.TaskTray;
    using Windows.Utilities;

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        [DllImport("user32.dll")]
        static extern bool AllowSetForegroundWindow(int dwProcessId);

        public const string MainAppProcessName = "MRobot.Windows";

        public static ILog Log = LogManager.GetLogger("App");


        private void App_OnStartup(object sender, StartupEventArgs e)
        {
            this.DispatcherUnhandledException += OnDispatcherUnhandledException;

            if (e.Args.All(arg => arg != TaskTrayShared.PreventTrayStartingAppArg))
            {
                MakeSureAppIsRunning(); 
            }

            if (e.Args.Length > 0)
            {
                VerifyAndSendCommandToApp(e.Args[0]);
            }

            if (CheckForExistingInstance())
            {
                Shutdown();
            }
            else
            {
                MainWindow = new MainWindow();
                MainWindow.Show();
            }
        }


        private static void VerifyAndSendCommandToApp(string command)
        {
            if (command.StartsWith(TaskTrayShared.UriCommandPrefix))
            {
                bool commandSent = false;
                int attempts = 0;

                AllowMainAppToSetForegroundWindow();

                do
                {
                    var t = NamedPipeChannel.SendMessageToServer(TaskTrayShared.CommandsPipeName, command);
                    t.Wait();

                    commandSent = t.Result;
                    attempts++;
                } while (!commandSent && attempts < 10);
            }
        }

        private void MakeSureAppIsRunning()
        {
            try
            {
                if (!IsDifferentProcessWithNameRunning(MainAppProcessName))
                {
                    StartMainApp();
                    Thread.Sleep(500);
                }
            }
            catch (Exception exc)
            {
                Log.Error("Attempting to start main app.", exc);
            }
        }

        private static void StartMainApp()
        {
            var shortcutName = string.Concat(Environment.GetFolderPath(Environment.SpecialFolder.Programs),
                @"\N7Software\Multiplayer Robot\Multiplayer Robot Client.appref-ms");

            Process.Start(shortcutName);
        }

        public static void AllowMainAppToSetForegroundWindow()
        {
            try
            {
                var appProcess = FindProcess(MainAppProcessName);
                if (appProcess != null)
                {
                    AllowSetForegroundWindow(appProcess.Id);
                }
            }
            catch (Exception exc)
            {
                Log.Debug("Allowing app to set foreground window.", exc);
            }
        }

        private bool CheckForExistingInstance()
        {
            return IsDifferentProcessWithNameRunning(Process.GetCurrentProcess().ProcessName);
        }

        private bool IsDifferentProcessWithNameRunning(string processName)
        {
            Process process = FindProcess(processName);

            return process != null;
        }

        private static Process FindProcess(string processName)
        {
            Process me = Process.GetCurrentProcess();

            foreach (var proc in Process.GetProcessesByName(processName))
            {
                if (proc.Id != me.Id && proc.SessionId == me.SessionId)
                {
                    return proc;
                }
            }

            return null;
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs args)
        {
            Log.Error("Unhandled Exception!", args.Exception);
        }
    }
}
