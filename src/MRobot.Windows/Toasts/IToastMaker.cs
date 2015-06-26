using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MRobot.Windows.Toasts
{
    public interface IToastMaker
    {
        void ShowToast(
            string textHeader,
            string textBody,
            string imageFilePath,
            Action toastActivated,
            Action toastDismissed,
            Action toastTimedOut);
    }
}
