using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class AetheriaContestBuild
{
    private const string OutputEnvironmentVariable = "AETHERIA_CONTEST_WEBGL_OUTPUT";

    [MenuItem("Aetheria/Build Contest WebGL", priority = 80)]
    public static void BuildWebGL()
    {
        var outputPath = ResolveOutputPath();
        var scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();
        if (scenes.Length == 0)
        {
            throw new InvalidOperationException("No enabled scenes are configured for the contest build.");
        }

        if (Directory.Exists(outputPath))
        {
            Directory.Delete(outputPath, true);
        }
        Directory.CreateDirectory(outputPath);

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        });

        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new InvalidOperationException(
                "Aetheria WebGL build failed: " + report.summary.result);
        }

        Debug.Log(
            "[Aetheria] Contest WebGL build completed: " + outputPath
            + " (" + report.summary.totalSize + " bytes)");
    }

    private static string ResolveOutputPath()
    {
        var configuredPath = Environment.GetEnvironmentVariable(OutputEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            return Path.GetFullPath(configuredPath);
        }

        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        return Path.Combine(desktop, "NHN", "Aetheria_WebGL");
    }
}
