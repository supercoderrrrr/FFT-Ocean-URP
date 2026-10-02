using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
[RequireComponent(typeof(FFTOceanMesh))]
public sealed class FFTOceanController : MonoBehaviour
{
    public enum DebugView
    {
        FinalOcean = 0,
        Displacement = 1,
        Normal = 2,
        Foam = 3,
        CascadeLod = 4
    }

    [Serializable]
    private struct CascadeSettings
    {
        [Min(2f)] public float domainSize;
        [Min(0f)] public float spectrumScale;
        [Min(0f)] public float shortWaveDamping;
        [Min(0.01f)] public float minWavelength;
        [Min(0.02f)] public float maxWavelength;
        [Range(0f, 2f)] public float choppinessScale;
        public int seedOffset;
    }

    [Header("Required Assets")]
    [SerializeField] private ComputeShader oceanCompute;
    [SerializeField] private Material oceanMaterial;

    [Header("FFT")]
    [SerializeField, Range(64, 512)] private int resolution = 256;
    [SerializeField] private int seed = 2026;
    [SerializeField, Min(0.01f)] private float simulationSpeed = 1f;

    [Header("Environment")]
    [SerializeField, Min(1f)] private float waterDepth = 500f;
    [SerializeField, Range(0.1f, 30f)] private float windSpeed = 0.5f;
    [SerializeField, Range(-180f, 180f)] private float windDirectionDegrees = -29.81f;
    [SerializeField, Min(100f)] private float fetch = 100000f;
    [SerializeField, Range(1f, 8f)] private float peakEnhancement = 3.3f;
    [FormerlySerializedAs("directionalSpread")]
    [SerializeField, Range(0f, 1f)] private float spreadBlend = 1f;
    [SerializeField, Range(0.01f, 1f)] private float swell = 0.198f;
    [SerializeField, Range(0f, 2f)] private float choppiness = 1f;

    [Header("Three Non-overlapping Spectral Cascades")]
    [SerializeField] private CascadeSettings largeWaves = new CascadeSettings
    {
        domainSize = 250f,
        spectrumScale = 1f,
        shortWaveDamping = 0.01f,
        minWavelength = 2.833333f,
        maxWavelength = 100000f,
        choppinessScale = 1f,
        seedOffset = 0
    };

    [FormerlySerializedAs("detailWaves")]
    [SerializeField] private CascadeSettings midWaves = new CascadeSettings
    {
        domainSize = 17f,
        spectrumScale = 1f,
        shortWaveDamping = 0.01f,
        minWavelength = 0.833333f,
        maxWavelength = 2.833333f,
        choppinessScale = 1f,
        seedOffset = 7919
    };

    [SerializeField] private CascadeSettings shortWaves = new CascadeSettings
    {
        domainSize = 5f,
        spectrumScale = 1f,
        shortWaveDamping = 0.01f,
        minWavelength = 0.039f,
        maxWavelength = 0.833333f,
        choppinessScale = 1f,
        seedOffset = 15401
    };

    [Header("Breaking-wave history")]
    [SerializeField, Range(0.05f, 2f)] private float turbulenceRecovery = 0.5f;

    [Header("Presentation")]
    [SerializeField] private DebugView debugView;
    [SerializeField] private Transform followTarget;
    [SerializeField, Min(0.05f)] private float followSnap = 0.125f;
    [SerializeField, Range(0f, 200f)] private float followForwardOffset;
    [SerializeField, Range(0.04f, 0.5f)] private float heightProbeInterval = 0.08f;

    private readonly Cascade[] cascades = { new Cascade(), new Cascade(), new Cascade() };
    private FFTOceanMesh oceanMesh;
    private IReadOnlyList<MeshRenderer> meshRenderers;
    private MaterialPropertyBlock propertyBlock;
    private float simulationTime;
    private bool initialized;
    private bool spectrumDirty = true;
    private ComputeBuffer heightProbeBuffer;
    private bool heightReadbackPending;
    private bool hasSurfaceHeightSample;
    private float nextHeightProbeTime;
    private float sampledSurfaceHeight;

    public float SampledSurfaceHeight => hasSurfaceHeightSample ? sampledSurfaceHeight : transform.position.y;
    public bool IsSimulationReady => initialized;

    private static readonly int[] DisplacementIds =
    {
        Shader.PropertyToID("_Displacement0"),
        Shader.PropertyToID("_Displacement1"),
        Shader.PropertyToID("_Displacement2")
    };

    private static readonly int[] SurfaceDataIds =
    {
        Shader.PropertyToID("_SurfaceData0"),
        Shader.PropertyToID("_SurfaceData1"),
        Shader.PropertyToID("_SurfaceData2")
    };

    private static readonly int DomainSizesId = Shader.PropertyToID("_DomainSizes");
    private static readonly int SpectrumResolutionId = Shader.PropertyToID("_SpectrumResolution");
    private static readonly int DebugViewId = Shader.PropertyToID("_DebugView");
    private static readonly int ClipmapSpacingId = Shader.PropertyToID("_ClipmapSpacing");
    private static readonly int ClipmapHalfExtentId = Shader.PropertyToID("_ClipmapHalfExtent");
    private static readonly int[] SpectrumIds =
    {
        Shader.PropertyToID("_Spectrum0"),
        Shader.PropertyToID("_Spectrum1"),
        Shader.PropertyToID("_Spectrum2"),
        Shader.PropertyToID("_Spectrum3")
    };
    private static readonly int[] InputIds =
    {
        Shader.PropertyToID("_Input0"),
        Shader.PropertyToID("_Input1"),
        Shader.PropertyToID("_Input2"),
        Shader.PropertyToID("_Input3")
    };
    private static readonly int[] OutputIds =
    {
        Shader.PropertyToID("_Output0"),
        Shader.PropertyToID("_Output1"),
        Shader.PropertyToID("_Output2"),
        Shader.PropertyToID("_Output3")
    };

    private sealed class Cascade
    {
        public RenderTexture initialSpectrum;
        public readonly RenderTexture[] ping = new RenderTexture[4];
        public readonly RenderTexture[] pong = new RenderTexture[4];
        public RenderTexture displacement;
        public RenderTexture surfaceData;

        public void Release()
        {
            ReleaseTexture(ref initialSpectrum);
            for (int i = 0; i < 4; i++)
            {
                ReleaseTexture(ref ping[i]);
                ReleaseTexture(ref pong[i]);
            }
            ReleaseTexture(ref displacement);
            ReleaseTexture(ref surfaceData);
        }

        private static void ReleaseTexture(ref RenderTexture texture)
        {
            if (texture == null)
                return;
            if (texture.IsCreated())
                texture.Release();
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(texture);
            else
                UnityEngine.Object.DestroyImmediate(texture);
            texture = null;
        }
    }

    private int initializeKernel;
    private int updateKernel;
    private int bitReverseHorizontalKernel;
    private int stageHorizontalKernel;
    private int bitReverseVerticalKernel;
    private int stageVerticalKernel;
    private int assembleKernel;
    private int sampleSurfaceHeightKernel;

    private void Awake()
    {
        oceanMesh = GetComponent<FFTOceanMesh>();
        propertyBlock = new MaterialPropertyBlock();
    }

    private void OnEnable()
    {
        if (!Application.isPlaying)
            return;
        if (!ValidateSupport())
        {
            enabled = false;
            return;
        }
        Initialize();
    }

    private void Update()
    {
        if (!initialized)
            return;

        FollowPresentationCamera();
        simulationTime += Time.deltaTime * simulationSpeed;
        if (spectrumDirty)
        {
            InitializeSpectrum(cascades[0], largeWaves, 0);
            InitializeSpectrum(cascades[1], midWaves, 1);
            InitializeSpectrum(cascades[2], shortWaves, 2);
            spectrumDirty = false;
        }

        SimulateCascade(cascades[0], largeWaves);
        SimulateCascade(cascades[1], midWaves);
        SimulateCascade(cascades[2], shortWaves);
        BindRendererTextures();
        UpdateSurfaceHeightProbe();
    }

    private void OnDisable()
    {
        ReleaseResources();
    }

    private void FollowPresentationCamera()
    {
        if (followTarget == null)
            return;

        Vector3 currentPosition = transform.position;
        Vector3 flatForward = Vector3.ProjectOnPlane(followTarget.forward, Vector3.up).normalized;
        Vector3 targetPosition = followTarget.position + flatForward * followForwardOffset;
        currentPosition.x = Mathf.Round(targetPosition.x / followSnap) * followSnap;
        currentPosition.z = Mathf.Round(targetPosition.z / followSnap) * followSnap;
        transform.position = currentPosition;
    }

    private void OnValidate()
    {
        resolution = Mathf.Clamp(Mathf.ClosestPowerOfTwo(resolution), 64, 512);
        windSpeed = Mathf.Max(0.1f, windSpeed);
        fetch = Mathf.Max(100f, fetch);
        followSnap = Mathf.Max(0.05f, followSnap);
        heightProbeInterval = Mathf.Clamp(heightProbeInterval, 0.04f, 0.5f);
        ValidateCascade(ref largeWaves);
        ValidateCascade(ref midWaves);
        ValidateCascade(ref shortWaves);
        spectrumDirty = true;
    }

    private static void ValidateCascade(ref CascadeSettings settings)
    {
        settings.domainSize = Mathf.Max(2f, settings.domainSize);
        settings.minWavelength = Mathf.Max(0.01f, settings.minWavelength);
        settings.maxWavelength = Mathf.Max(settings.minWavelength * 1.001f, settings.maxWavelength);
        settings.choppinessScale = Mathf.Clamp(settings.choppinessScale, 0f, 2f);
    }

    [ContextMenu("Rebuild Spectrum")]
    private void RebuildSpectrum()
    {
        spectrumDirty = true;
    }

    private bool ValidateSupport()
    {
        if (!SystemInfo.supportsComputeShaders)
        {
            Debug.LogError("FFT Ocean requires Compute Shader support.", this);
            return false;
        }
        if (oceanCompute == null || oceanMaterial == null)
        {
            Debug.LogError("FFT Ocean is missing its Compute Shader or Material reference.", this);
            return false;
        }
        return true;
    }

    private void Initialize()
    {
        ReleaseResources();
        oceanMesh.EnsureCreated();
        meshRenderers = oceanMesh.Renderers;
        if (meshRenderers.Count == 0)
        {
            Debug.LogError("FFT Ocean clipmap did not create any renderers.", this);
            enabled = false;
            return;
        }

        FindKernels();
        CreateCascadeResources(cascades[0], "Large");
        CreateCascadeResources(cascades[1], "Mid");
        CreateCascadeResources(cascades[2], "Detail");
        heightProbeBuffer = new ComputeBuffer(1, sizeof(float), ComputeBufferType.Structured);
        sampledSurfaceHeight = transform.position.y;
        hasSurfaceHeightSample = false;
        heightReadbackPending = false;
        nextHeightProbeTime = 0f;
        foreach (MeshRenderer renderer in meshRenderers)
            renderer.sharedMaterial = oceanMaterial;

        simulationTime = 0f;
        spectrumDirty = true;
        initialized = true;
    }

    private void FindKernels()
    {
        initializeKernel = oceanCompute.FindKernel("InitializeSpectrum");
        updateKernel = oceanCompute.FindKernel("UpdateSpectrum");
        bitReverseHorizontalKernel = oceanCompute.FindKernel("BitReverseHorizontal");
        stageHorizontalKernel = oceanCompute.FindKernel("FFTStageHorizontal");
        bitReverseVerticalKernel = oceanCompute.FindKernel("BitReverseVertical");
        stageVerticalKernel = oceanCompute.FindKernel("FFTStageVertical");
        assembleKernel = oceanCompute.FindKernel("AssembleOcean");
        sampleSurfaceHeightKernel = oceanCompute.FindKernel("SampleSurfaceHeight");
    }

    private void CreateCascadeResources(Cascade cascade, string label)
    {
        cascade.initialSpectrum = CreateTexture($"FFT {label} Initial Spectrum", RenderTextureFormat.ARGBFloat, false);
        for (int i = 0; i < 4; i++)
        {
            cascade.ping[i] = CreateTexture($"FFT {label} Ping {i}", RenderTextureFormat.ARGBFloat, false);
            cascade.pong[i] = CreateTexture($"FFT {label} Pong {i}", RenderTextureFormat.ARGBFloat, false);
        }
        cascade.displacement = CreateTexture($"FFT {label} Displacement", RenderTextureFormat.ARGBHalf, true);
        cascade.surfaceData = CreateTexture($"FFT {label} Surface Data", RenderTextureFormat.ARGBHalf, true);
    }

    private RenderTexture CreateTexture(string textureName, RenderTextureFormat format, bool useMipMaps)
    {
        RenderTexture texture = new RenderTexture(resolution, resolution, 0, format, RenderTextureReadWrite.Linear)
        {
            name = textureName,
            dimension = TextureDimension.Tex2D,
            enableRandomWrite = true,
            wrapMode = TextureWrapMode.Repeat,
            filterMode = useMipMaps ? FilterMode.Trilinear : FilterMode.Bilinear,
            anisoLevel = useMipMaps ? 4 : 1,
            useMipMap = useMipMaps,
            autoGenerateMips = false
        };
        texture.Create();
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = texture;
        GL.Clear(false, true, Color.clear);
        RenderTexture.active = previous;
        return texture;
    }

    private void SetSharedSimulationParameters(CascadeSettings settings)
    {
        float directionRadians = windDirectionDegrees * Mathf.Deg2Rad;
        Vector2 windDirection = new Vector2(Mathf.Cos(directionRadians), Mathf.Sin(directionRadians));
        oceanCompute.SetInt("_Resolution", resolution);
        oceanCompute.SetInt("_LogResolution", (int)Mathf.Log(resolution, 2));
        oceanCompute.SetFloat("_Gravity", 9.81f);
        oceanCompute.SetFloat("_Depth", waterDepth);
        oceanCompute.SetFloat("_DomainSize", settings.domainSize);
        oceanCompute.SetFloat("_WindSpeed", windSpeed);
        oceanCompute.SetVector("_WindDirection", windDirection);
        oceanCompute.SetFloat("_Fetch", fetch);
        oceanCompute.SetFloat("_SpectrumScale", settings.spectrumScale);
        oceanCompute.SetFloat("_PeakEnhancement", peakEnhancement);
        oceanCompute.SetFloat("_SpreadBlend", spreadBlend);
        oceanCompute.SetFloat("_Swell", swell);
        oceanCompute.SetFloat("_ShortWaveDamping", settings.shortWaveDamping);
        oceanCompute.SetFloat("_MinWavelength", settings.minWavelength);
        oceanCompute.SetFloat("_MaxWavelength", settings.maxWavelength);
        oceanCompute.SetFloat("_Choppiness", choppiness * settings.choppinessScale);
        oceanCompute.SetFloat("_TurbulenceRecovery", turbulenceRecovery);
    }

    private void InitializeSpectrum(Cascade cascade, CascadeSettings settings, int cascadeIndex)
    {
        SetSharedSimulationParameters(settings);
        oceanCompute.SetInt("_Seed", seed + settings.seedOffset + cascadeIndex * 104729);
        oceanCompute.SetTexture(initializeKernel, "_InitialSpectrum", cascade.initialSpectrum);
        Dispatch2D(initializeKernel);
    }

    private void SimulateCascade(Cascade cascade, CascadeSettings settings)
    {
        SetSharedSimulationParameters(settings);
        oceanCompute.SetFloat("_Time", simulationTime);
        oceanCompute.SetFloat("_DeltaTime", Mathf.Min(Time.deltaTime, 0.05f));
        oceanCompute.SetTexture(updateKernel, "_InitialSpectrumRead", cascade.initialSpectrum);
        BindWritableFields(updateKernel, cascade.ping);
        Dispatch2D(updateKernel);

        RenderTexture[] source = cascade.ping;
        RenderTexture[] destination = cascade.pong;
        DispatchTransform(bitReverseHorizontalKernel, source, destination);
        Swap(ref source, ref destination);
        for (int stageSize = 2; stageSize <= resolution; stageSize <<= 1)
        {
            oceanCompute.SetInt("_StageSize", stageSize);
            DispatchTransform(stageHorizontalKernel, source, destination);
            Swap(ref source, ref destination);
        }

        DispatchTransform(bitReverseVerticalKernel, source, destination);
        Swap(ref source, ref destination);
        for (int stageSize = 2; stageSize <= resolution; stageSize <<= 1)
        {
            oceanCompute.SetInt("_StageSize", stageSize);
            DispatchTransform(stageVerticalKernel, source, destination);
            Swap(ref source, ref destination);
        }

        BindReadableFields(assembleKernel, source);
        oceanCompute.SetTexture(assembleKernel, "_Displacement", cascade.displacement);
        oceanCompute.SetTexture(assembleKernel, "_SurfaceData", cascade.surfaceData);
        Dispatch2D(assembleKernel);
        cascade.displacement.GenerateMips();
        cascade.surfaceData.GenerateMips();
    }

    private void DispatchTransform(int kernel, RenderTexture[] source, RenderTexture[] destination)
    {
        BindReadableFields(kernel, source);
        BindWritableOutputs(kernel, destination);
        Dispatch2D(kernel);
    }

    private void BindReadableFields(int kernel, RenderTexture[] textures)
    {
        for (int i = 0; i < 4; i++)
            oceanCompute.SetTexture(kernel, InputIds[i], textures[i]);
    }

    private void BindWritableFields(int kernel, RenderTexture[] textures)
    {
        for (int i = 0; i < 4; i++)
            oceanCompute.SetTexture(kernel, SpectrumIds[i], textures[i]);
    }

    private void BindWritableOutputs(int kernel, RenderTexture[] textures)
    {
        for (int i = 0; i < 4; i++)
            oceanCompute.SetTexture(kernel, OutputIds[i], textures[i]);
    }

    private void BindRendererTextures()
    {
        propertyBlock.Clear();
        for (int i = 0; i < cascades.Length; i++)
        {
            propertyBlock.SetTexture(DisplacementIds[i], cascades[i].displacement);
            propertyBlock.SetTexture(SurfaceDataIds[i], cascades[i].surfaceData);
        }
        propertyBlock.SetVector(DomainSizesId, new Vector4(largeWaves.domainSize, midWaves.domainSize, shortWaves.domainSize, 0f));
        propertyBlock.SetFloat(SpectrumResolutionId, resolution);
        propertyBlock.SetFloat(DebugViewId, (float)debugView);
        for (int i = 0; i < meshRenderers.Count; i++)
        {
            float spacing = oceanMesh.Spacings[i];
            propertyBlock.SetFloat(ClipmapSpacingId, spacing);
            propertyBlock.SetFloat(ClipmapHalfExtentId, oceanMesh.PatchResolution * spacing * 0.5f);
            meshRenderers[i].SetPropertyBlock(propertyBlock);
        }
    }

    public void ConfigureUnderwaterMaterial(Material material)
    {
        if (!initialized || material == null)
            return;

        material.SetTexture("_OceanDisplacement0", cascades[0].displacement);
        material.SetTexture("_OceanDisplacement1", cascades[1].displacement);
        material.SetTexture("_OceanDisplacement2", cascades[2].displacement);
        material.SetTexture("_OceanDerivatives0", cascades[0].surfaceData);
        material.SetTexture("_OceanDerivatives1", cascades[1].surfaceData);
        material.SetTexture("_OceanDerivatives2", cascades[2].surfaceData);
        material.SetVector("_OceanDomainSizes", new Vector4(
            largeWaves.domainSize,
            midWaves.domainSize,
            shortWaves.domainSize,
            0f));
    }

    private void UpdateSurfaceHeightProbe()
    {
        if (followTarget == null || heightProbeBuffer == null || heightReadbackPending
            || Time.unscaledTime < nextHeightProbeTime)
            return;

        oceanCompute.SetInt("_Resolution", resolution);
        oceanCompute.SetVector("_HeightProbeWorldXZ", new Vector4(
            followTarget.position.x,
            followTarget.position.z,
            0f,
            0f));
        oceanCompute.SetVector("_HeightProbeDomains", new Vector4(
            largeWaves.domainSize,
            midWaves.domainSize,
            shortWaves.domainSize,
            0f));
        oceanCompute.SetTexture(sampleSurfaceHeightKernel, "_ProbeDisplacement0", cascades[0].displacement);
        oceanCompute.SetTexture(sampleSurfaceHeightKernel, "_ProbeDisplacement1", cascades[1].displacement);
        oceanCompute.SetTexture(sampleSurfaceHeightKernel, "_ProbeDisplacement2", cascades[2].displacement);
        oceanCompute.SetBuffer(sampleSurfaceHeightKernel, "_HeightProbeResult", heightProbeBuffer);
        oceanCompute.Dispatch(sampleSurfaceHeightKernel, 1, 1, 1);

        heightReadbackPending = true;
        nextHeightProbeTime = Time.unscaledTime + heightProbeInterval;
        AsyncGPUReadback.Request(heightProbeBuffer, OnHeightProbeComplete);
    }

    private void OnHeightProbeComplete(AsyncGPUReadbackRequest request)
    {
        heightReadbackPending = false;
        if (!this || !initialized || request.hasError)
            return;

        var data = request.GetData<float>();
        if (data.Length == 0 || float.IsNaN(data[0]) || float.IsInfinity(data[0]))
            return;

        sampledSurfaceHeight = transform.position.y + data[0];
        hasSurfaceHeightSample = true;
    }

    private void Dispatch2D(int kernel)
    {
        int groups = Mathf.CeilToInt(resolution / 8f);
        oceanCompute.Dispatch(kernel, groups, groups, 1);
    }

    private static void Swap(ref RenderTexture[] a, ref RenderTexture[] b)
    {
        (a, b) = (b, a);
    }

    private void ReleaseResources()
    {
        initialized = false;
        heightReadbackPending = false;
        hasSurfaceHeightSample = false;
        if (heightProbeBuffer != null)
        {
            heightProbeBuffer.Release();
            heightProbeBuffer = null;
        }
        foreach (Cascade cascade in cascades)
            cascade.Release();
        if (meshRenderers == null)
            return;
        foreach (MeshRenderer renderer in meshRenderers)
        {
            if (renderer != null)
                renderer.SetPropertyBlock(null);
        }
    }
}
