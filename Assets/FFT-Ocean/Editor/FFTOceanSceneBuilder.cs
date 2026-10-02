#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class FFTOceanSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/FFT-Ocean.unity";
    private const string OceanComputePath = "Assets/FFT-Ocean/Compute/FFTOcean.compute";
    private const string OceanShaderPath = "Assets/FFT-Ocean/Shaders/FFTOceanURP.shader";
    private const string UnderwaterShaderPath = "Assets/FFT-Ocean/Shaders/FFTOceanUnderwaterURP.shader";
    private const string OceanMaterialPath = "Assets/FFT-Ocean/Materials/FFTOceanPortfolio.mat";
    private const string SkyMaterialPath = "Assets/FFT-Ocean/Materials/FFTOceanSky.mat";
    private const string SeabedMaterialPath = "Assets/FFT-Ocean/Materials/FFTOceanSeabed.mat";
    private const string CausticsTexturePath = "Assets/Textures/caustics_1.png";
    private const string HighFidelityRendererPath = "Assets/Settings/URP-HighFidelity-Renderer.asset";

    [MenuItem("Tools/FFT Ocean/Build Portfolio Scene")]
    public static void BuildPortfolioScene()
    {
        EnsureFolders();
        Material oceanMaterial = CreateOceanMaterial();
        Material skyMaterial = CreateSkyMaterial();
        Material seabedMaterial = CreateSeabedMaterial();
        ComputeShader oceanCompute = AssetDatabase.LoadAssetAtPath<ComputeShader>(OceanComputePath);
        Shader underwaterShader = AssetDatabase.LoadAssetAtPath<Shader>(UnderwaterShaderPath);
        if (oceanCompute == null)
            throw new MissingReferenceException($"Missing compute shader at {OceanComputePath}");
        if (underwaterShader == null)
            throw new MissingReferenceException($"Missing underwater shader at {UnderwaterShaderPath}");

        EnsureUnderwaterRendererFeature(underwaterShader);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "FFT-Ocean";

        Camera camera = CreateCamera();
        Light sun = CreateSun();
        FFTOceanController ocean = CreateOcean(oceanCompute, oceanMaterial, camera.transform);
        ConfigureUnderwaterCamera(camera, ocean, sun);
        CreateSeabed(seabedMaterial);
        ConfigureEnvironment(skyMaterial);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeGameObject = GameObject.Find("FFT Ocean");
        Debug.Log("FFT Ocean portfolio scene built successfully: " + ScenePath);
    }

    public static void BuildFromCommandLine()
    {
        BuildPortfolioScene();
    }

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/FFT-Ocean/Editor"))
            AssetDatabase.CreateFolder("Assets/FFT-Ocean", "Editor");
        if (!AssetDatabase.IsValidFolder("Assets/FFT-Ocean/Materials"))
            AssetDatabase.CreateFolder("Assets/FFT-Ocean", "Materials");
    }

    private static Material CreateOceanMaterial()
    {
        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(OceanShaderPath);
        if (shader == null)
            throw new MissingReferenceException($"Missing ocean shader at {OceanShaderPath}");

        Material material = AssetDatabase.LoadAssetAtPath<Material>(OceanMaterialPath);
        if (material == null)
        {
            material = new Material(shader) { name = "FFT Ocean Portfolio" };
            AssetDatabase.CreateAsset(material, OceanMaterialPath);
        }
        else
        {
            material.shader = shader;
        }

        material.SetColor("_DeepColor", new Color(0.03457636f, 0.12297464f, 0.1981132f, 1f));
        material.SetColor("_SubsurfaceColor", new Color(0.1541919f, 0.8857628f, 0.990566f, 1f));
        material.SetColor("_FoamColor", Color.white);
        material.SetFloat("_LodScale", 7.13f);
        material.SetFloat("_NormalStrength", 1f);
        material.SetFloat("_Smoothness", 0.91f);
        material.SetFloat("_DistantSmoothness", 0.689f);
        material.SetFloat("_RoughnessScale", 0.0044f);
        material.SetFloat("_FresnelPower", 5f);
        material.SetFloat("_ReflectionStrength", 1f);
        material.SetFloat("_SunGlitter", 1f);
        material.SetFloat("_SubsurfaceStrength", 0.133f);
        material.SetFloat("_SubsurfaceScale", 4.8f);
        material.SetFloat("_SubsurfaceBase", -0.1f);
        material.SetFloat("_FoamBiasLOD0", 0.84f);
        material.SetFloat("_FoamBiasLOD1", 1.83f);
        material.SetFloat("_FoamBiasLOD2", 2.72f);
        material.SetFloat("_FoamScale", 2.4f);
        material.SetFloat("_ContactFoam", 1f);
        material.SetFloat("_ContactFoamDistance", 0.75f);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material CreateSkyMaterial()
    {
        Shader shader = Shader.Find("Skybox/Procedural");
        if (shader == null)
            throw new MissingReferenceException("Built-in procedural skybox shader is unavailable.");

        Material material = AssetDatabase.LoadAssetAtPath<Material>(SkyMaterialPath);
        if (material == null)
        {
            material = new Material(shader) { name = "FFT Ocean Sky" };
            AssetDatabase.CreateAsset(material, SkyMaterialPath);
        }
        material.SetFloat("_SunSize", 0.035f);
        material.SetFloat("_SunSizeConvergence", 5f);
        material.SetFloat("_AtmosphereThickness", 0.88f);
        material.SetColor("_SkyTint", new Color(0.42f, 0.58f, 0.72f, 1f));
        material.SetColor("_GroundColor", new Color(0.12f, 0.16f, 0.19f, 1f));
        material.SetFloat("_Exposure", 1.05f);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material CreateSeabedMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            throw new MissingReferenceException("URP Lit shader is unavailable.");

        Material material = AssetDatabase.LoadAssetAtPath<Material>(SeabedMaterialPath);
        if (material == null)
        {
            material = new Material(shader) { name = "FFT Ocean Seabed" };
            AssetDatabase.CreateAsset(material, SeabedMaterialPath);
        }
        else
        {
            material.shader = shader;
        }

        material.SetColor("_BaseColor", new Color(0.16f, 0.19f, 0.17f, 1f));
        material.SetFloat("_Smoothness", 0.12f);
        material.SetFloat("_Metallic", 0f);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Camera CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetPositionAndRotation(new Vector3(0f, 3.61f, -10f), Quaternion.Euler(0f, 164.11f, 0f));
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.Skybox;
        camera.fieldOfView = 60f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 5000f;
        camera.allowHDR = true;
        UniversalAdditionalCameraData cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
        cameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        cameraData.antialiasingQuality = AntialiasingQuality.High;
        cameraData.requiresDepthTexture = true;
        cameraData.requiresColorTexture = true;
        cameraObject.AddComponent<AudioListener>();
        cameraObject.AddComponent<FFTOceanShowcaseCamera>();
        return camera;
    }

    private static Light CreateSun()
    {
        GameObject lightObject = new GameObject("Sun");
        lightObject.transform.rotation = Quaternion.Euler(9.9f, -30f, 0f);
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.95686275f, 0.8392157f, 1f);
        light.intensity = 1f;
        light.shadows = LightShadows.Soft;
        light.shadowStrength = 1f;
        lightObject.AddComponent<UniversalAdditionalLightData>();
        RenderSettings.sun = light;
        return light;
    }

    private static FFTOceanController CreateOcean(ComputeShader oceanCompute, Material material, Transform cameraTransform)
    {
        GameObject oceanObject = new GameObject("FFT Ocean");
        FFTOceanMesh mesh = oceanObject.AddComponent<FFTOceanMesh>();
        SerializedObject meshSerialized = new SerializedObject(mesh);
        meshSerialized.FindProperty("patchResolution").intValue = 240;
        meshSerialized.FindProperty("clipLevels").intValue = 8;
        meshSerialized.FindProperty("baseVertexSpacing").floatValue = 0.125f;
        meshSerialized.FindProperty("maximumExpectedWaveHeight").floatValue = 32f;
        meshSerialized.ApplyModifiedPropertiesWithoutUndo();

        FFTOceanController controller = oceanObject.AddComponent<FFTOceanController>();
        SerializedObject controllerSerialized = new SerializedObject(controller);
        controllerSerialized.FindProperty("oceanCompute").objectReferenceValue = oceanCompute;
        controllerSerialized.FindProperty("oceanMaterial").objectReferenceValue = material;
        controllerSerialized.FindProperty("resolution").intValue = 256;
        controllerSerialized.FindProperty("seed").intValue = 20260813;
        controllerSerialized.FindProperty("simulationSpeed").floatValue = 1f;
        controllerSerialized.FindProperty("waterDepth").floatValue = 500f;
        controllerSerialized.FindProperty("windSpeed").floatValue = 0.5f;
        controllerSerialized.FindProperty("windDirectionDegrees").floatValue = -29.81f;
        controllerSerialized.FindProperty("fetch").floatValue = 100000f;
        controllerSerialized.FindProperty("peakEnhancement").floatValue = 3.3f;
        controllerSerialized.FindProperty("spreadBlend").floatValue = 1f;
        controllerSerialized.FindProperty("swell").floatValue = 0.198f;
        controllerSerialized.FindProperty("choppiness").floatValue = 1f;
        controllerSerialized.FindProperty("turbulenceRecovery").floatValue = 0.5f;
        controllerSerialized.FindProperty("debugView").enumValueIndex = 0;
        controllerSerialized.FindProperty("followTarget").objectReferenceValue = cameraTransform;
        controllerSerialized.FindProperty("followSnap").floatValue = 0.125f;
        controllerSerialized.FindProperty("followForwardOffset").floatValue = 0f;
        SetCascade(controllerSerialized.FindProperty("largeWaves"), 250f, 1f, 0.01f, 2.833333f, 100000f, 1f, 0);
        SetCascade(controllerSerialized.FindProperty("midWaves"), 17f, 1f, 0.01f, 0.833333f, 2.833333f, 1f, 7919);
        SetCascade(controllerSerialized.FindProperty("shortWaves"), 5f, 1f, 0.01f, 0.039f, 0.833333f, 1f, 15401);
        controllerSerialized.ApplyModifiedPropertiesWithoutUndo();
        return controller;
    }

    private static void ConfigureUnderwaterCamera(Camera camera, FFTOceanController ocean, Light sun)
    {
        FFTOceanUnderwaterController underwater = camera.gameObject.AddComponent<FFTOceanUnderwaterController>();
        SerializedObject serialized = new SerializedObject(underwater);
        serialized.FindProperty("ocean").objectReferenceValue = ocean;
        serialized.FindProperty("mainLight").objectReferenceValue = sun;
        serialized.FindProperty("absorption").vector3Value = new Vector3(0.09f, 0.032f, 0.015f);
        serialized.FindProperty("scatteringColor").colorValue = new Color(0.015f, 0.14f, 0.20f, 1f);
        serialized.FindProperty("scatteringDensity").floatValue = 0.025f;
        serialized.FindProperty("maximumVisibility").floatValue = 140f;
        serialized.FindProperty("causticsTint").colorValue = new Color(0.72f, 0.9f, 1f, 1f);
        serialized.FindProperty("causticsTexture").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(CausticsTexturePath);
        serialized.FindProperty("causticsIntensity").floatValue = 0.6f;
        serialized.FindProperty("causticsScale").floatValue = 0.12f;
        serialized.FindProperty("causticsContrast").floatValue = 1.8f;
        serialized.FindProperty("causticsDepthFade").floatValue = 30f;
        serialized.FindProperty("causticsSpeed").floatValue = 0.18f;
        serialized.FindProperty("causticsRelativeMotion").floatValue = 0.025f;
        serialized.FindProperty("suspendedParticles").floatValue = 0f;
        serialized.FindProperty("lightShaftStrength").floatValue = 0.12f;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    public static bool TuneExistingProjectedCaustics()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            return false;

        FFTOceanUnderwaterController underwater = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            underwater = root.GetComponentInChildren<FFTOceanUnderwaterController>(true);
            if (underwater != null)
                break;
        }

        if (underwater == null)
            return false;

        SerializedObject serialized = new SerializedObject(underwater);
        serialized.FindProperty("causticsTint").colorValue = new Color(0.72f, 0.9f, 1f, 1f);
        serialized.FindProperty("causticsIntensity").floatValue = 0.6f;
        serialized.FindProperty("causticsContrast").floatValue = 1.8f;
        serialized.FindProperty("causticsDepthFade").floatValue = 30f;
        serialized.FindProperty("causticsSpeed").floatValue = 0.18f;
        serialized.FindProperty("causticsRelativeMotion").floatValue = 0.025f;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        return true;
    }

    private static void CreateSeabed(Material material)
    {
        GameObject seabed = GameObject.CreatePrimitive(PrimitiveType.Plane);
        seabed.name = "Underwater Showcase Seabed";
        seabed.transform.position = new Vector3(0f, -18f, 0f);
        seabed.transform.localScale = new Vector3(70f, 1f, 70f);
        Object.DestroyImmediate(seabed.GetComponent<Collider>());
        seabed.GetComponent<MeshRenderer>().sharedMaterial = material;
    }

    private static void EnsureUnderwaterRendererFeature(Shader shader)
    {
        UniversalRendererData rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(HighFidelityRendererPath);
        if (rendererData == null)
            throw new MissingReferenceException($"Missing active renderer data at {HighFidelityRendererPath}");

        FFTOceanUnderwaterFeature feature = null;
        foreach (ScriptableRendererFeature candidate in rendererData.rendererFeatures)
        {
            if (candidate is FFTOceanUnderwaterFeature underwaterFeature)
            {
                feature = underwaterFeature;
                break;
            }
        }

        if (feature == null)
        {
            feature = ScriptableObject.CreateInstance<FFTOceanUnderwaterFeature>();
            feature.name = "FFT Ocean Underwater";
            AssetDatabase.AddObjectToAsset(feature, rendererData);
            rendererData.rendererFeatures.Add(feature);
            var validateMethod = rendererData.GetType().GetMethod(
                "ValidateRendererFeatures",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            validateMethod?.Invoke(rendererData, null);
        }

        feature.ConfigureShader(shader);
        feature.SetActive(true);
        rendererData.SetDirty();
        EditorUtility.SetDirty(feature);
        EditorUtility.SetDirty(rendererData);
    }

    private static void SetCascade(SerializedProperty cascade, float domainSize, float scale, float damping, float minWavelength, float maxWavelength, float choppinessScale, int seedOffset)
    {
        cascade.FindPropertyRelative("domainSize").floatValue = domainSize;
        cascade.FindPropertyRelative("spectrumScale").floatValue = scale;
        cascade.FindPropertyRelative("shortWaveDamping").floatValue = damping;
        cascade.FindPropertyRelative("minWavelength").floatValue = minWavelength;
        cascade.FindPropertyRelative("maxWavelength").floatValue = maxWavelength;
        cascade.FindPropertyRelative("choppinessScale").floatValue = choppinessScale;
        cascade.FindPropertyRelative("seedOffset").intValue = seedOffset;
    }

    private static void ConfigureEnvironment(Material skyMaterial)
    {
        RenderSettings.skybox = skyMaterial;
        RenderSettings.ambientMode = AmbientMode.Skybox;
        RenderSettings.ambientSkyColor = new Color(0.212f, 0.227f, 0.259f, 1f);
        RenderSettings.ambientEquatorColor = new Color(0.114f, 0.125f, 0.133f, 1f);
        RenderSettings.ambientGroundColor = new Color(0.047f, 0.043f, 0.035f, 1f);
        RenderSettings.ambientIntensity = 1f;
        RenderSettings.fog = false;
        DynamicGI.UpdateEnvironment();
    }
}
#endif
