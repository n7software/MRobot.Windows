using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MRobot.Windows.Utilities
{
    public static class OperatingSystemChecker
    {
        public static bool IsWindows8OrLater()
        {
            var win8Version = new Version(6, 2, 9200, 0);

            return Environment.OSVersion.Platform == PlatformID.Win32NT
                && Environment.OSVersion.Version >= win8Version;
        }
    }
}
