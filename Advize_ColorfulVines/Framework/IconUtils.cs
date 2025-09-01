namespace Advize_ColorfulVines;
using UnityEngine;
using static StaticMembers;

static class IconUtils
{
    private static Texture2D pieceIcon;

    internal static void InitializeVineIcon(Sprite sourceIcon)
    {
        pieceIcon = DuplicateTexture(sourceIcon);
    }

    internal static void UpdateVineIcon()
    {
        prefabRefs["CV_VineAsh_sapling"].GetComponent<Piece>().m_icon = ModifyTextureColor(64, 64, VineColorFromConfig);
    }

    private static Texture2D DuplicateTexture(Sprite sprite)
    {
        // The resulting sprite dimensions
        int width = (int)sprite.textureRect.width;
        int height = (int)sprite.textureRect.height;

        // The whole sprite atlas
        Texture2D texture = sprite.texture;

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
        Color[] colors = readableTexture.GetPixels((int)sprite.textureRect.x, (int)sprite.textureRect.y, width, height);
        smallTexture.SetPixels(colors);
        smallTexture.Apply();

        return smallTexture;
    }

    private static Sprite ModifyTextureColor(int width, int height, Color targetColor)
    {
        Texture2D modified = new(width, height);

        Color.RGBToHSV(targetColor, out float vineColorHue, out float vineColorSaturation, out float vineColorValue);

        Color[] allPixels = pieceIcon.GetPixels();
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
                Color color = pieceIcon.GetPixel(x, y);
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
