using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MRobot.Windows.Data
{
    public class GameSlot
    {
        public int Id { get; set; }
        public int GameId { get; set; }
        public int TurnOrder { get; set; }
        public int? Difficulty { get; set; }
        public string PrimaryUserId { get; set; }
        public List<long> AllUserIds { get; set; }
        public int TimesSkipped { get; set; }
        public int? AverageTurnSeconds { get; set; }
        public int CivId { get; set; }

        public List<User> Users { get; set; }
    }
}
