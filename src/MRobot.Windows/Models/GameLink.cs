using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MRobot.Windows.Models
{
    using Data;

    public class GameLink : ActionLink
    {
        private Game _game;

        public bool IsCurrentTurn
        {
            get { return _game.IsCurrentUserTurnAndNotSubmitted(); }
        }

        public GameLink(Game game)
        {
            _game = game;
            ActionToPerform = OnGameLinkClicked;
            Name = _game.Name;
        }

        private void OnGameLinkClicked()
        {
            Process.Start(App.WebsiteBaseUrl + "/#Games/" + _game.Id);
        }
    }
}
