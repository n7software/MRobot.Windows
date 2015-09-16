using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MRobot.Windows.GameLogic.Apps
{
    public class CivAppControllerFactory
    {
        public ICivAppController GetAppControllerByGameType(int gameType)
        {
            switch (gameType)
            {
                case 0:
                    return new CivFiveController();

                case 1:
                    return new CivBeyondEarthController();

                default:
                    throw new InvalidOperationException($"Invalid GameType provided: {gameType}");
            }
        }
    }
}
