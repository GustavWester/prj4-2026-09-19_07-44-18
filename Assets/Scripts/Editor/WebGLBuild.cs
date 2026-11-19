#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Menu: Build > WebGL til hjemmesiden.
/// Bygger spillet direkte ind i hjemmesidens public/unity-build-mappe, som unityGame.jsx loader fra.
/// Unity navngiver filerne efter mappen: unity-build.loader.js, unity-build.data.unityweb osv.
/// Forventer at SW4PRJ4_Goonsite ligger ved siden af dette repo.
/// </summary>
public static class WebGLBuild
{
    private const string WebsiteGameFolder = "../SW4PRJ4_Goonsite/mit-projekt/public/unity-build";

    [MenuItem("Build/WebGL til hjemmesiden")]
    public static void Build()
    {
        var outputPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", WebsiteGameFolder));
        if (!Directory.Exists(Path.GetDirectoryName(outputPath)))
            throw new DirectoryNotFoundException($"Fandt ikke hjemmesidens public-mappe: {Path.GetDirectoryName(outputPath)}");

        // Gzip + fallback: virker på alle webservere (også Vite dev-server) uden ekstra server-opsætning
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;

        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (scenes.Length == 0)
            throw new InvalidOperationException("Ingen scener er slået til i Build Settings.");

        if (Directory.Exists(outputPath))
            Directory.Delete(outputPath, true); // gamle build-filer skal ikke blive liggende

        var report = BuildPipeline.BuildPlayer(scenes, outputPath, BuildTarget.WebGL, BuildOptions.None);
        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException($"WebGL-build fejlede: {report.summary.result}");

        // Burst-debugfiler skal ikke med på hjemmesiden
        foreach (var dir in Directory.GetDirectories(outputPath, "*_DoNotShip"))
            Directory.Delete(dir, true);

        Debug.Log($"WebGL-build færdig: {outputPath}");
        EditorUtility.RevealInFinder(outputPath);
    }
}
#endif
