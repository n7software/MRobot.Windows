using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using log4net;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;
using MS.WindowsAPICodePack.Internal;

namespace MRobot.Windows.Toasts
{
    public class WindowsApiToastMaker : IToastMaker
    {
        #region Fields

        private const string AppId = "N7Software.MRobot.Windows";

#if DEBUG
        private const string ShortcutName = "Multiplayer Robot (Debug).lnk";
#else
        private const string ShortcutName = "Multiplayer Robot.lnk";
#endif

        private ILog Log = LogManager.GetLogger("WindowsApiToastMaker");

        #endregion

        #region Constructors

        public WindowsApiToastMaker(string applicationIconPath, bool overwriteExistingShortcut)
        {
            TryCreateShortcut(applicationIconPath, overwriteExistingShortcut);
        }

        #endregion

        #region Public Methods
        public void ShowToast(
            string textHeader,
            string textBody,
            string imageFilePath,
            Action toastActivated,
            Action toastDismissed,
            Action toastTimedOut)
        {
            try
            {
                XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastImageAndText02);

                var textElements = toastXml.GetElementsByTagName("text");
                if (!string.IsNullOrEmpty(textHeader))
                {
                    textElements[0].AppendChild(toastXml.CreateTextNode(textHeader));
                }
                if (!string.IsNullOrEmpty(textBody))
                {
                    textElements[1].AppendChild(toastXml.CreateTextNode(textBody));
                }

                if (!string.IsNullOrWhiteSpace(imageFilePath))
                {
                    toastXml.GetElementsByTagName("image")[0].Attributes.GetNamedItem("src").NodeValue = imageFilePath;
                }

                var toast = new ToastNotification(toastXml);

                if (toastActivated != null)
                {
                    toast.Activated += (sender, args) => toastActivated();
                }

                toast.Dismissed += (sender, args) =>
                    {
                        switch (args.Reason)
                        {
                            case ToastDismissalReason.UserCanceled:
                                if (toastDismissed != null) toastDismissed();
                                break;
                            case ToastDismissalReason.TimedOut:
                                if (toastTimedOut != null) toastTimedOut();
                                break;
                        }
                    };

                ToastNotificationManager.CreateToastNotifier(AppId).Show(toast);
            }
            catch (Exception exc)
            {
                Log.Error("Showing Win8 Toast", exc);
            }
        }
        #endregion

        #region Methods

        // In order to display toasts, a desktop application must have a shortcut on the Start menu.
        // Also, an AppUserModelID must be set on that shortcut.
        // The shortcut should be created as part of the installer. The following code shows how to create
        // a shortcut and assign an AppUserModelID using Windows APIs. You must download and include the 
        // Windows API Code Pack for Microsoft .NET Framework for this code to function
        //
        // Included in this project is a wxs file that be used with the WiX toolkit
        // to make an installer that creates the necessary shortcut. One or the other should be used.
        private bool TryCreateShortcut(string applicationIconPath, bool overwriteExistingShortcut)
        {
            try
            {
                string shortcutPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Microsoft\\Windows\\Start Menu\\Programs\\" + ShortcutName;
                if (overwriteExistingShortcut || !File.Exists(shortcutPath))
                {
                    InstallShortcut(shortcutPath, applicationIconPath);
                    return true;
                }
            }
            catch (Exception exc)
            {
                Log.Debug("Creating Windows 8 shortcut", exc);
            }
            return false;
        }

        private void InstallShortcut(string shortcutPath, string applicationIconPath)
        {
            // Find the path to the current executable
            string exePath = Process.GetCurrentProcess().MainModule.FileName;
            var newShortcut = (IShellLinkW)new CShellLink();

            // Create a shortcut to the exe
            ErrorHelper.VerifySucceeded(newShortcut.SetPath(exePath));
            ErrorHelper.VerifySucceeded(newShortcut.SetArguments(""));
            ErrorHelper.VerifySucceeded(newShortcut.SetIconLocation(applicationIconPath, 0));

            // Open the shortcut property store, set the AppUserModelId property
            var newShortcutProperties = (IPropertyStore)newShortcut;

            using (PropVariant appId = new PropVariant(AppId))
            {
                ErrorHelper.VerifySucceeded(newShortcutProperties.SetValue(SystemProperties.System.AppUserModel.ID, appId));
                ErrorHelper.VerifySucceeded(newShortcutProperties.Commit());
            }

            // Commit the shortcut to disk
            var newShortcutSave = (IPersistFile)newShortcut;

            ErrorHelper.VerifySucceeded(newShortcutSave.Save(shortcutPath, true));
        }

        private string GetIconPath()
        {
            return string.Empty;
        }

        #endregion
    }
}
