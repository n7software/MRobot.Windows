using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MRobot.Windows.Data
{
    public class Game
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Type { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public bool HasMods { get; set; }
        public bool ShowInPublic { get; set; }
        public int JoinRankFilter { get; set; }
        public int? ThreadId { get; set; }
        public int Slotlimit { get; set; }

        public Turn CurrenTurn { get; set; }
        public Turn LastTurn { get; set; }
        public List<GameSlot> GameSlots { get; set; }

        public bool IsCurrentUserTurnAndNotSubmitted()
        {
            return IsCurrentUserTurn() && CurrenTurn.SubmittedAt == null;
        }

        public bool IsCurrentUserTurn()
        {
            return GameSlots != null
                   && CurrenTurn != null
                   && GameSlots.Any(gs => gs.Id == CurrenTurn.SlotId && gs.AllUserIds.Contains(App.CurrentUserId));
        }
    }
}
