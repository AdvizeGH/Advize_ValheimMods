namespace Advize_PlantEverything;

using BepInEx.Logging;
using System.IO;
using System.Reflection;
using UnityEngine;
using static PlantEverything;

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

    internal static Texture2D DuplicateTexture(Sprite sprite)
    {
        // The resulting sprite dimensions
        int width = (int)sprite.textureRect.width;
        int height = (int)sprite.textureRect.height;

        // The whole sprite atlas
        var texture = sprite.texture;

        RenderTexture previous = RenderTexture.active;

        // Our RenderTexture for displaying the whole sprite atlas.
        RenderTexture renderTex = RenderTexture.GetTemporary(
            texture.width,
            texture.height,
            0,
            RenderTextureFormat.Default,
            RenderTextureReadWrite.sRGB);

        Graphics.Blit(texture, renderTex);
        RenderTexture.active = renderTex;

        // Create a copy of the texture that is readable
        Texture2D readableTexture = new(texture.width, texture.height);
        readableTexture.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
        readableTexture.Apply();
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(renderTex);

        // Crop to the needed texture
        Texture2D smallTexture = new(width, height);
        var colors = readableTexture.GetPixels((int)sprite.textureRect.x, (int)sprite.textureRect.y, width, height);
        smallTexture.SetPixels(colors);
        smallTexture.Apply();

        return smallTexture;
    }

    internal static Sprite ModifyTextureColor(Texture2D baseTexture, int width, int height, Color targetColor)
    {
        Texture2D modified = new(width, height);

        Color.RGBToHSV(targetColor, out float vineColorHue, out float vineColorSaturation, out float vineColorValue);

        Color[] allPixels = baseTexture.GetPixels();
        float[] hueDifferences = new float[width * height];
        float[] saturationDifferences = new float[width * height];
        float[] valueDifferences = new float[width * height];

        for (int i = 0; i < width * height; i++)
        {
            Color.RGBToHSV(allPixels[i], out float H, out float S, out float V);
            hueDifferences[i] = vineColorHue - H;
            saturationDifferences[i] = vineColorSaturation - S;
            valueDifferences[i] = vineColorValue - V;
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Color color = baseTexture.GetPixel(x, y);
                float originalAlpha = color.a;
                modified.SetPixel(x, y, Color.clear);

                if (color.a != 0)
                {
                    Color.RGBToHSV(color, out float H, out float S, out float V);
                    H += hueDifferences[x + y * width];
                    S += saturationDifferences[x + y * width];
                    float tintFactor = valueDifferences[x + y * width] > 0 ? 1.5f : 1f / 3f;
                    V *= V + valueDifferences[x + y * width] * tintFactor; // Weird formula but most accurate one I've found so far
                    color = Color.HSVToRGB(H, S, V);
                    color.a = originalAlpha;
                    modified.SetPixel(x, y, color);
                }
            }
        }

        modified.Apply();

        return Sprite.Create(modified, new(0, 0, width, height), Vector2.zero);
    }
}
