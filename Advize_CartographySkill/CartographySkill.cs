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
    public const string Version = "3.2.0";
    public const int SKILL_TYPE = 1337;

    internal static ManualLogSource ModLogger = new($" {PluginName}");
    private static ModConfig config;

    private static Skills.SkillDef cartographySkillDef;

    internal static AssetBundle assetBundle;
    private static readonly Dictionary<string, Texture2D> cachedTextures = [];
    private static readonly Dictionary<Texture2D, Sprite> cachedSprites = [];

    private static bool IsMatchingSkillName(string name) => string.Equals(config.SkillName, name, StringComparison.OrdinalIgnoreCase);

    public void Awake()
    {
        BepInEx.Logging.Logger.Sources.Add(ModLogger);
        assetBundle = LoadAssetBundle("cartographyskill");
        config = new ModConfig(Config, new ServerSync.ConfigSync(PluginID) { DisplayName = PluginName, CurrentVersion = Version, MinimumRequiredVersion = "3.2.0" });

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
        if (cachedTextures.TryGetValue(fileName, out Texture2D texture))
            return texture;

        return cachedTextures[fileName] = assetBundle.LoadAsset<Texture2D>(fileName);
    }

    private static Sprite CreateSprite(string fileName, Rect spriteSection)
    {
        Texture2D texture = LoadTexture(fileName);

        if (cachedSprites.TryGetValue(texture, out Sprite sprite))
            return sprite;

        return cachedSprites[texture] = Sprite.Create(texture, spriteSection, Vector2.zero);
    }

    internal static void Dbgl(string message, bool forceLog = false, LogLevel level = LogLevel.Info)
    {
        if (forceLog || config.EnableDebugMessages)
            ModLogger.Log(level, message);
    }
}
