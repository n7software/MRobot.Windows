using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Xml.Linq;
using log4net;
using MRobot.Windows.Utilities;

namespace MRobot.Windows.Settings
{
    public class FileSystemSettings
    {
        #region Properties
        private string _settingsFilePath = string.Empty;

        protected ILog _log = LogManager.GetLogger("LocalSettings");

        private XElement _xml = new XElement("Settings");

        public string SettingsXmlString
        {
            get
            {
                return _xml.ToString();
            }
            set
            {
                _xml = XElement.Parse(value);
            }
        }
        #endregion

        #region Constructor
        protected FileSystemSettings(string settingsFilePath)
        {
            _settingsFilePath = settingsFilePath;

            LoadSettingsFromDisk();
        }
        #endregion

        #region Methods     

        protected void LoadSettingsFromDisk()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    _xml = XElement.Load(_settingsFilePath);
                }
            }
            catch (Exception exc)
            {
                _log.Error("Attempting to read settings from disk", exc);
            }
        }
        protected void SaveSettingsToDisk()
        {
            try
            {
                _xml.Save(_settingsFilePath);
            }
            catch (Exception exc)
            {
                _log.Error("Attempting to save settigns to disk.", exc);
            }
        }

        protected string GetValue(string name, string defaultValue)
        {
            string result = defaultValue;

            var xel = _xml.Element(name);
            if (xel != null)
            {
                result = xel.Value;
            }

            return result;
        }
        protected void SetValue(string name, string value)
        {
            var existing = _xml.Element(name);
            if (existing == null)
            {
                existing = new XElement(name);
                _xml.Add(existing);
            }

            existing.Value = value;

            SaveSettingsToDisk();
        }

        protected T GetValue<T>(string name, T defaultValue)
        {
            T result = defaultValue;

            try
            {
                var converter = TypeDescriptor.GetConverter(typeof(T));
                if (converter != null)
                {
                    return (T)converter.ConvertFromString(GetValue(name, defaultValue.ToString()));
                }
            }
            catch { }

            return result;
        }
        protected void SetValue<T>(string name, T value)
        {
            SetValue(name, value.ToString());
        }

        protected string GetFromBase64(string name)
        {
            string result = string.Empty;

            string base64 = GetValue(name, string.Empty);
            if (!string.IsNullOrWhiteSpace(base64))
            {
                try
                {
                    return UTF8Encoding.UTF8.GetString(Convert.FromBase64String(base64));
                }
                catch (Exception exc)
                {
                    _log.Error("Getting Base 64 Setting", exc);
                }
            }

            return result;
        }
        protected void SetToBase64(string name, string value)
        {
            try
            {
                SetValue(name, Convert.ToBase64String(UTF8Encoding.UTF8.GetBytes(value)));
            }
            catch (Exception exc)
            {
                _log.Error("Setting Base 64 Setting", exc);
            }
        }

        protected string GetEncryptedValue(string name)
        {
            string result = string.Empty;

            var value = GetValue(name, string.Empty);
            if (!string.IsNullOrWhiteSpace(value))
            {
                try
                {
                    result = Crypto.DecryptStringAES(value, name);
                }
                catch (Exception exc)
                {
                    _log.Error("Decrypting Setting", exc);
                }
            }

            return result;
        }
        protected void SetEncryptedValue(string name, string value)
        {
            try
            {
                SetValue(name, Crypto.EncryptStringAES(value, name));
            }
            catch (Exception exc)
            {
                _log.Error("Encrypting Setting", exc);
            }
        }
        #endregion
    }

    public enum CloseCivCondition
    {
        Never = 0,
        NewSave = 1,
        NewSaveNoMoreTurns = 2
    }

    public enum CivDxVersion
    {
        Dx9 = 0,
        Dx11 = 1,
        DxWin8 = 2
    }
}
