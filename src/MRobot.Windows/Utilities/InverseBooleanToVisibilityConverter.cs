namespace MRobot.Windows.Utilities
{
    using System.Windows;

    /// <summary>
    /// 
    /// </summary>
    public sealed class InverseBooleanToVisibilityConverter : BooleanConverter<Visibility>
    {
        /// <summary>
        /// 
        /// </summary>
        public InverseBooleanToVisibilityConverter() : base(Visibility.Collapsed, Visibility.Visible)
        {            
        }
    }
}
