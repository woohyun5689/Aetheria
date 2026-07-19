using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class AetheriaUniversal2DSetup
{
    private const string SettingsFolder = "Assets/Settings";
    private const string RendererPath = SettingsFolder + "/Aetheria_2D_Renderer.asset";
    private const string PipelinePath = SettingsFolder + "/Aetheria_Universal2D_Pipeline.asset";

    [MenuItem("Aetheria/Setup Universal 2D Project")]
    public static void Setup()
    {
        EnsureFolder();

        var rendererData = AssetDatabase.LoadAssetAtPath<Renderer2DData>(RendererPath);
        if (rendererData == null)
        {
            rendererData = ScriptableObject.CreateInstance<Renderer2DData>();
            AssetDatabase.CreateAsset(rendererData, RendererPath);
        }

        var pipelineAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
        if (pipelineAsset == null)
        {
            pipelineAsset = UniversalRenderPipelineAsset.Create(rendererData);
            AssetDatabase.CreateAsset(pipelineAsset, PipelinePath);
        }

        GraphicsSettings.defaultRenderPipeline = pipelineAsset;
        QualitySettings.renderPipeline = pipelineAsset;

        EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Aetheria Universal 2D setup complete.");
    }

    private static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder(SettingsFolder))
        {
            Directory.CreateDirectory(SettingsFolder);
            AssetDatabase.Refresh();
        }
    }
}
