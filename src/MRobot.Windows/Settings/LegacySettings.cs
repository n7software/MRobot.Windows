using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Xml.Linq;
using log4net;
using MRobot.Windows.Settings;

namespace MRobot.Windows.Utilities
{
    public class LegacySettings : FileSystemSettings
    {
        #region Properties
        public string AuthenticationKey
        {
            get
            {
                return GetEncryptedValue("AuthenticationKey");
            }
            set
            {
                SetEncryptedValue("AuthenticationKey", value ?? string.Empty);
            }
        }

        public List<string> PreviousAuthenticationKeys
        {
            get
            {
                var joinedValues = GetEncryptedValue("PreviousAuthenticationKeys");

                return new List<string>(joinedValues.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
            }
            set
            {
                if (value != null)
                {
                    var joinedValues = string.Join(",", value);

                    SetEncryptedValue("PreviousAuthenticationKeys", joinedValues);
                }
            }
        }

        public bool DisplayedToolTrayNotice
        {
            get { return GetValue<bool>("DisplayedToolTrayNotice", false); }
            set { SetValue<bool>("DisplayedToolTrayNotice", value); }
        }

        public bool DisplayExitWarning
        {
            get { return GetValue<bool>("DisplayExitWarning", true); }
            set { SetValue<bool>("DisplayExitWarning", value); }
        }

        public bool StartMinimized
        {
            get { return GetValue<bool>("StartMinimized", false); }
            set { SetValue<bool>("StartMinimized", value); }
        }

        public bool AutoDownloadSaveFiles
        {
            get 
            {
                return false; // Disabled this feature in an attempt to reduce server load
                return GetValue<bool>("AutoDownloadSaveFiles", true); 
            }
            set { SetValue<bool>("AutoDownloadSaveFiles", value); }
        }

        public bool CloseCivOnSaveFileChanges
        {
            get { return GetValue<bool>("CloseCivOnSaveFileChanges", false); }
            set { SetValue<bool>("CloseCivOnSaveFileChanges", value); }
        }

        public int CloseCivOnConditionInt
        {
            get { return GetValue<int>("CloseCivOnConditionInt", 2); }
            set { SetValue<int>("CloseCivOnConditionInt", value); }
        }
        public CloseCivCondition CloseCivOnCondition
        {
            get { return (CloseCivCondition)CloseCivOnConditionInt; }
        }

        public int CivDirectXVersionInt
        {
            get { return GetValue<int>("CivDirectXVersionInt", 0); }
            set { SetValue<int>("CivDirectXVersionInt", value); }
        }
        public CivDxVersion CivDirectXVersion
        {
            get { return (CivDxVersion)CivDirectXVersionInt; }
        }

        public bool RememberDxVersion
        {
            get { return GetValue<bool>("RememberDxVersion", false); }
            set { SetValue<bool>("RememberDxVersion", value); }
        }

        public bool LaunchCivAfterSaveDownload
        {
            get { return GetValue<bool>("LaunchCivAfterSaveDownload", true); }
            set { SetValue<bool>("LaunchCivAfterSaveDownload", value); }
        }

        public bool ArchiveSubmittedSaveFiles
        {
            get { return GetValue<bool>("ArchiveSubmittedSaveFiles", true); }
            set { SetValue<bool>("ArchiveSubmittedSaveFiles", value); }
        }

        public bool PlaySoundWhenNotifyingNewTurn
        {
            get { return GetValue<bool>("PlaySoundWhenNotifyingNewTurn", false); }
            set { SetValue<bool>("PlaySoundWhenNotifyingNewTurn", value); }
        }

        public bool ShowNotificationOfNewTurn
        {
            get { return GetValue<bool>("ShowNotificationOfNewTurn", true); }
            set { SetValue<bool>("ShowNotificationOfNewTurn", value); }
        }

        public int RepeatNotificationIntervalSeconds
        {
            get { return GetValue<int>("RepeatNotificationIntervalSeconds", 900); }
            set { SetValue<int>("RepeatNotificationIntervalSeconds", value); }
        }

        public bool HideWhenMinimized
        {
            get { return GetValue<bool>("HideWhenMinimized", true); }
            set { SetValue<bool>("HideWhenMinimized", value); }
        }
        
        public double MainWindowWidth
        {
            get { return GetValue<double>("MainWindowWidth", 500.0); }
            set { SetValue<double>("MainWindowWidth", value); }
        }

        public double MainWindowHeight
        {
            get { return GetValue<double>("MainWindowHeight", 260.0); }
            set { SetValue<double>("MainWindowHeight", value); }
        }

        public double MainWindowTop
        {
            get { return GetValue<double>("MainWindowTop", (System.Windows.SystemParameters.FullPrimaryScreenHeight / 2)); }
            set { SetValue<double>("MainWindowTop", value); }
        }

        public double MainWindowLeft
        {
            get { return GetValue<double>("MainWindowLeft", (System.Windows.SystemParameters.FullPrimaryScreenWidth / 2)); }
            set { SetValue<double>("MainWindowLeft", value); }
        }
        
        public bool LogTraceStatements
        {
            get { return GetValue<bool>("LogTraceStatements", false); }
            set { SetValue<bool>("LogTraceStatements", value); }
        }

        public bool UseLegacyFileTransfer
        {
            get 
            {
                return false; // Disabled this setting as the implementation for it has been deprecated
                return GetValue<bool>("UseLegacyFileTransfer", false); 
            }
            set { SetValue<bool>("UseLegacyFileTransfer", value); }
        }

        public bool ShowPointsNotification
        {
            get { return GetValue<bool>("ShowPointsNotification", true); }
            set { SetValue<bool>("ShowPointsNotification", value); }
        }

        #endregion

        #region Constructor
        public LegacySettings(string settingsFilePath)
            : base(settingsFilePath)
        {
        }
        #endregion
    }
}
