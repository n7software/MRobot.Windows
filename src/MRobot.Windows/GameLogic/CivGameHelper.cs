using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using MRobot.Windows.Settings;

namespace MRobot.Windows.GameLogic
{
    public static class CivGameHelper
    {
        private static readonly ILog Log = LogManager.GetLogger("CivGameHelper");

        public static bool LaunchGame()
        {
            try
            {
                var process = new Process();
                process.StartInfo.FileName = "steam://rungameid/8930" + GetDxVersionParam();
                process.StartInfo.UseShellExecute = true;
                process.Start();

                return true;
            }
            catch (Exception exc)
            {
                Log.Error("Launching Civ V", exc);
                return false;
            }
        }

        private static string GetDxVersionParam()
        {
            string dxParam = "//%5C";

            switch (App.LocalSettings.CivDirectXVersion)
            {
                case CivDxVersion.Dx9:
                    dxParam += "dx9";
                    break;

                case CivDxVersion.Dx11:
                    dxParam += "dx11";
                    break;

                case CivDxVersion.DxWin8:
                    dxParam += "win8";
                    break;
            }

            return dxParam;
        }
    }
}
