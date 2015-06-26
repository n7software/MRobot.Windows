using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MRobot.Windows.Settings
{
    public class LocalSettings : FileSystemSettings
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

        public bool HasCopiedLegacySettings
        {
            get { return GetValue<bool>("HasCopiedLegacySettings", false); }
            set { SetValue<bool>("HasCopiedLegacySettings", value); }
        }

        public string WebsiteBaseUrl
        {
            get { return GetValue("WebsiteBaseUrl", "https://new.multiplayerrobot.com"); }
            set { SetValue("WebsiteBaseUrl", value); }
        }
        
        public int InstalledStartMenuShortcutVersion
        {
            get { return GetValue<int>("InstalledStartMenuShortcutVersion", 0); }
            set { SetValue<int>("InstalledStartMenuShortcutVersion", value); }
        }

        public string LastUpdateVersion
        {
            get { return GetValue("LastUpdateVersion", string.Empty); }
            set { SetValue("LastUpdateVersion", value); }
        }

        #endregion
        
        #region Constructor
        public LocalSettings(string settingsFilePath)
            : base(settingsFilePath)
        {

        } 
        #endregion
    }
}
