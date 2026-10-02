#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class FFTOceanPortfolioCapture : MonoBehaviour
{
    private const int CaptureWidth = 1280;
    private const int CaptureHeight = 720;
    private const int SimulationFrameRate = 30;
    private const int OutputFrameRate = 15;
    private const int FramesPerPreset = 30;

    private static readonly CapturePreset[] Presets =
    {
        new CapturePreset("baseline", 0.5f, 1f, 1f),
        new CapturePreset("sharp-crests", 0.5f, 1.12f, 1f),
        new CapturePreset("stronger-wind", 0.85f, 1.08f, 1f)
    };

    private Camera captureCamera;
    private FFTOceanController ocean;
    private RenderTexture renderTexture;
    private Texture2D readbackTexture;
    private string outputDirectory;
    private int outputFrameIndex;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateWhenRequested()
    {
        string[] arguments = Environment.GetCommandLineArgs();
        int argumentIndex = Array.IndexOf(arguments, "-fftOceanCapture");
        if (argumentIndex < 0 || argumentIndex + 1 >= arguments.Length)
            return;

        GameObject captureObject = new GameObject("FFT Ocean Portfolio Capture");
        FFTOceanPortfolioCapture capture = captureObject.AddComponent<FFTOceanPortfolioCapture>();
        capture.outputDirectory = Path.GetFullPath(arguments[argumentIndex + 1]);
        DontDestroyOnLoad(captureObject);
    }

    private IEnumerator Start()
    {
        captureCamera = Camera.main;
        ocean = FindObjectOfType<FFTOceanController>();
        if (captureCamera == null || ocean == null)
        {
            Debug.LogError("FFT Ocean capture requires a main camera and ocean controller");
            Application.Quit(2);
            yield break;
        }

        FFTOceanShowcaseCamera showcaseCamera = captureCamera.GetComponent<FFTOceanShowcaseCamera>();
        if (showcaseCamera != null)
            showcaseCamera.enabled = false;

        Directory.CreateDirectory(outputDirectory);
        ConfigureCaptureCamera();
        Time.captureFramerate = SimulationFrameRate;
        Application.targetFrameRate = SimulationFrameRate;
        QualitySettings.vSyncCount = 0;

        renderTexture = new RenderTexture(CaptureWidth, CaptureHeight, 24, RenderTextureFormat.ARGB32)
        {
            name = "FFT Ocean Portfolio Capture",
            antiAliasing = 1
        };
        renderTexture.Create();
        readbackTexture = new Texture2D(CaptureWidth, CaptureHeight, TextureFormat.RGB24, false, false);

        while (!ocean.IsSimulationReady)
            yield return null;

        for (int presetIndex = 0; presetIndex < Presets.Length; presetIndex++)
        {
            CapturePreset preset = Presets[presetIndex];
            ocean.ApplySimulationParameters(preset.windSpeed, preset.choppiness, preset.simulationSpeed);

            for (int warmupFrame = 0; warmupFrame < SimulationFrameRate; warmupFrame++)
                yield return null;

            for (int frameIndex = 0; frameIndex < FramesPerPreset; frameIndex++)
            {
                for (int simulationStep = 0; simulationStep < SimulationFrameRate / OutputFrameRate; simulationStep++)
                {
                    AnimateCamera(outputFrameIndex + simulationStep / 2f);
                    yield return null;
                }

                string frameName = $"frame-{outputFrameIndex:0000}-{preset.label}.png";
                CaptureFrame(Path.Combine(outputDirectory, frameName));
                if (frameIndex == FramesPerPreset / 2)
                    CaptureFrame(Path.Combine(outputDirectory, $"still-{preset.label}.png"));
                outputFrameIndex++;
            }
        }

        captureCamera.transform.SetPositionAndRotation(
            new Vector3(0f, -8f, -12f),
            Quaternion.Euler(18f, 0f, 0f));
        for (int warmupFrame = 0; warmupFrame < SimulationFrameRate; warmupFrame++)
            yield return null;
        CaptureFrame(Path.Combine(outputDirectory, "still-underwater.png"));

        File.WriteAllText(
            Path.Combine(outputDirectory, "capture-complete.txt"),
            $"{outputFrameIndex} frames at {OutputFrameRate} fps");
        yield return null;
        Application.Quit(0);
    }

    private void ConfigureCaptureCamera()
    {
        captureCamera.transform.SetPositionAndRotation(
            new Vector3(0f, 4.8f, -15f),
            Quaternion.Euler(7f, 0f, 0f));
        captureCamera.fieldOfView = 58f;
    }

    private void AnimateCamera(float frame)
    {
        float time = frame / OutputFrameRate;
        Vector3 position = new Vector3(
            Mathf.Sin(time * 0.16f) * 2.5f,
            4.8f + Mathf.Sin(time * 0.31f) * 0.18f,
            -15f + time * 0.45f);
        captureCamera.transform.SetPositionAndRotation(
            position,
            Quaternion.Euler(7f + Mathf.Sin(time * 0.2f) * 0.4f, time * 0.45f, 0f));
    }

    private void CaptureFrame(string path)
    {
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture previousTarget = captureCamera.targetTexture;
        captureCamera.targetTexture = renderTexture;
        captureCamera.Render();
        RenderTexture.active = renderTexture;
        readbackTexture.ReadPixels(new Rect(0f, 0f, CaptureWidth, CaptureHeight), 0, 0, false);
        readbackTexture.Apply(false, false);
        File.WriteAllBytes(path, readbackTexture.EncodeToPNG());
        captureCamera.targetTexture = previousTarget;
        RenderTexture.active = previousActive;
    }

    private void OnDestroy()
    {
        Time.captureFramerate = 0;
        if (readbackTexture != null)
            Destroy(readbackTexture);
        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }
    }

    private readonly struct CapturePreset
    {
        public readonly string label;
        public readonly float windSpeed;
        public readonly float choppiness;
        public readonly float simulationSpeed;

        public CapturePreset(string label, float windSpeed, float choppiness, float simulationSpeed)
        {
            this.label = label;
            this.windSpeed = windSpeed;
            this.choppiness = choppiness;
            this.simulationSpeed = simulationSpeed;
        }
    }
}
#endif
