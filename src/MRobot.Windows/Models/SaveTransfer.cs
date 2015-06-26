using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MRobot.Windows.Models
{
    using System.Windows.Media;
    using Data;

    public class SaveTransfer : ModelBase
    {
        private int progressPercentage;
        private bool isFailed;
        private int secondsUntilRetry;

        public Game Game { get; set; }
        public int GameId { get { return Game.Id; }}

        public string Name { get { return Game.Name; } }

        public Visual IconVisual { get; set; }

        public bool IsFailed
        {
            get { return isFailed; }
            set
            {
                isFailed = value; 
                OnPropertyChanged("IsFailed");
            }
        }

        public int SecondsUntilRetry
        {
            get { return secondsUntilRetry; }
            set
            {
                secondsUntilRetry = value;
                OnPropertyChanged("SecondsUntilRetry");
            }
        }

        public int ProgressPercentage
        {
            get { return progressPercentage; }
            set
            {
                progressPercentage = value;
                OnPropertyChanged("ProgressPercentage");
            }
        }
    }
}
