#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public static class CiBuild
{
    public static void Build()
    {
        var targetName = Environment.GetEnvironmentVariable("UNITY_BUILD_TARGET");
        var outputPath = Environment.GetEnvironmentVariable("UNITY_BUILD_PATH");

        if (string.IsNullOrWhiteSpace(targetName) || string.IsNullOrWhiteSpace(outputPath))
            throw new InvalidOperationException("UNITY_BUILD_TARGET and UNITY_BUILD_PATH are required.");

        if (!Enum.TryParse(targetName, out BuildTarget target))
            throw new InvalidOperationException($"Unsupported Unity build target: {targetName}");

        var scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        if (scenes.Length == 0)
            throw new InvalidOperationException("No enabled scenes are configured in Build Settings.");

        var namedTarget = NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(target));
        PlayerSettings.SetScriptingBackend(namedTarget, ScriptingImplementation.Mono2x);

        var report = BuildPipeline.BuildPlayer(scenes, outputPath, target, BuildOptions.StrictMode);
        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException($"Unity build failed with result: {report.summary.result}");
    }
}
#endif