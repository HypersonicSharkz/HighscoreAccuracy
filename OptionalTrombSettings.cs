﻿using System;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;

// Credit to CripsyKevin#4931 on the modding discord for this code

namespace HighscoreAccuracy;

public class OptionalTrombSettings
{
    private static bool? _enabled;

    public static bool enabled
    {
        get
        {
            if (_enabled == null)
                _enabled = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("TrombSettings");
            return (bool)_enabled;
        }
    }

    public static object GetConfigPage(string pageName)
    {
        try
        {
            Type trombConfig = null;
            trombConfig = Type.GetType("TrombSettings.TrombConfig, TrombSettings");
            if (trombConfig == null)
            {
                Plugin.Log.LogDebug("TrombSettings not found.");
                return null;
            }

            var trombSettingsInstance = trombConfig.GetField("TrombSettings").GetValue(null);
            var indexerMethod = trombSettingsInstance.GetType().GetIndexer(typeof(string));
            var settingsPage = indexerMethod.GetGetMethod().Invoke(trombSettingsInstance, new object[] { pageName });
            return settingsPage;
        }
        catch (Exception e)
        {
            Plugin.Log.LogError("Exception trying to get config page. Reporting TrombSettings as not found.");
            Plugin.Log.LogError(e.Message);
            Plugin.Log.LogError(e.StackTrace);
            return null;
        }
    }

    public static void AddSlider(object page, float min, float max, float increment, bool integerOnly, ConfigEntryBase entry)
    {
        try
        {
            Type clazz = Type.GetType("TrombSettings.StepSliderConfig, TrombSettings");
            if (clazz == null)
                return;
            var ctor = clazz.GetConstructor(new Type[] { typeof(float), typeof(float), typeof(float), typeof(bool), typeof(ConfigEntryBase) });
            var slider = ctor?.Invoke(new object[] { min, max, increment, integerOnly, entry });

            if (slider != null)
            {
                // Find "public new void Add(BaseConfig configEntry)"
                Type baseConfigClass = Type.GetType("TrombSettings.BaseConfig, TrombSettings");
                var addMethod = page.GetType().GetMethod("Add", new Type[] { baseConfigClass });
                addMethod.Invoke(page, new object[] { slider });
            }
            else
                Plugin.Log.LogError("Couldn't create slider!");
        }
        catch (Exception e)
        {
            Plugin.Log.LogError("Exception trying to create slider. Reporting TrombSettings as not found.");
            Plugin.Log.LogError(e.Message);
            Plugin.Log.LogError(e.StackTrace);
        }
    }

    public static void Add(object page, ConfigEntryBase entry)
    {
        var addFn = page.GetType().GetMethod("Add", new Type[] { typeof(ConfigEntryBase) });
        addFn.Invoke(page, new object[] { entry });
    }
}

internal static class TypeExtensions
{
    // From https://stackoverflow.com/a/55457150
    public static PropertyInfo GetIndexer(this Type type, params Type[] arguments) => type.GetProperties().First(x => x.GetIndexParameters().Select(y => y.ParameterType).SequenceEqual(arguments));
}
