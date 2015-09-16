using System;
using System.Collections.Generic;
using System.Diagnostics;
using log4net;
using MRobot.Windows.Settings;
using MRobot.Windows.Utilities;

namespace MRobot.Windows.GameLogic.Apps
{
    internal class CivFiveController : ICivAppController
    {
        public const int CivVGameId = 8930;
        private const string SavedGameExtension = ".Civ5Save";
        private const string SavedGameExtensionFilter = "*.Civ5Save";

        private static readonly ILog Log = LogManager.GetLogger("CivGameHelper");

        private static readonly List<string> CivProcessNames = new List<string>
        {
            "CivilizationV",
            "CivilizationV_DX11",
            "CivilizationV_Tablet"
        };

        public bool Launch()
        {
            try
            {
                var process = new Process
                {
                    StartInfo =
                    {
                        FileName = "steam://rungameid/8930" + GetDxVersionParam(),
                        UseShellExecute = true
                    }
                };
                process.Start();

                return true;
            }
            catch (Exception exc)
            {
                Log.Error("Launching Civ V", exc);
                return false;
            }
        }

        public bool IsRunning()
        {
            try
            {
                return LauncherUtil.FindProcessByName(CivProcessNames) != null;
            }
            catch (Exception exc)
            {
                Log.Debug("Finding Civ process", exc);
                return false;
            }
        }

        public void AttemptToClose()
        {
            try
            {
                Process civProcess = LauncherUtil.FindProcessByName(CivProcessNames);
                civProcess?.Kill();
            }
            catch (Exception exc)
            {
                Log.Debug("Closing game", exc);
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
