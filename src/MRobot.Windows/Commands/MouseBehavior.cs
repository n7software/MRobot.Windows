namespace MRobot.Windows.Commands
{
    using System.Windows;
    using System.Windows.Input;

    public class MouseBehaviour
    {
        public static readonly DependencyProperty MouseUpCommandProperty =
            DependencyProperty.RegisterAttached("MouseUpCommand", typeof(ICommand),
            typeof(MouseBehaviour), new FrameworkPropertyMetadata(
            new PropertyChangedCallback(MouseUpCommandChanged)));

        public static readonly DependencyProperty MouseUpCommandParameterProperty =
            DependencyProperty.RegisterAttached("MouseUpCommandParameter", typeof(object),
            typeof(MouseBehaviour));

        private static void MouseUpCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            FrameworkElement element = (FrameworkElement)d;

            element.MouseUp += new MouseButtonEventHandler(element_MouseUp);
        }

        static void element_MouseUp(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;

            FrameworkElement element = (FrameworkElement)sender;
            ICommand command = GetMouseUpCommand(element);
            object commandParameter = GetMouseUpCommandParameter(element);
            command.Execute(commandParameter ?? e);
        }

        public static void SetMouseUpCommand(UIElement element, ICommand value)
        {
            element.SetValue(MouseUpCommandProperty, value);
        }

        public static ICommand GetMouseUpCommand(UIElement element)
        {
            return (ICommand)element.GetValue(MouseUpCommandProperty);
        }

        public static void SetMouseUpCommandParameter(UIElement element, object value)
        {
            element.SetValue(MouseUpCommandParameterProperty, value);
        }

        public static object GetMouseUpCommandParameter(UIElement element)
        {
            return element.GetValue(MouseUpCommandParameterProperty);
        }
    }
}
