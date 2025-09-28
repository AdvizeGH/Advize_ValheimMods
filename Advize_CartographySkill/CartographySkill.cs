namespace Advize_CartographySkill;

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

[BepInPlugin(PluginID, PluginName, Version)]
public partial class CartographySkill : BaseUnityPlugin
{
    public const string PluginID = "advize.CartographySkill";
    public const string PluginName = "CartographySkill";
    public const string Version = "3.1.0";
    public const int SKILL_TYPE = 1337;

    internal static ManualLogSource ModLogger = new($" {PluginName}");
    private static ModConfig config;

    private static Skills.SkillDef cartographySkillDef;

    internal static AssetBundle assetBundle;
    private static readonly Dictionary<string, Texture2D> cachedTextures = [];
    private static readonly Dictionary<Texture2D, Sprite> cachedSprites = [];

    internal static readonly Dictionary<LogLevel, Action<string>> logActions = new()
    {
        { LogLevel.Fatal, ModLogger.LogFatal },
        { LogLevel.Error, ModLogger.LogError },
        { LogLevel.Warning, ModLogger.LogWarning },
        { LogLevel.Message, ModLogger.LogMessage },
        { LogLevel.Info, ModLogger.LogInfo },
        { LogLevel.Debug, ModLogger.LogDebug }
    };

    private static bool IsMatchingSkillName(string name) => string.Equals(config.SkillName, name, StringComparison.OrdinalIgnoreCase);

    public void Awake()
    {
        BepInEx.Logging.Logger.Sources.Add(ModLogger);
        assetBundle = LoadAssetBundle("cartographyskill");
        config = new ModConfig(Config, new ServerSync.ConfigSync(PluginID) { DisplayName = PluginName, CurrentVersion = Version, MinimumRequiredVersion = "3.1.0" });

        cartographySkillDef = new()
        {
            m_description = "$csSkillDescription",
            m_icon = CreateSprite("cartographyicon.png", new Rect(0, 0, 32, 32)),
            m_increseStep = 1.0f,
            m_skill = (Skills.SkillType)SKILL_TYPE
        };

        new Harmony(PluginID).PatchAll();
    }

    private static void UpdateExploreRadius()
    {
        if (!Player.m_localPlayer || !config.EnableSkill) return;

        float skillLevel = Player.m_localPlayer.GetSkillFactor((Skills.SkillType)SKILL_TYPE) * 100;
        float newExploreRadius = config.BaseExploreRadius + (config.ExploreRadiusIncrease * skillLevel);

        Dbgl($"Previous explore radius was: {Minimap.instance.m_exploreRadius} new radius is: {newExploreRadius}");
        Minimap.instance.m_exploreRadius = newExploreRadius;
    }

    private static void OnLanguageChange()
    {
        UpdateLocalization(null, null);
    }

    internal static void UpdateLocalization(object sender, EventArgs e)
    {
        Localization.instance.AddWord($"csSkillDescription", config.SkillDescription);
        Localization.instance.AddWord($"skill_{SKILL_TYPE}", config.SkillName);
    }

    internal static AssetBundle LoadAssetBundle(string fileName)
    {
        Stream manifestResourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"Advize_{PluginName}.Assets.{fileName}");
        return AssetBundle.LoadFromStream(manifestResourceStream);
    }

    private static Texture2D LoadTexture(string fileName)
    {
        Texture2D result;

        if (cachedTextures.ContainsKey(fileName))
        {
            result = cachedTextures[fileName];
        }
        else
        {
            result = assetBundle.LoadAsset<Texture2D>(fileName);
            cachedTextures.Add(fileName, result);
        }

        return result;
    }

    private static Sprite CreateSprite(string fileName, Rect spriteSection)
    {
        try
        {
            Sprite result;
            Texture2D texture = LoadTexture(fileName);

            if (cachedSprites.ContainsKey(texture))
            {
                result = cachedSprites[texture];
            }
            else
            {
                result = Sprite.Create(texture, spriteSection, Vector2.zero);
                cachedSprites.Add(texture, result);
            }
            return result;
        }
        catch
        {
            Dbgl("Unable to load texture", forceLog: true, level: LogLevel.Error);
        }

        return null;
    }

    internal static void Dbgl(string message, bool forceLog = false, LogLevel level = LogLevel.Info)
    {
        if (forceLog || config.EnableDebugMessages)
            logActions[level](message);
    }
}
