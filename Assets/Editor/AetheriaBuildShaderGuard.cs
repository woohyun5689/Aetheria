#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

[InitializeOnLoad]
internal sealed class AetheriaBuildShaderGuard : IPreprocessBuildWithReport
{
    private const string GraphicsSettingsPath = "ProjectSettings/GraphicsSettings.asset";

    private static readonly string[] RuntimeShaderNames =
    {
        "UI/Default",
        "UI/DefaultETC1",
        "Sprites/Default"
    };

    static AetheriaBuildShaderGuard()
    {
        EditorApplication.delayCall += EnsureRuntimeUiShadersAfterReload;
    }

    public int callbackOrder => -10000;

    public void OnPreprocessBuild(BuildReport report)
    {
        if (!EnsureRuntimeUiShaders())
        {
            throw new BuildFailedException(
                "Aetheria runtime UI shaders could not be preserved. " +
                "The player build was stopped to prevent an all-magenta UI.");
        }
    }

    [MenuItem("Aetheria/Build/Ensure Runtime UI Shaders")]
    private static void EnsureRuntimeUiShadersFromMenu()
    {
        if (EnsureRuntimeUiShaders())
        {
            Debug.Log("Aetheria runtime UI shaders are ready for player builds.");
        }
    }

    private static void EnsureRuntimeUiShadersAfterReload()
    {
        EnsureRuntimeUiShaders();
    }

    private static bool EnsureRuntimeUiShaders()
    {
        var settingsAssets = AssetDatabase.LoadAllAssetsAtPath(GraphicsSettingsPath);
        if (settingsAssets == null || settingsAssets.Length == 0)
        {
            Debug.LogError("Aetheria build guard could not load GraphicsSettings.");
            return false;
        }

        var serializedSettings = new SerializedObject(settingsAssets[0]);
        var alwaysIncludedShaders = serializedSettings.FindProperty("m_AlwaysIncludedShaders");
        if (alwaysIncludedShaders == null || !alwaysIncludedShaders.isArray)
        {
            Debug.LogError("Aetheria build guard could not find Always Included Shaders.");
            return false;
        }

        var existingShaders = new HashSet<Shader>();
        for (var index = 0; index < alwaysIncludedShaders.arraySize; index++)
        {
            var shader = alwaysIncludedShaders.GetArrayElementAtIndex(index).objectReferenceValue as Shader;
            if (shader != null)
            {
                existingShaders.Add(shader);
            }
        }

        var changed = false;
        for (var index = 0; index < RuntimeShaderNames.Length; index++)
        {
            var shaderName = RuntimeShaderNames[index];
            var shader = Shader.Find(shaderName);
            if (shader == null)
            {
                if (string.Equals(shaderName, "UI/Default", StringComparison.Ordinal))
                {
                    Debug.LogError("Required runtime shader was not found: " + shaderName);
                    return false;
                }

                Debug.LogWarning("Optional runtime shader was not found: " + shaderName);
                continue;
            }

            if (existingShaders.Contains(shader))
            {
                continue;
            }

            var newIndex = alwaysIncludedShaders.arraySize;
            alwaysIncludedShaders.InsertArrayElementAtIndex(newIndex);
            alwaysIncludedShaders.GetArrayElementAtIndex(newIndex).objectReferenceValue = shader;
            existingShaders.Add(shader);
            changed = true;
        }

        if (!changed)
        {
            return true;
        }

        serializedSettings.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(settingsAssets[0]);
        AssetDatabase.SaveAssets();
        Debug.Log("Aetheria build guard added runtime UI shaders to GraphicsSettings.");
        return true;
    }
}
#endif
