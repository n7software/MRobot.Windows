using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MRobot.Windows.TaskTray;
using MRobot.Windows.Utilities;

namespace MRobot.Windows.Models
{
    using Data;

    public class GameLink : ActionLink
    {
        private Game _game;

        public bool IsCurrentTurn => _game.IsCurrentUserTurnAndNotSubmitted();

        public int Type => _game.Type;

        public GameLink(Game game)
        {
            _game = game;
            ActionToPerform = OnGameLinkClicked;
            Name = _game.Name;
        }

        private void OnGameLinkClicked()
        {
            LauncherUtil.LaunchUrlWithDefaultBrowser(TaskTrayShared.WebsiteUrl + "/#Games/" + _game.Id);
        }
    }
}
