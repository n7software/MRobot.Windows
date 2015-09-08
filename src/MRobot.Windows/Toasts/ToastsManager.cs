using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MRobot.Windows.Utilities;

namespace MRobot.Windows.Toasts
{
    public class ToastsManager : IToastMaker
    {
        #region Fields

        private static IToastMaker InternalToastMaker;

        #endregion

        #region Constructors
        public ToastsManager(string applicationIconPath)
        {
            InitializeInternalToastMaker(applicationIconPath);
        }

        public ToastsManager(string applicationIconPath, string defaultImageFilePath)
            : this(applicationIconPath)
        {
            DefaultToastImageFilePath = defaultImageFilePath;
        } 
        #endregion

        #region Properties
        public string DefaultToastImageFilePath { get; set; }
        #endregion

        #region Public Methods

        public void ShowToast(
            string textHeader,
            string textBody,
            Action toastActivated)
        {
            ShowToast(textHeader, textBody, toastActivated, null, null);
        }

        public void ShowToast(
            string textHeader,
            string textBody)
        {
            ShowToast(textHeader, textBody, null, null, null);
        }

        public void ShowToast(
            string textHeader,
            string textBody,
            string imageFilePath,
            Action toastActivated)
        {
            ShowToast(textHeader, textBody, imageFilePath, toastActivated, null, null);
        }

        public void ShowToast(
            string textHeader,
            string textBody,
            Action toastActivated,
            Action toastDismissed,
            Action toastTimedOut)
        {
            ShowToast(textHeader, textBody, DefaultToastImageFilePath, toastActivated, toastDismissed, toastTimedOut);
        }

        public void ShowToast(
            string textHeader,
            string textBody,
            string imageFilePath, 
            Action toastActivated, 
            Action toastDismissed,
            Action toastTimedOut)
        {
            InternalToastMaker.ShowToast(textHeader, textBody, imageFilePath, toastActivated, toastDismissed, toastTimedOut);
        }

        #endregion

        #region Private Methods

        private static void InitializeInternalToastMaker(string applicationIconPath)
        {
            if (InternalToastMaker == null)
            {
                bool isWin8 = OperatingSystemChecker.IsWindows8OrLater();

                InternalToastMaker = isWin8
                        ? new WindowsApiToastMaker(applicationIconPath,
                            App.LocalSettings.InstalledStartMenuShortcutVersion != App.StartMenuShortcutVersion)
                        : (IToastMaker)new WpfToastMaker();

                if (isWin8)
                {
                    App.LocalSettings.InstalledStartMenuShortcutVersion = App.StartMenuShortcutVersion;
                }
            }
        }

        #endregion
    }
}
