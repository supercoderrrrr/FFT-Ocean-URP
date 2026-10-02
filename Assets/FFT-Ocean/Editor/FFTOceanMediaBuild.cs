#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class FFTOceanMediaBuild
{
    public static void TuneUnderwaterAndBuildCapturePlayer()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/FFT-Ocean.unity");
        FFTOceanUnderwaterController underwater = UnityEngine.Object.FindObjectOfType<FFTOceanUnderwaterController>();
        if (underwater == null)
            throw new InvalidOperationException("The portfolio scene is missing its underwater controller");

        SerializedObject settings = new SerializedObject(underwater);
        settings.FindProperty("lightShaftStrength").floatValue = 0.12f;
        settings.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        BuildCapturePlayer();
    }

    public static void BuildCapturePlayer()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string outputDirectory = Path.Combine(projectRoot, "Builds", "PortfolioCapturePlayer");
        Directory.CreateDirectory(outputDirectory);
        string executablePath = Path.Combine(outputDirectory, "FFTOceanCapture.exe");

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/FFT-Ocean.unity" },
            locationPathName = executablePath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException($"Capture player build failed with {report.summary.totalErrors} errors");

        Debug.Log("FFT Ocean capture player built at " + executablePath);
    }
}
#endif
