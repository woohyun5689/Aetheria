#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class AetheriaProjectSetup
{
    private const string SceneFolder = "Assets/Scenes";
    private const string ScenePath = "Assets/Scenes/Aetheria.unity";

    static AetheriaProjectSetup()
    {
        EditorApplication.delayCall += EnsureProjectScene;
        EditorApplication.delayCall += Force2DSceneView;
    }

    [MenuItem("Aetheria/Setup 2D Scene")]
    public static void EnsureProjectScene()
    {
        if (!AssetDatabase.IsValidFolder(SceneFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Scenes");
        }

        if (!File.Exists(ScenePath))
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenePath, true)
        };

        if (EditorSceneManager.GetActiveScene().path != ScenePath)
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        EnsureSceneCamera();
        EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;
        Force2DSceneView();
    }

    private static void EnsureSceneCamera()
    {
        if (UnityEngine.Object.FindFirstObjectByType<Camera>() != null)
        {
            return;
        }

        var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);

        var camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color32(3, 5, 10, 255);

        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
    }

    private static void Force2DSceneView()
    {
        foreach (SceneView sceneView in SceneView.sceneViews)
        {
            sceneView.in2DMode = true;
            sceneView.Repaint();
        }

        SceneView.RepaintAll();
    }
}
#endif
