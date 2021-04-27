using BepInEx.Configuration;
using System;

namespace Advize_CartographySkill.Configuration
{
    public class ConfigBaseEntry
    {
        protected object _serverValue = null;
        public ConfigEntryBase BaseEntry;
        public bool ServerAuthoritative;
        protected bool _didError = false;

        internal ConfigBaseEntry(ConfigEntryBase configEntry, bool serverAuthoritative)
        {
            BaseEntry = configEntry;
            ServerAuthoritative = serverAuthoritative;
        }

        public void SetSerializedValue(string value)
        {
            try
            {
                object tmp = (_serverValue = TomlTypeConverter.ConvertToValue(value, BaseEntry.SettingType));
                _didError = false;
            }
            catch (Exception ex)
            {
                Config.Logger.LogWarning($"Config value of setting \"{BaseEntry.Definition}\" could not be parsed and will be ignored. Reason: {ex.Message}; Value: {value}");
            }
        }

        public void ClearServerValue()
        {
            _serverValue = null;
            _didError = false;
        }
    }
}
