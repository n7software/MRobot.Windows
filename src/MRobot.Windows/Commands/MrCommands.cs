using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MRobot.Windows.Commands
{
    using System.Windows.Input;

    public static class MrCommands
    {
        public static readonly RoutedCommand MenuItemHeaderClick = new RoutedCommand("MenuItemHeaderClick", typeof(MrCommands));

        public static readonly RoutedCommand MenuSubLinkClick = new RoutedCommand("MenuSubLinkClick", typeof(MrCommands));

        public static readonly RoutedCommand LaunchCiv = new RoutedCommand("LaunchCiv", typeof(MrCommands));
    }
}
