﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using log4net;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.Win32;
using MRobot.Windows.Data;
using MRobot.Windows.GameLogic;
using MRobot.Windows.Hubs;
using MRobot.Windows.Settings;
using MRobot.Windows.TaskTray;
using MRobot.Windows.Toasts;
using MRobot.Windows.Utilities;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]

namespace MRobot.Windows
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        #region Fields
        
        private const string LocalSettingsFileName = "settings.xml";
        public const int StartMenuShortcutVersion = 3;

        private bool _hasCopiedAppIcon = false;

        #endregion

        #region Static Properties

        public static ILog Log { get; private set; }

        public static LocalSettings LocalSettings { get; private set; }

        public static DesktopSetting SyncedSettings { get; set; }

        public static long CurrentUserId { get; private set; }

        public static ToastsManager ToastMaker { get; private set; }
        public static GameManager GameManager { get; private set; }
        public static SyncedSettingsManager SyncedSettingsManager { get; private set; }

        public static UserHub UserHub { get; private set; }
        public static GameHub GameHub { get; private set; }

        #endregion

        private void App_OnStartup(object sender, StartupEventArgs e)
        {
            this.DispatcherUnhandledException += OnDispatcherUnhandledException;

            Log = LogManager.GetLogger("App");

            if (CheckForExistingInstance())
            {
                Shutdown();
            }
            else
            {
                InitializeLocalSettings();
                InitializeToastMaker();
                InitialiazeMenuItems();
                ConnectToServer()
                    .ContinueWith(t => AuthenticateWithServer(t.Result))
                    .ContinueWith(t => InitializeGameManager())
                    .ContinueWith(t => InitializeSyncedSettings());

                Task.Run(new Action(AttemptToSetAddRemoveProgramsIcon));
            }
        }

        private static GameManager InitializeGameManager()
        {
            GameManager = new GameManager();
            return GameManager;
        }

        private void InitializeSyncedSettings()
        {
            SyncedSettings = new DesktopSetting();
            SyncedSettingsManager = new SyncedSettingsManager();
        }

        private void InitializeToastMaker()
        {
            ToastMaker = new ToastsManager(GetAppIconPath(), Path.GetFullPath(".\\Resources\\Images\\mr-logo.png"));
        }

        public static async Task<bool> ConnectToServer()
        {
            CurrentUserId = 0;

            try
            {
                var hubConnection = new HubConnection(LocalSettings.WebsiteBaseUrl)
                {
                    TraceLevel = TraceLevels.All,
                    TraceWriter = Console.Out
                };

                UserHub = new UserHub(hubConnection);
                GameHub = new GameHub(hubConnection);

                ServicePointManager.DefaultConnectionLimit = 10;

                hubConnection.Error += exception => LogManager.GetLogger("SignalR").Error("Error", exception);

                await hubConnection.Start();

                return true;
            }
            catch (Exception exc)
            {
                CurrentUserId = -2;
                Log.Error("Creating SignalR Connection", exc);
                return false;
            }
        }

        public static async Task<bool> AuthenticateWithServer(bool isConnected = true)
        {
            if (isConnected)
            {
                CurrentUserId = 0;
                AuthenticationResult result;

                do
                {
                    result = await App.UserHub.Authenticate(LocalSettings.AuthenticationKey);

                    if (result == AuthenticationResult.Success)
                    {
                        CurrentUserId = await App.UserHub.GetCurrentUserId();
                    }
                    else if (result == AuthenticationResult.InvalidAuthKey)
                    {
                        CurrentUserId = -1;
                    }
                    else if (result == AuthenticationResult.TooManySessionCreates)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1));
                    }
                } while (result == AuthenticationResult.TooManySessionCreates);

                return result == AuthenticationResult.Success;
            }

            return false;
        }

        private void InitializeLocalSettings()
        {
            var legacySettings = CreateLegacySettings();
            CreateLocalSettings();
            CopyLegacySettings(legacySettings);
        }

        private void CopyLegacySettings(LegacySettings legacySettings)
        {
            if (legacySettings != null && !LocalSettings.HasCopiedLegacySettings)
            {
                LocalSettings.AuthenticationKey = legacySettings.AuthenticationKey;
                LocalSettings.CivDirectXVersionInt = legacySettings.CivDirectXVersionInt;
                LocalSettings.RememberDxVersion = legacySettings.RememberDxVersion;
                LocalSettings.HasCopiedLegacySettings = true;
            }
        }

        private void CreateLocalSettings()
        {
            var settingsPath = TaskTrayShared.GetAppDataDirectoryPath();

            LocalSettings = new LocalSettings(Path.Combine(settingsPath, LocalSettingsFileName));
        }

        private LegacySettings CreateLegacySettings()
        {
            var legacyPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "GMR",
                LocalSettingsFileName);

            return File.Exists(legacyPath) ? new LegacySettings(legacyPath) : null;
        }

        private void InitialiazeMenuItems()
        {
            for (int i = 0; i < AppMenuItems.MenuItems.Count; i++)
            {
                AppMenuItems.MenuItems[i].Position = i;
            }
        }

        private void AttemptToSetAddRemoveProgramsIcon()
        {
            try
            {
                RegistryKey myUninstallKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall");
                string[] mySubKeyNames = myUninstallKey.GetSubKeyNames();
                for (int i = 0; i < mySubKeyNames.Length; i++)
                {
                    RegistryKey myKey = myUninstallKey.OpenSubKey(mySubKeyNames[i], true);

                    object myValue = myKey.GetValue("DisplayName");
                    if (myValue != null && (string)myValue == "Multiplayer Robot Client")
                    {
                        myKey.SetValue("DisplayIcon", GetAppIconPath());
                        break;
                    }
                }
            }
            catch (Exception)
            {

            }
        }

        public string GetAppIconPath()
        {
            string iconFileName = "App.ico";
            string sourcePath = Path.Combine(GetExecutingPath(), iconFileName );
            string targetPath = Path.Combine(TaskTrayShared.GetAppDataDirectoryPath(), iconFileName);

            if (!_hasCopiedAppIcon)
            {
                _hasCopiedAppIcon = true;

                try
                {
                    File.Copy(sourcePath, targetPath, true);
                }
                catch (Exception exc)
                {
                    Log.Debug("Copying app icon.", exc);
                } 
            }

            return targetPath;
        }

        public static string GetExecutingPath()
        {
            try
            {
                string fullPath = Path.GetFullPath(Assembly.GetExecutingAssembly().Location);

                return Path.GetDirectoryName(fullPath);
            }
            catch (Exception exc)
            {
                Log.Debug("Getting executing path.", exc);
                return string.Empty;
            }
        }

        private bool CheckForExistingInstance()
        {
            Process existingClient = null;
            Process me = Process.GetCurrentProcess();

            foreach (var proc in Process.GetProcessesByName(me.ProcessName))
            {
                if (proc.Id != me.Id && proc.SessionId == me.SessionId)
                {
                    existingClient = proc;
                    break;
                }
            }

            return existingClient != null;
        }

        public static void Restart()
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                System.Windows.Forms.Application.Restart();
                Application.Current.Shutdown();
            }));
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs args)
        {
            if (Log != null)
            {
                Log.Error("APP EXCEPTION", args.Exception);
            }
        }
    }
}
