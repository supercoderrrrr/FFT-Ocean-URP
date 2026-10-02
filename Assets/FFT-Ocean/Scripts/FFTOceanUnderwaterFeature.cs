using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public sealed class FFTOceanUnderwaterFeature : ScriptableRendererFeature
{
    [SerializeField] private Shader underwaterShader;

    private Material underwaterMaterial;
    private UnderwaterPass underwaterPass;
    private FFTOceanUnderwaterController activeController;

    public override void Create()
    {
        CoreUtils.Destroy(underwaterMaterial);
        underwaterMaterial = underwaterShader != null
            ? CoreUtils.CreateEngineMaterial(underwaterShader)
            : null;
        underwaterPass = new UnderwaterPass(underwaterMaterial)
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing
        };
    }

    public void ConfigureShader(Shader shader)
    {
        underwaterShader = shader;
        Create();
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        activeController = null;
        if (underwaterPass == null || underwaterMaterial == null
            || renderingData.cameraData.cameraType != CameraType.Game)
            return;

        Camera camera = renderingData.cameraData.camera;
        if (!camera.TryGetComponent(out FFTOceanUnderwaterController controller)
            || !controller.IsEffectActive)
            return;

        activeController = controller;
        underwaterPass.SetController(controller);
        renderer.EnqueuePass(underwaterPass);
    }

    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        if (activeController == null || underwaterPass == null
            || renderingData.cameraData.camera != activeController.TargetCamera)
            return;

        underwaterPass.SetSource(renderer.cameraColorTargetHandle);
    }

    protected override void Dispose(bool disposing)
    {
        underwaterPass?.Dispose();
        underwaterPass = null;
        CoreUtils.Destroy(underwaterMaterial);
        underwaterMaterial = null;
    }

    private sealed class UnderwaterPass : ScriptableRenderPass
    {
        private readonly ProfilingSampler underwaterProfilingSampler = new ProfilingSampler("FFT Ocean Underwater");
        private readonly Material material;
        private RTHandle source;
        private RTHandle temporaryColor;
        private FFTOceanUnderwaterController controller;

        public UnderwaterPass(Material material)
        {
            this.material = material;
            ConfigureInput(ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Depth);
        }

        public void SetController(FFTOceanUnderwaterController value)
        {
            controller = value;
        }

        public void SetSource(RTHandle value)
        {
            source = value;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;
            descriptor.msaaSamples = 1;
            RenderingUtils.ReAllocateIfNeeded(
                ref temporaryColor,
                descriptor,
                FilterMode.Bilinear,
                TextureWrapMode.Clamp,
                name: "_FFTOceanUnderwaterColor");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (material == null || controller == null || source == null || temporaryColor == null)
                return;

            controller.ConfigureMaterial(material);
            CommandBuffer command = CommandBufferPool.Get();
            using (new ProfilingScope(command, underwaterProfilingSampler))
            {
                Blitter.BlitCameraTexture(command, source, temporaryColor, material, 0);
                Blitter.BlitCameraTexture(command, temporaryColor, source);
            }
            context.ExecuteCommandBuffer(command);
            CommandBufferPool.Release(command);
        }

        public void Dispose()
        {
            temporaryColor?.Release();
            temporaryColor = null;
            source = null;
            controller = null;
        }
    }
}
