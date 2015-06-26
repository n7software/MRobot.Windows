using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MRobot.Windows.Models
{
    using CivilizationV.Save;

    public class LocalGameSave
    {
        public GameSave GameSave { get; set; }
        public string LocalFilePath { get; set; }

        public LocalGameSave(GameSave gameSave, string filePath)
        {
            GameSave = gameSave;
            LocalFilePath = filePath;
        }
    }
}
