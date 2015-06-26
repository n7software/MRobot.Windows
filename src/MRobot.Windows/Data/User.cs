using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MRobot.Windows.Data
{
    public class User
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string SteamProfileUrl { get; set; }
        public string AvatarSmallUrl { get; set; }
        public string AvatarMediumUrl { get; set; }
        public string AvatarLargeUrl { get; set; }
        public int AccountTypeInt { get; set; }
        public int Points { get; set; }
        public string Rank { get; set; }
        public DateTime LastLogin { get; set; }
        public DateTime FirstLogin { get; set; }
    }
}
