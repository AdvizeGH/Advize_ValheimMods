using Advize_CartographySkill.Configuration;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Advize_CartographySkill
{
    [BepInPlugin(PluginID, PluginName, Version)]
    public partial class CartographySkill : BaseUnityPlugin
    {
        public const string PluginID = "advize.CartographySkill";
        public const string PluginName = "CartographySkill";
        public const string Version = "3.0.1";
        public const int SKILL_TYPE = 1337;

        private readonly Harmony harmony = new(PluginID);
        public static ManualLogSource CSLogger = new($" {PluginName}");

        private static CartographySkillDef cartographySkill;

        private static readonly Dictionary<string, Texture2D> cachedTextures = [];
        private static readonly Dictionary<Texture2D, Sprite> cachedSprites = [];

        private static ModConfig config;

        private static readonly Dictionary<string, string> stringDictionary = new()
        {
            { "SkillName", "Cartography" },
            { "SkillDescription", "Increases map explore radius." }
        };

        public void Awake()
        {
            BepInEx.Logging.Logger.Sources.Add(CSLogger);
            config = new ModConfig(Config, new ServerSync.ConfigSync(PluginID) { DisplayName = PluginName, CurrentVersion = Version, MinimumRequiredVersion = "3.0.1" });
            if (config.EnableLocalization)
                LoadLocalizedStrings();
            cartographySkill = new();
            harmony.PatchAll();
        }

        private static string ModConfigDirectory()
        {
            string path = Path.Combine(Paths.ConfigPath, PluginName);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return path;
        }

        private void LoadLocalizedStrings()
        {
            string fileName = $"Advize_{PluginName}.json";
            string filePath = Path.Combine(ModConfigDirectory(), fileName);

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
            string filePath = Path.Combine(ModConfigDirectory(), $"Advize_{PluginName}.json");

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
                Dbgl("Unable to load texture", true, true);
            }

            return null;
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
                Stream manifestResourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"Advize_{PluginName}.Assets.{fileName}");
                byte[] array = new byte[manifestResourceStream.Length];
                manifestResourceStream.Read(array, 0, array.Length);
                Texture2D texture = new(0, 0);
                ImageConversion.LoadImage(texture, array);
                result = texture;
                cachedTextures.Add(fileName, result);
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
            public List<string> localizedStrings = [];
        }
    }
}
