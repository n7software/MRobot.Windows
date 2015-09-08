using System;
using System.IO;

namespace MRobot.Windows.TaskTray
{
    public static class TaskTrayShared
    {
#if LOCAL
        public const string WebsiteUrl = "http://localhost:22945";
#else
        public const string WebsiteUrl = "https://new.multiplayerrobot.com";
#endif

        public const string CommandsPipeName = "MRobot.Windows.TaskTrayCommandsPipe";
        public const string StatusPipeName = "MRobot.Windows.TaskTrayStatusPipe";

        public const string PreventTrayStartingAppArg = "-nocheck";

        public const string ExitCommand = "ExitClicked";
        public const string OpenCommand = "OpenClicked";
        
        public const string UriCommandPrefix = "mrobot://";
        public const string UriAuthKeyCommand = "authkey/";
        public const string UriLaunchCivCommand = "launchCiv/";
        
        public static string GetAppDataDirectoryPath()
        {
            var dataDirectoryPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "MRobot");

            Directory.CreateDirectory(dataDirectoryPath);
            return dataDirectoryPath;
        }
    }

    public enum TaskTrayStatus
    {
        Close = -1,
        Normal = 0,
        Alert = 1
    }
}
