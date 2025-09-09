namespace Advize_PlantEverything;

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using static PlantEverything;
using static StaticContent;
using static StaticMembers;

static class PluginUtils
{
    internal static AssetBundle LoadAssetBundle(string fileName)
    {
        Stream manifestResourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"Advize_{PluginName}.Assets.{fileName}");
        return AssetBundle.LoadFromStream(manifestResourceStream);
    }

    internal static GameObject CreatePrefab(string name)
    {
        GameObject loadedPrefab = assetBundle.LoadAsset<GameObject>(name);
        loadedPrefab.SetActive(true);

        return loadedPrefab;
    }

    internal static Piece GetOrAddPieceComponent(GameObject go) => go.GetComponent<Piece>() ?? go.AddComponent<Piece>();

    internal static string GetPrefabName(Component c) => c.transform.root.name.Replace("(Clone)", "");

    internal static bool IsModdedPrefab(Component c) => c && prefabRefs.ContainsKey(GetPrefabName(c));

    internal static bool IsModdedPrefabOrSapling(string s) => s.StartsWith("$pe") || s.EndsWith("_sapling");

    internal static bool HoldingCultivator => Player.m_localPlayer?.GetRightItem()?.m_shared.m_name == "$item_cultivator";

    internal static void RemoveFromCultivator(List<PrefabDB> prefabs)
    {
        if (HoldingCultivator) SheatheCultivator();

        PieceTable pieceTable = prefabRefs["Cultivator"].GetComponent<ItemDrop>().m_itemData.m_shared.m_buildPieces;
        prefabs.ForEach(x => pieceTable.m_pieces.Remove(x.Prefab));
    }

    internal static void SheatheCultivator()
    {
        Dbgl("Cultivator updated through config change, unequipping cultivator.", forceLog: true);
        if (!ZNet.instance.HaveStopped) Player.m_localPlayer.HideHandItems();
    }

    internal static Piece CreatePiece(PieceDB pdb)
    {
        Piece piece = GetOrAddPieceComponent(prefabRefs[pdb.key]);

        piece.m_name = pdb.extraResource ? pdb.pieceName : $"$pe{pdb.Name}Name";
        piece.m_description = pdb.extraResource ? pdb.pieceDescription : $"$pe{pdb.Name}Description";
        piece.m_category = Piece.PieceCategory.Misc;
        piece.m_cultivatedGroundOnly = (pdb.key.Contains("berryBush") || pdb.key.Contains("Pickable")) && config.RequireCultivation;
        piece.m_groundOnly = piece.m_groundPiece = pdb.isGrounded ?? !config.PlaceAnywhere;
        piece.m_canBeRemoved = pdb.canBeRemoved ?? true;
        piece.m_targetNonPlayerBuilt = false;
        piece.m_randomTarget = config.EnemiesTargetPieces;

        return piece;
    }

    internal static Sprite CreateSprite(string fileName, Rect spriteSection)
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
            Dbgl("Unable to load texture", true, LogLevel.Error);
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
            result = assetBundle.LoadAsset<Texture2D>(fileName);
            cachedTextures.Add(fileName, result);
        }

        return result;
    }

    private static string SerializeExtraResource(ExtraResource extraResource, bool prettyPrint = true) => JsonUtility.ToJson(extraResource, prettyPrint);

    internal static ExtraResource DeserializeExtraResource(string extraResource) => JsonUtility.FromJson<ExtraResource>(extraResource);

    private static void SaveExtraResources()
    {
        string filePath = Path.Combine(CustomConfigPath, "ExtraResources.json");
        Dbgl($"deserializedExtraResources.Count is {deserializedExtraResources.Count}");

        string fullContent = "";
        //foreach (ExtraResource test in deserializedExtraResources)
        //{
        //    fullContent += SerializeExtraResource(test) + ";\n";
        //}
        fullContent += SerializeExtraResource(deserializedExtraResources[0]) + ";\n\n";
        fullContent += SerializeExtraResource(deserializedExtraResources[1], false) + ";\n";

        File.WriteAllText(filePath, fullContent);
        Dbgl($"Serialized extraResources to {filePath}", true);
    }

    internal static void LoadExtraResources()
    {
        Dbgl("LoadExtraResources");
        deserializedExtraResources.Clear();
        string fileName = "ExtraResources.json";
        string filePath = Path.Combine(CustomConfigPath, fileName);

        try
        {
            string jsonText = File.ReadAllText(filePath);
            string[] split = jsonText.Split(';');

            foreach (string value in split)
            {
                if (value.IsNullOrWhiteSpace()) continue;
                ExtraResource er = DeserializeExtraResource(value);
                if (er.IsValid())
                {
                    deserializedExtraResources.Add(er);
                    //Dbgl($"er1 {er.prefabName}, {er.resourceName}, {er.resourceCost}, {er.groundOnly}, {er.pieceName}, {er.pieceDescription}");
                }
                else
                {
                    if (er.prefabName.StartsWith("PE_Fake")) continue;
                    Dbgl($"Invalid resource, {er.prefabName}, configured in {fileName}, skipping entry", true, LogLevel.Warning);
                }
            }

            Dbgl($"Loaded extra resources from {filePath}", true);
            //Dbgl($"deserializedExtraResources.Count is {deserializedExtraResources.Count}");
            Dbgl($"Assigning local value from deserializedExtraResources");

            List<string> resourcesToSync = [];
            deserializedExtraResources.ForEach(er => resourcesToSync.Add(SerializeExtraResource(er)));
            config.SyncedExtraResources.AssignLocalValue(resourcesToSync);
            return;
        }
        catch (Exception e)
        {
            //Dbgl(e.GetType().FullName, true, LogLevel.Error);
            if (e is FileNotFoundException)
            {
                Dbgl($"Error loading data from {fileName}. Generating new file with example values", true, LogLevel.Warning);
                deserializedExtraResources = GenerateExampleResources();
                SaveExtraResources();
            }
            else
            {
                Dbgl($"Error loading data from {fileName}. Additional resources have not been added", level: LogLevel.Warning);
                deserializedExtraResources.Clear();
            }
        }
    }

    internal static void LoadLocalizedStrings()
    {
        string fileName = $"{config.Language}_{PluginName}.json";
        string filePath = Path.Combine(CustomConfigPath, fileName);

        try
        {
            string jsonText = File.ReadAllText(filePath);
            ModLocalization ml = JsonUtility.FromJson<ModLocalization>(jsonText);

            foreach (string value in ml.LocalizedStrings)
            {
                string[] split = value.Split(':');
                DefaultLocalizedStrings.Remove(split[0]);
                DefaultLocalizedStrings.Add(split[0], split[1]);
            }

            Dbgl($"Loaded localized strings from {filePath}");
            return;
        }
        catch
        {
            Dbgl("EnableLocalization is true but unable to load localized text file, generating new one from default English values", true);
        }
        SerializeDict();
    }

    private static void SerializeDict()
    {
        string filePath = Path.Combine(CustomConfigPath, $"english_{PluginName}.json");

        ModLocalization ml = new();
        foreach (KeyValuePair<string, string> kvp in DefaultLocalizedStrings)
        {
            ml.LocalizedStrings.Add($"{kvp.Key}:{kvp.Value}");
        }

        File.WriteAllText(filePath, JsonUtility.ToJson(ml, true));

        Dbgl($"Saved english localized strings to {filePath}");
    }

    internal static Heightmap.Biome GetBiomeMask(Heightmap.Biome[] biomes)
    {
        Heightmap.Biome biomeMask = 0;

        foreach (Heightmap.Biome biome in biomes)
        {
            biomeMask |= biome;
        }

        return biomeMask;
    }

    internal static string SetupConfigDirectory()
    {
        string path = Path.Combine(Paths.ConfigPath, PluginName);
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        return path;
    }
}
