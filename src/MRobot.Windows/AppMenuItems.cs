using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MRobot.Windows.Data;
using MRobot.Windows.TaskTray;

namespace MRobot.Windows
{
    using System.Windows;
    using System.Windows.Media;
    using Models;

    static class AppMenuItems
    {
        //For easy potential switching to 
        public readonly static string Hostname = TaskTrayShared.WebsiteUrl + "/";

        public static readonly MenuItemModel GamesMenu = new MenuItemModel
        {
            Name = "Games",
            MainLinkUrl = Hostname + "#Games",
            IconVisual = Application.Current.FindResource("GamesIcon") as Visual,
            CustomHeaderTemplate = Application.Current.FindResource("GamePopupMenuHeaderTemplate") as DataTemplate,
            Links = new List<Link>
            {
                new GameLink(new Game{Name = "Loading..."}),
                new Link {Name = "Create New", Url = Hostname + "#Games/CreateNew"},
                new Link {Name = "Find More", Url = Hostname + "#Games/Find"}
            }
        };

        public static readonly MenuItemModel TransfersMenu = new MenuItemModel
        {
            Name = "Transfers",
            IconVisual = Application.Current.FindResource("TransfersIcon") as Visual,
            CustomContentTemplate = Application.Current.FindResource("TransfersPopupTemplate") as DataTemplate
        };

        public readonly static IList<MenuItemModel> MenuItems = new List<MenuItemModel>
        {
            new MenuItemModel{ 
                Name = "Home",
                MainLinkUrl = Hostname,
                IconVisual = Application.Current.FindResource("HomeIcon") as Visual,
                Links = new List<Link>
                {
                    new Link { Name = "Blog", Url = "http://blog.multiplayerrobot.com" },
                    new Link { Name = "Facebook", Url = "http://facebook.com/MultiplayerRobot" },
                    new Link { Name = "Twitter", Url = "http://twitter.com/GMRobot" },
                    new Link { Name = "Client Settings", Url = Hostname + "#Apps/Settings" }
                }
            },

            GamesMenu,
            
            new MenuItemModel{
                Name = "Community",
                MainLinkUrl = Hostname + "#Community",
                IconVisual = Application.Current.FindResource("CommunityIcon") as Visual,
                Links = new List<Link>
                {
                    new Link { Name = "Forums", Url = Hostname + "#Community/Forums" },
                    new Link { Name = "Players", Url = Hostname + "#Community/Players" }
                }
            },
            
            new MenuItemModel{
                Name = "Account",
                MainLinkUrl = Hostname + "#Account",
                IconVisual = Application.Current.FindResource("AccountIcon") as Visual,
                Links = new List<Link>
                {
                    new Link { Name = "Profile", Url = Hostname + "#Account" },
                    new Link { Name = "Settings", Url = Hostname + "#Account/ControlPanel/Global" },
                    //new Link { Name = "Credits", Url = Hostname + "#Account/Credits" }
                }
            },
            
            new MenuItemModel{
                Name = "Help",
                MainLinkUrl = Hostname + "#Help",
                IconVisual = Application.Current.FindResource("HelpIcon") as Visual,
                Links = new List<Link>
                {
                    new Link { Name = "FAQ", Url = Hostname + "#Help/FAQ" },
                    new Link { Name = "About Us", Url = Hostname + "#Help/AboutUs" },
                    new Link { Name = "Legal", Url = Hostname + "#Help/Legal" }
                }
            },
            
            new MenuItemModel{
                Name = "Messages",
                IconVisual = Application.Current.FindResource("MessageIcon") as Visual
            },
            
            new MenuItemModel{
                Name = "Notifications",
                IconVisual = Application.Current.FindResource("NotificationIcon") as Visual
            },
            
            TransfersMenu,
        };
    }
}
