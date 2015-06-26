using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MRobot.Windows.Utilities
{
    using System.Windows;
    using System.Windows.Controls;
    using Models;

    public class PopupMenuHeaderTemplateSelector : DataTemplateSelector
    {
        public DataTemplate StandardTemplate { get; set; }

        /// <summary>
        /// When overridden in a derived class, returns a <see cref="T:System.Windows.DataTemplate"/> based on custom logic.
        /// </summary>
        /// <returns>
        /// Returns a <see cref="T:System.Windows.DataTemplate"/> or null. The default value is null.
        /// </returns>
        /// <param name="item">The data object for which to select the template.</param><param name="container">The data-bound object.</param>
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var menuItem = item as MenuItemModel;
            if (menuItem != null)
            {
                return menuItem.CustomHeaderTemplate ?? StandardTemplate;
            }

            return null;
        }
    }
}
