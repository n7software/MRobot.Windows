using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MRobot.Windows.Models;

namespace MRobot.Windows.Utilities
{
    public class LinkTemplateSelector : DataTemplateSelector
    {
        public DataTemplate PlainLink { get; set; }
        public DataTemplate ActionLink { get; set; }
        public DataTemplate GameLink { get; set; }
        public DataTemplate SaveTransfer { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is GameLink)
            {
                return GameLink;
            }
            else if (item is ActionLink)
            {
                return ActionLink;
            }

            return PlainLink;
        }
    }
}
