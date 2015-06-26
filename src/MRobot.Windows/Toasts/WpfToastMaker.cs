using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MRobot.Windows.Toasts
{
    using System.Media;

    public class WpfToastMaker : IToastMaker
    {
        public void ShowToast(
            string textHeader,
            string textBody,
            string imageFilePath,
            Action toastActivated,
            Action toastDismissed,
            Action toastTimedOut)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                var toastWindow = new WpfToastWindow(textHeader, textBody, imageFilePath);

                if (toastActivated != null)
                {
                    toastWindow.UserActivated += toastActivated;
                }

                if (toastDismissed != null)
                {
                    toastWindow.Dismissed += toastDismissed;
                }

                if (toastTimedOut != null)
                {
                    toastWindow.TimedOut += toastTimedOut;
                }

                toastWindow.Show();

            }));
        }
    }
}
