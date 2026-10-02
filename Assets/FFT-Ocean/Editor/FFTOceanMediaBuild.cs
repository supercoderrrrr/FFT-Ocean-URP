#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class FFTOceanMediaBuild
{
    public static void BuildCapturePlayer()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string outputDirectory = Path.Combine(projectRoot, "Temp", "PortfolioCapturePlayer");
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
