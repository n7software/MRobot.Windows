using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MRobot.Windows.Data;

namespace MRobot.Windows.Settings
{
    public class SyncedSettingsManager
    {
        private readonly string _localMachineId = "TODO";
        private DesktopSetting _globalSettings;
        private readonly Dictionary<string, DesktopSetting> _desktopSettings = new Dictionary<string, DesktopSetting>();

        public SyncedSettingsManager()
        {
            LoadAllSettings();
            App.UserHub.DesktopSettingsUpdated += OnDesktopSettingsUpdated;
        }

        private async void LoadAllSettings()
        {
            IEnumerable<DesktopSetting> allDesktopSettings = await App.UserHub.GetDesktopSettings();

            foreach (var desktopSetting in allDesktopSettings)
            {
                CacheDesktopSetting(desktopSetting);
            }

            UpdateEffectiveSettings();
        }

        private void UpdateEffectiveSettings()
        {
            lock (_desktopSettings)
            {
                if (_desktopSettings.ContainsKey(_localMachineId))
                {
                    App.SyncedSettings = _desktopSettings[_localMachineId];
                }
                else
                {
                    App.SyncedSettings = _globalSettings ?? App.SyncedSettings;
                }
            }
        }

        private void OnDesktopSettingsUpdated(DesktopSetting desktopSetting)
        {
            CacheDesktopSetting(desktopSetting);
        }

        private void CacheDesktopSetting(DesktopSetting desktopSetting)
        {
            if (string.IsNullOrEmpty(desktopSetting.MachineId))
            {
                _globalSettings = desktopSetting;
            }
            else
            {
                lock (_desktopSettings)
                {
                    _desktopSettings[desktopSetting.MachineId] = desktopSetting;
                }
            }
        }
    }
}
