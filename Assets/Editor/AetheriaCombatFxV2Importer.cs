using System;
using UnityEditor;
using UnityEngine;

public sealed class AetheriaCombatFxV2Importer : AssetPostprocessor
{
    private const string SkillRoot =
        "Assets/Resources/UI/VisualRefresh/CombatFX/SkillsV2/";
    private const string CommonRoot =
        "Assets/Resources/UI/VisualRefresh/CombatFX/V2/";

    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(SkillRoot, StringComparison.Ordinal)
            && !assetPath.StartsWith(CommonRoot, StringComparison.Ordinal))
        {
            return;
        }

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Default;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.sRGBTexture = true;
        importer.isReadable = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.crunchedCompression = false;
    }

    public override uint GetVersion()
    {
        return 1;
    }
}
