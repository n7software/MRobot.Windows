using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Microsoft.Win32;
using MRobot.Windows.Utilities;

namespace MRobot.Windows.TaskTray
{
    public class TaskTrayManager
    {
        private const string TaskTrayProcessName = "MRobot.WindowsTaskTray";
        private const string TaskTrayExecutableName = TaskTrayProcessName + ".exe";

        private CancellationTokenSource _watchDogCts;
        private ILog Log = LogManager.GetLogger("TaskTrayManager");

        private List<string> _taskTrayDependantFileNames = new List<string>
        {
            "log4net.dll",
            "Hardcodet.Wpf.TaskbarNotification.dll",
            TaskTrayExecutableName + ".config"
        };


        public void StartTaskTray()
        {
            if (_watchDogCts != null)
            {
                _watchDogCts.Cancel();
            }

            _watchDogCts = new CancellationTokenSource();

            var cancelToken = _watchDogCts.Token;

            Task.Run(() =>
            {
                try
                {
                    while (!cancelToken.IsCancellationRequested)
                    {
                        if (!IsTaskTrayRunning())
                        {
                            LaunchTaskTray();
                        }

                        Task.Delay(1000, cancelToken).Wait(cancelToken);
                    }
                }
                catch (OperationCanceledException)
                {
                }
            },
            cancelToken);
        }

        public void CopyTrayExecutable()
        {
            try
            {
                string sourceFilePath = Path.Combine(App.GetExecutingPath(), TaskTrayExecutableName);
                string targetFilePath = GetTrayExecutableFilePath();

                KillTaskTray();

                if (File.Exists(targetFilePath))
                {
                    WaitForTargetFileToBeFree(targetFilePath);
                    File.Delete(targetFilePath);
                }

                File.Copy(sourceFilePath, targetFilePath, true);

                CopyDependantAssemblies();
            }
            catch (Exception exc)
            {
                Log.Error("Copying tray executable.", exc);
            }
        }

        public void CreateUriScheme()
        {
            try
            {
                var appKey = Registry.CurrentUser.CreateSubKey(@"Software\Classes\mrobot");
                appKey.SetValue(string.Empty, "URL:mrobot Protocol");
                appKey.SetValue("URL Protocol", string.Empty);

                appKey.CreateSubKey("DefaultIcon").SetValue(string.Empty, TaskTrayExecutableName);

                appKey.CreateSubKey("Shell")
                    .CreateSubKey("Open")
                        .CreateSubKey("Command")
                            .SetValue(string.Empty, string.Format("\"{0}\" \"%1\"", GetTrayExecutableFilePath()));
            }
            catch (Exception exc)
            {
                Log.Debug("Creating URI Scheme.", exc);
            }
        }

        public void KillTaskTray()
        {
            try
            {
                if (_watchDogCts != null)
                {
                    _watchDogCts.Cancel();
                    _watchDogCts = null;
                }

                Process trayProcess = FindTrayProcess();
                if (trayProcess != null)
                {
                    // Attempt to close the tray gracefully
                    NamedPipeChannel.SendMessageToServer(TaskTrayShared.StatusPipeName, TaskTrayStatus.Close.ToString());
                    Task.Delay(1000).Wait();
                    
                    // If it's still running, kill it
                    if (!trayProcess.HasExited)
                    {
                        trayProcess.Kill();
                    }
                }
            }
            catch (Exception exc)
            {
                Log.Debug("Killing task tray.", exc);
            }
        }
        

        private void CopyDependantAssemblies()
        {
            string sourceDirPath = App.GetExecutingPath();
            string targetDirPath = GetTaskTrayDirectoryPath();

            foreach (var fileName in _taskTrayDependantFileNames)
            {
                File.Copy(
                    Path.Combine(sourceDirPath, fileName),
                    Path.Combine(targetDirPath, fileName),
                    true);
            }
        }

        private string GetTaskTrayDirectoryPath()
        {
            string dirPath = Path.Combine(TaskTrayShared.GetAppDataDirectoryPath(), "tray");
            Directory.CreateDirectory(dirPath);
            return dirPath;
        }

        private void WaitForTargetFileToBeFree(string targetFilePath)
        {
            using (var file = FileOpenUtil.TryGetStream(targetFilePath, FileAccess.Write))
            {
                file.Close();
            }
        }

        private void LaunchTaskTray()
        {
            try
            {
                var trayProcessPath = GetTrayExecutableFilePath();

                Process.Start(
                    new ProcessStartInfo(trayProcessPath)
                    {
                        UseShellExecute = true,
                        Arguments = TaskTrayShared.PreventTrayStartingAppArg
                    });
            }
            catch (Exception exc)
            {
                Log.Debug("Launching task tray process.", exc);
            }
        }

        private string GetTrayExecutableFilePath()
        {
            return Path.Combine(GetTaskTrayDirectoryPath(), TaskTrayExecutableName);
        }

        private bool IsTaskTrayRunning()
        {
            try
            {
                return FindTrayProcess() != null;
            }
            catch (Exception exc)
            {
                Log.Debug("Checking for tray process.", exc);
                return false;
            }
        }

        private static Process FindTrayProcess()
        {
            Process me = Process.GetCurrentProcess();

            foreach (var proc in Process.GetProcessesByName(TaskTrayProcessName))
            {
                if (proc.SessionId == me.SessionId)
                {
                    return proc;
                }
            }
            
            return null;
        }
    }
}
