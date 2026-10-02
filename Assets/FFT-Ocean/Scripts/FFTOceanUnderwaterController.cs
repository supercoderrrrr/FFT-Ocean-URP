using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public sealed class FFTOceanUnderwaterController : MonoBehaviour
{
    [Header("Required References")]
    [SerializeField] private FFTOceanController ocean;
    [SerializeField] private Light mainLight;

    [Header("Waterline")]
    [SerializeField, Range(0.01f, 0.5f)] private float enterDepth = 0.08f;
    [SerializeField, Range(0.01f, 0.75f)] private float exitHeight = 0.14f;
    [SerializeField, Range(0.05f, 2f)] private float waterlineFeather = 0.28f;
    [SerializeField, Range(0.25f, 5f)] private float surfaceTransitionRange = 1.5f;

    [Header("Underwater Optics")]
    [SerializeField] private Vector3 absorption = new Vector3(0.09f, 0.032f, 0.015f);
    [SerializeField] private Color scatteringColor = new Color(0.015f, 0.14f, 0.20f, 1f);
    [SerializeField, Range(0.005f, 0.2f)] private float scatteringDensity = 0.025f;
    [SerializeField, Range(10f, 250f)] private float maximumVisibility = 140f;
    [SerializeField, Range(0f, 0.02f)] private float distortionStrength = 0.0028f;
    [SerializeField, Range(0.02f, 1f)] private float distortionScale = 0.16f;
    [SerializeField, Range(0f, 1f)] private float distortionSpeed = 0.055f;

    [Header("Caustics And Atmosphere")]
    [SerializeField, Tooltip("Seamless grayscale caustics texture. Enable Repeat and mipmaps in its import settings.")]
    private Texture2D causticsTexture;
    [SerializeField] private Color causticsTint = new Color(0.72f, 0.9f, 1f, 1f);
    [SerializeField, Range(0f, 3f)] private float causticsIntensity = 0.6f;
    [SerializeField, Range(0.01f, 1f), Tooltip("Texture tiles per world metre. Lower values create larger caustic cells.")]
    private float causticsScale = 0.12f;
    [FormerlySerializedAs("causticsSharpness")]
    [SerializeField, Range(0.25f, 4f), Tooltip("Raises texture contrast. Values above one produce narrower bright lines.")]
    private float causticsContrast = 1.8f;
    [SerializeField, Range(1f, 80f)] private float causticsDepthFade = 30f;
    [SerializeField, Range(0f, 2f), Tooltip("Continuous counter-scroll speed. It never reverses and the world-space centre remains fixed.")]
    private float causticsSpeed = 0.18f;
    [FormerlySerializedAs("causticsShimmer")]
    [SerializeField, Range(0f, 0.15f), Tooltip("Relative UV travel multiplier for the two oppositely scrolling texture layers.")]
    private float causticsRelativeMotion = 0.025f;
    [SerializeField, Range(0f, 1f)] private float suspendedParticles = 0f;
    [SerializeField, Range(0f, 1f)] private float lightShaftStrength = 0.12f;

    private Camera targetCamera;
    private bool underwater;
    private float cameraWaterDepth;

    public bool IsUnderwater => underwater;
    public Camera TargetCamera => targetCamera;
    public bool IsEffectActive => Application.isPlaying
        && enabled
        && ocean != null
        && ocean.IsSimulationReady
        && (underwater || cameraWaterDepth > -surfaceTransitionRange);

    private void Awake()
    {
        targetCamera = GetComponent<Camera>();
    }

    private void OnEnable()
    {
        if (targetCamera == null)
            targetCamera = GetComponent<Camera>();
        RefreshState();
    }

    private void LateUpdate()
    {
        RefreshState();
    }

    private void OnValidate()
    {
        absorption.x = Mathf.Max(0f, absorption.x);
        absorption.y = Mathf.Max(0f, absorption.y);
        absorption.z = Mathf.Max(0f, absorption.z);
        maximumVisibility = Mathf.Max(10f, maximumVisibility);
        waterlineFeather = Mathf.Max(0.05f, waterlineFeather);
        surfaceTransitionRange = Mathf.Max(0.25f, surfaceTransitionRange);
    }

    private void RefreshState()
    {
        if (ocean == null)
        {
            underwater = false;
            cameraWaterDepth = float.NegativeInfinity;
            return;
        }

        cameraWaterDepth = ocean.SampledSurfaceHeight - transform.position.y;
        if (underwater)
        {
            if (cameraWaterDepth < -exitHeight)
                underwater = false;
        }
        else if (cameraWaterDepth > enterDepth)
        {
            underwater = true;
        }
    }

    public void ConfigureMaterial(Material material)
    {
        if (material == null || ocean == null)
            return;

        Vector3 sunRayDirection = mainLight != null ? mainLight.transform.forward : Vector3.down;
        Color sunColor = mainLight != null ? mainLight.color * mainLight.intensity : Color.white;

        material.SetFloat("_WaterSurfaceHeight", ocean.SampledSurfaceHeight);
        material.SetFloat("_CausticsProjectionHeight", ocean.transform.position.y);
        material.SetFloat("_CameraWaterDepth", cameraWaterDepth);
        material.SetFloat("_WaterlineFeather", waterlineFeather);
        material.SetVector("_Absorption", absorption);
        material.SetColor("_ScatteringColor", scatteringColor);
        material.SetFloat("_ScatteringDensity", scatteringDensity);
        material.SetFloat("_MaximumVisibility", maximumVisibility);
        material.SetFloat("_DistortionStrength", distortionStrength);
        material.SetFloat("_DistortionScale", distortionScale);
        material.SetFloat("_DistortionSpeed", distortionSpeed);
        material.SetTexture("_CausticsTexture", causticsTexture);
        material.SetFloat("_HasCausticsTexture", causticsTexture != null ? 1f : 0f);
        material.SetColor("_CausticsTint", causticsTint);
        material.SetFloat("_CausticsIntensity", causticsIntensity);
        material.SetFloat("_CausticsScale", causticsScale);
        material.SetFloat("_CausticsContrast", causticsContrast);
        material.SetFloat("_CausticsDepthFade", causticsDepthFade);
        material.SetFloat("_CausticsSpeed", causticsSpeed);
        material.SetFloat("_CausticsRelativeMotion", causticsRelativeMotion);
        material.SetFloat("_SuspendedParticles", suspendedParticles);
        material.SetFloat("_LightShaftStrength", lightShaftStrength);
        material.SetVector("_SunRayDirection", sunRayDirection);
        material.SetColor("_UnderwaterSunColor", sunColor);
    }
}
