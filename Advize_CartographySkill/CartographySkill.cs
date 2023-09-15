using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using Advize_CartographySkill.Configuration;

namespace Advize_CartographySkill
{
    [BepInPlugin(PluginID, PluginName, Version)]
    public partial class CartographySkill : BaseUnityPlugin
    {
        public const string PluginID = "advize.CartographySkill";
        public const string PluginName = "CartographySkill";
        public const string Version = "3.0.0";
        public const int SKILL_TYPE = 1337;

        private readonly Harmony harmony = new(PluginID);
        public static ManualLogSource CSLogger = new($" {PluginName}");

        private static CartographySkillDef cartographySkill;

        private static int tileCount = 0;

        private static readonly string modDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private static readonly Dictionary<string, Texture2D> cachedTextures = new();

        private static ModConfig config;

        private static readonly Dictionary<string, string> stringDictionary = new()
        {
            { "SkillName", "Cartography" },
            { "SkillDescription", "Increases map explore radius." }
        };

        public void Awake()
        {
            BepInEx.Logging.Logger.Sources.Add(CSLogger);
            config = new ModConfig(Config, new ServerSync.ConfigSync(PluginID) { DisplayName = PluginName, CurrentVersion = Version, MinimumRequiredVersion = "3.0.0" });
            if (config.EnableLocalization)
                LoadLocalizedStrings();
            cartographySkill = new();
            harmony.PatchAll();
        }

        private void LoadLocalizedStrings()
        {
            string fileName = $"Advize_{PluginName}.json";
            string filePath = Path.Combine(modDirectory, fileName);

            try
            {
                string jsonText = File.ReadAllText(filePath);
                LocalizedStrings localizedStrings = JsonUtility.FromJson<LocalizedStrings>(jsonText);

                foreach (string value in localizedStrings.localizedStrings)
                {
                    string[] split = value.Split(':');
                    stringDictionary.Remove(split[0]);
                    stringDictionary.Add(split[0], split[1]);
                }

                Dbgl($"Loaded localized strings from {filePath}");
                return;
            }
            catch
            {
                Dbgl("Unable to load localized text file, generating new one from default English values", true);
            }
            SerializeDict();
        }

        private void SerializeDict()
        {
            string filePath = Path.Combine(modDirectory, $"Advize_{PluginName}.json");

            LocalizedStrings localizedStrings = new();
            foreach (KeyValuePair<string, string> kvp in stringDictionary)
            {
                localizedStrings.localizedStrings.Add($"{kvp.Key}:{kvp.Value}");
            }

            File.WriteAllText(filePath, JsonUtility.ToJson(localizedStrings, true));

            Dbgl($"Saved english localized strings to {filePath}");
        }

        public static void InitLocalization()
        {
            Dbgl("InitLocalization");
            foreach (KeyValuePair<string, string> kvp in stringDictionary)
            {
                Localization.instance.AddWord($"cs{kvp.Key}", kvp.Value);
            }
            Localization.instance.AddWord($"skill_{SKILL_TYPE}", stringDictionary["SkillName"]);
            stringDictionary.Clear();
        }

        private static Sprite CreateSprite(string fileName, Rect spriteSection)
        {
            try
            {
                Texture2D texture = LoadTexture(fileName);
                return Sprite.Create(texture, spriteSection, Vector2.zero);
            }
            catch
            {
                Dbgl("Unable to load texture", true, true);
            }

            return null;
        }

        private static Texture2D LoadTexture(string fileName)
        {
            bool textureLoaded = cachedTextures.ContainsKey(fileName);
            Texture2D result;
            if (textureLoaded)
            {
                result = cachedTextures[fileName];
            }
            else
            {
                Stream manifestResourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"Advize_{PluginName}.Assets.{fileName}");
                byte[] array = new byte[manifestResourceStream.Length];
                manifestResourceStream.Read(array, 0, array.Length);
                Texture2D texture = new(0, 0);
                ImageConversion.LoadImage(texture, array);
                result = texture;
            }

            return result;
        }

        internal static void Dbgl(string message, bool forceLog = false, bool logError = false)
        {
            if (forceLog || config.EnableDebugMessages)
            {                
                if (!logError)
                    CSLogger.LogInfo(message);
                else
                    CSLogger.LogError(message);
            }
        }

        private class CartographySkillDef
        {
            public string name = "$csSkillName";
            public Skills.SkillDef skillDef = new()
            {
                m_description = "$csSkillDescription",
                m_icon = CreateSprite("cartographyicon.png", new Rect(0, 0, 32, 32)),
                m_increseStep = 1.0f,
                m_skill = (Skills.SkillType)SKILL_TYPE
            };
        }

        internal class LocalizedStrings
        {
            public List<string> localizedStrings = new();
        }
    }
}
