﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using MRobot.Windows.Data;

namespace MRobot.Windows.Hubs
{
    public class UserHub : BaseHub
    {
        #region Constructor

        public UserHub(HubConnection connection)
            : base("UserHub", connection)
        {
            HubProxy.On<User>("userUpdated", user => UserUpdated(user));
            HubProxy.On<DesktopSetting>("desktopSettingsUpdated", setting => DesktopSettingsUpdated(setting));
        }

        #endregion

        #region Events

        public event Action<User> UserUpdated = _ => { };
        public event Action<DesktopSetting> DesktopSettingsUpdated = _ => { };

        #endregion

        #region Public Methods
        public async Task<AuthenticationResult> Authenticate(string authKey)
        {
            return await HubProxy.Invoke<AuthenticationResult>("Authenticate", authKey);
        }

        public void RefreshCurrentUserInfo()
        {
            HubProxy.Invoke("RefreshCurrentUser");
        }

        public async Task<long> GetCurrentUserId()
        {
            return await HubProxy.Invoke<long>("GetCurrentUserId");
        }

        public async Task<IEnumerable<DesktopSetting>> GetDesktopSettings()
        {
            return await HubProxy.Invoke<IEnumerable<DesktopSetting>>("GetDesktopSEttings");
        }
        #endregion
    }
}
