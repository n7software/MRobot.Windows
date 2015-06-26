using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MRobot.Windows.Models
{
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;

    public class MenuItemModel : ModelBase
    {
        private bool isSelected;
        private List<Link> links;
        private int count;

        public string Name { get; set; }

        public Visual IconVisual { get; set; }

        public string MainLinkUrl { get; set; }

        public int Count
        {
            get { return count; }
            set
            {
                count = value;
                OnPropertyChanged("Count");
                OnPropertyChanged("ShowCount");
            }
        }

        public bool ShowCount
        {
            get { return Count > 0; }
        }

        public List<Link> Links
        {
            get { return links; }
            set
            {
                links = value; 
                OnPropertyChanged("Links");
            }
        }

        public DataTemplate CustomContentTemplate { get; set; }
        public DataTemplate CustomHeaderTemplate { get; set; }

        public int Position { get; set; }

        public bool IsSelected
        {
            get { return isSelected; }
            set
            {
                isSelected = value;
                OnPropertyChanged("IsSelected");
            }
        }

        public MenuItemModel()
        {
            Links = new List<Link>();
        }
    }
}
