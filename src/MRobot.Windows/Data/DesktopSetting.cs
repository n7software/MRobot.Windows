using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MRobot.Windows.Data
{
    public class DesktopSetting
    {
        public DesktopSetting()
        {
            ExitPromptEnabled = true;
            ArchiveSubmittedFiles = true;
            NotifyPointsEarned = true;
            NotifyNewTurn = true;
            NotifyWhenUpdated = true;
            RepeatTurnNotifyEveryMinutes = 15;
            DirectXVersionToUse = CivDirectXVersions.NotSelected;
            AutoCloseCivCondition = AutoCloseCivSettings.Never;
        }

        public int Id { get; set; }
        public long UserId { get; set; }
        public string MachineId { get; set; }
        public bool ExitPromptEnabled { get; set; }
        public bool ArchiveSubmittedFiles { get; set; }
        public bool NotifyPointsEarned { get; set; }
        public bool NotifyNewTurn { get; set; }
        public int RepeatTurnNotifyEveryMinutes { get; set; }
        public CivDirectXVersions DirectXVersionToUse { get; set; }
        public AutoCloseCivSettings AutoCloseCivCondition { get; set; }
        public bool NotifyWhenUpdated { get; set; }

        public virtual User User { get; set; }
    }
}
