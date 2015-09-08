using System;
using System.Collections.Generic;

namespace MRobot.Windows.Utilities
{
    public interface IFileSystemWatcher
    {
        List<string> ExtensionWhiteList { get; set; }
        string PathToWatch { get; set; }
        bool WatchSubdirectories { get; set; }

        event Action<string> ItemAdded;
        event Action<string> ItemModified;
        event Action<string> ItemRemoved;

        void Start();
        void Stop();
    }
}