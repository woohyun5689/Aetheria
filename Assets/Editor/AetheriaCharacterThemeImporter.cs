using UnityEditor;
using UnityEngine;

public sealed class AetheriaCharacterThemeImporter : AssetPostprocessor
{
    private const string ThemeRoot = "Assets/Resources/UI/CharacterThemes/";

    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(ThemeRoot, System.StringComparison.Ordinal))
        {
            return;
        }

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.sRGBTexture = true;
        importer.isReadable = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.spritePixelsPerUnit = 100f;
        importer.maxTextureSize = 2048;
    }

    public override uint GetVersion()
    {
        return 1;
    }
}
