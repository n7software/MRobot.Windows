using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MRobot.Windows.Data
{
    public enum AutoCloseCivSettings : int
    {
        Never = 0,
        NewSaveDetected = 1,
        NewSaveDetectedNoOtherSaves = 2
    }
}
