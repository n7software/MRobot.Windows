using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MRobot.Windows.GameLogic.Apps
{
    public interface ICivAppController
    {
        bool Launch();
        bool IsRunning();
        void AttemptToClose();
    }
}
