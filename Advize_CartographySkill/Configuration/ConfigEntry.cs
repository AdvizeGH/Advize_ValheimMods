namespace Advize_CartographySkill.Configuration
{
    public class ConfigEntry<T> : ConfigBaseEntry
    {
        public T ServerValue => (T)_serverValue;
        private BepInEx.Configuration.ConfigEntry<T> _configEntry;

        internal ConfigEntry(BepInEx.Configuration.ConfigEntry<T> configEntry, bool serverAuthoritative) : base(configEntry, serverAuthoritative)
        {
            _configEntry = configEntry;
        }

        public T Value
        {
            get
            {
                //Todo: Extended behaviour for value selection?
                if (ServerAuthoritative && !Config.ZNet.IsServer())
                {
                    if (_serverValue != null)
                    {
                        return ServerValue;
                    }
                    else
                    {
                        if (!_didError)
                        {
                            //Config.Logger.LogWarning($"No received value for Server Authoritative Config. {BaseEntry.Definition}. Falling back to client config.");
                            _didError = true;
                        }
                        return _configEntry.Value;
                    }
                }
                return _configEntry.Value;
            }
            set
            {
                _configEntry.Value = value;
            }
        }
    }
}
