namespace Advize_PlantEasily;

using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using Attributes = ConfigurationManagerAttributes;

internal static class ConfigExtensions
{
    private static readonly Dictionary<string, int> _sectionOrder = [];

    internal static ConfigEntry<T> BindInOrder<T>(
        this ConfigFile config,
        string section,
        string key,
        T defaultValue,
        string description,
        Action<Attributes> manualAttributes = null,
        AcceptableValueBase acceptableValues = null)
    {
        if (!_sectionOrder.TryGetValue(section, out int next))
            next = 100;

        Attributes automaticAttributes = new() { Order = next };

        manualAttributes?.Invoke(automaticAttributes);

        if (automaticAttributes.Order == next)
            _sectionOrder[section] = next - 1;

        ConfigDescription desc = new(description, acceptableValues, automaticAttributes);
        return config.Bind(section, key, defaultValue, desc);
    }
}
