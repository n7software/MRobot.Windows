using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MRobot.Windows.Models
{
    public class LocalSaveFile
    {
        public int GameId { get; set; }
        public DateTime DownloadedAt { get; set; }
        public string PathOnDisk { get; set; }
    }
}
