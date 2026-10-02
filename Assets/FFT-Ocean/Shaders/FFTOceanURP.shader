Shader "FFT Ocean/URP/Portfolio Ocean"
{
    Properties
    {
        [Header(Case Matched Water)]
        _DeepColor("Water Color", Color) = (0.0346, 0.1230, 0.1981, 1)
        _SubsurfaceColor("Subsurface Color", Color) = (0.1542, 0.8858, 0.9906, 1)
        _FoamColor("Foam Color", Color) = (1, 1, 1, 1)

        [Header(Cascade LOD)]
        _LodScale("LOD Scale", Range(1, 10)) = 7.13
        _NormalStrength("Normal Strength", Range(0, 2)) = 1

        [Header(Surface)]
        _Smoothness("Maximum Gloss", Range(0, 1)) = 0.91
        _DistantSmoothness("Distant Smoothness", Range(0, 1)) = 0.689
        _RoughnessScale("Roughness Distance Scale", Range(0, 0.02)) = 0.0044
        _FresnelPower("Fresnel Power", Range(1, 8)) = 5
        _ReflectionStrength("Reflection Strength", Range(0, 2)) = 1
        _SunGlitter("Sun Glitter", Range(0, 3)) = 1
        _SubsurfaceStrength("Subsurface Strength", Range(0, 1)) = 0.133
        _SubsurfaceScale("Subsurface Height Scale", Range(0.1, 20)) = 4.8
        _SubsurfaceBase("Subsurface Base", Range(-5, 1)) = -0.1

        [Header(Jacobian Foam)]
        _FoamBiasLOD0("Foam Bias LOD 0", Range(0, 7)) = 0.84
        _FoamBiasLOD1("Foam Bias LOD 1", Range(0, 7)) = 1.83
        _FoamBiasLOD2("Foam Bias LOD 2", Range(0, 7)) = 2.72
        _FoamScale("Foam Scale", Range(0, 20)) = 2.4
        _ContactFoam("Contact Foam", Range(0, 1)) = 1
        _ContactFoamDistance("Contact Foam Distance", Range(0.05, 5)) = 0.75

        [HideInInspector] _Displacement0("Displacement 0", 2D) = "black" {}
        [HideInInspector] _Displacement1("Displacement 1", 2D) = "black" {}
        [HideInInspector] _Displacement2("Displacement 2", 2D) = "black" {}
        [HideInInspector] _SurfaceData0("Derivatives 0", 2D) = "black" {}
        [HideInInspector] _SurfaceData1("Derivatives 1", 2D) = "black" {}
        [HideInInspector] _SurfaceData2("Derivatives 2", 2D) = "black" {}
        [HideInInspector] _DomainSizes("Domain Sizes", Vector) = (250, 17, 5, 0)
        [HideInInspector] _SpectrumResolution("Spectrum Resolution", Float) = 256
        [HideInInspector] _ClipmapSpacing("Clipmap Spacing", Float) = 0.125
        [HideInInspector] _ClipmapHalfExtent("Clipmap Half Extent", Float) = 15
        [HideInInspector] _DebugView("Debug View", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Opaque"
        }
        LOD 350
        Cull Off
        ZWrite On
        Blend One Zero

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            Texture2D<float4> _Displacement0;
            Texture2D<float4> _Displacement1;
            Texture2D<float4> _Displacement2;
            Texture2D<float4> _SurfaceData0;
            Texture2D<float4> _SurfaceData1;
            Texture2D<float4> _SurfaceData2;
            SamplerState sampler_Displacement0;
            SamplerState sampler_Displacement1;
            SamplerState sampler_Displacement2;
            SamplerState sampler_SurfaceData0;
            SamplerState sampler_SurfaceData1;
            SamplerState sampler_SurfaceData2;

            CBUFFER_START(UnityPerMaterial)
                half4 _DeepColor;
                half4 _SubsurfaceColor;
                half4 _FoamColor;
                half _LodScale;
                half _NormalStrength;
                half _Smoothness;
                half _DistantSmoothness;
                half _RoughnessScale;
                half _FresnelPower;
                half _ReflectionStrength;
                half _SunGlitter;
                half _SubsurfaceStrength;
                half _SubsurfaceScale;
                half _SubsurfaceBase;
                half _FoamBiasLOD0;
                half _FoamBiasLOD1;
                half _FoamBiasLOD2;
                half _FoamScale;
                half _ContactFoam;
                half _ContactFoamDistance;
                float4 _DomainSizes;
                float _SpectrumResolution;
                float _ClipmapSpacing;
                float _ClipmapHalfExtent;
                half _DebugView;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float2 basePositionWS : TEXCOORD1;
                half fogFactor : TEXCOORD2;
                float4 shadowCoord : TEXCOORD3;
                half3 cascadeWeights : TEXCOORD4;
            };

            float2 CascadeUV(float2 worldPosition, float domainSize)
            {
                return frac(worldPosition / max(domainSize, 0.001));
            }

            half CascadeLod(float viewDistance, float domainSize)
            {
                return saturate(_LodScale * domainSize / max(viewDistance, 0.001));
            }

            float2 MorphClipmapEdge(float2 localPosition)
            {
                float edgeDistance = max(abs(localPosition.x), abs(localPosition.y));
                float morph = smoothstep(
                    _ClipmapHalfExtent - _ClipmapSpacing * 2.0,
                    _ClipmapHalfExtent,
                    edgeDistance);
                float coarseSpacing = _ClipmapSpacing * 2.0;
                float2 coarsePosition = round(localPosition / coarseSpacing) * coarseSpacing;
                return lerp(localPosition, coarsePosition, morph);
            }

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                float3 localPosition = input.positionOS;
                localPosition.xz = MorphClipmapEdge(localPosition.xz);
                float3 basePositionWS = TransformObjectToWorld(localPosition);
                float2 baseXZ = basePositionWS.xz;
                float viewDistance = distance(basePositionWS, _WorldSpaceCameraPos);

                half lod0 = CascadeLod(viewDistance, _DomainSizes.x);
                half lod1 = CascadeLod(viewDistance, _DomainSizes.y);
                half lod2 = CascadeLod(viewDistance, _DomainSizes.z);
                float3 displacement0 = _Displacement0.SampleLevel(
                    sampler_Displacement0, CascadeUV(baseXZ, _DomainSizes.x), 0).xyz;
                float3 displacement1 = _Displacement1.SampleLevel(
                    sampler_Displacement1, CascadeUV(baseXZ, _DomainSizes.y), 0).xyz;
                float3 displacement2 = _Displacement2.SampleLevel(
                    sampler_Displacement2, CascadeUV(baseXZ, _DomainSizes.z), 0).xyz;
                float3 displacement = displacement0 * lod0 + displacement1 * lod1 + displacement2 * lod2;

                float3 positionWS = basePositionWS + displacement;
                output.positionWS = positionWS;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.basePositionWS = baseXZ;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                output.shadowCoord = TransformWorldToShadowCoord(positionWS);
                output.cascadeWeights = half3(lod0, lod1, lod2);
                return output;
            }

            float Hash21(float2 value)
            {
                value = frac(value * float2(123.34, 456.21));
                value += dot(value, value + 45.32);
                return frac(value.x * value.y);
            }

            float ValueNoise(float2 position)
            {
                float2 cell = floor(position);
                float2 local = frac(position);
                local = local * local * (3.0 - 2.0 * local);
                float bottom = lerp(Hash21(cell), Hash21(cell + float2(1, 0)), local.x);
                float top = lerp(Hash21(cell + float2(0, 1)), Hash21(cell + 1.0), local.x);
                return lerp(bottom, top, local.y);
            }

            half ContactFoam(Varyings input)
            {
                float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS);
                float sceneDepth = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                float surfaceDepth = -TransformWorldToView(input.positionWS).z;
                float depthDifference = max(0.0, sceneDepth - surfaceDepth - 0.1);
                half noise = ValueNoise(input.basePositionWS * 0.5 + _Time.y * 0.025);
                half contact = saturate(max(0.0h, noise - depthDifference) * 5.0h) * 0.9h;
                return contact * _ContactFoam;
            }

            half4 Fragment(Varyings input, FRONT_FACE_TYPE frontFace : FRONT_FACE_SEMANTIC) : SV_Target
            {
                float2 uv0 = CascadeUV(input.basePositionWS, _DomainSizes.x);
                float2 uv1 = CascadeUV(input.basePositionWS, _DomainSizes.y);
                float2 uv2 = CascadeUV(input.basePositionWS, _DomainSizes.z);
                float4 displacement0 = _Displacement0.Sample(sampler_Displacement0, uv0);
                float4 displacement1 = _Displacement1.Sample(sampler_Displacement1, uv1);
                float4 displacement2 = _Displacement2.Sample(sampler_Displacement2, uv2);
                float4 derivatives0 = _SurfaceData0.Sample(sampler_SurfaceData0, uv0);
                float4 derivatives1 = _SurfaceData1.Sample(sampler_SurfaceData1, uv1);
                float4 derivatives2 = _SurfaceData2.Sample(sampler_SurfaceData2, uv2);

                half lod0 = input.cascadeWeights.x;
                half lod1 = input.cascadeWeights.y;
                half lod2 = input.cascadeWeights.z;
                float4 derivatives = derivatives0 * lod0 + derivatives1 * lod1 + derivatives2 * lod2;
                float2 slope = float2(
                    derivatives.x / max(1.0 + derivatives.z, 0.05),
                    derivatives.y / max(1.0 + derivatives.w, 0.05));
                half3 normalWS = normalize(half3(
                    -slope.x * _NormalStrength,
                    1.0h,
                    -slope.y * _NormalStrength));
                half faceSign = IS_FRONT_VFACE(frontFace, 1.0h, -1.0h);
                normalWS *= faceSign;
                half underside = saturate(-faceSign);

                half midPresent = smoothstep(0.02h, 0.12h, lod1);
                half detailPresent = smoothstep(0.02h, 0.12h, lod2);
                half turbulence = displacement0.a
                    + displacement1.a * midPresent
                    + displacement2.a * detailPresent;
                half foamBias = _FoamBiasLOD0
                    + (_FoamBiasLOD1 - _FoamBiasLOD0) * midPresent
                    + (_FoamBiasLOD2 - _FoamBiasLOD1) * detailPresent;
                half foam = saturate((-turbulence + foamBias) * _FoamScale);
                foam = saturate(max(foam, ContactFoam(input)));

                if (_DebugView > 0.5h && _DebugView < 1.5h)
                {
                    float3 displacement = displacement0.xyz * lod0
                        + displacement1.xyz * lod1 + displacement2.xyz * lod2;
                    return half4(displacement * 0.1 + 0.5, 1);
                }
                if (_DebugView > 1.5h && _DebugView < 2.5h)
                    return half4(normalWS * 0.5h + 0.5h, 1);
                if (_DebugView > 2.5h && _DebugView < 3.5h)
                    return half4(foam.xxx, 1);
                if (_DebugView > 3.5h)
                    return half4(1.0h - midPresent, midPresent * (1.0h - detailPresent), detailPresent, 1);

                half3 viewDirection = SafeNormalize(GetWorldSpaceViewDir(input.positionWS));
                Light mainLight = GetMainLight(input.shadowCoord);
                half3 lightDirection = normalize(mainLight.direction);
                half nDotV = saturate(dot(normalWS, viewDirection));
                half fresnel = pow(saturate(1.0h - nDotV), _FresnelPower);
                half totalInternalReflection = 1.0h - smoothstep(0.55h, 0.68h, nDotV);
                fresnel = lerp(fresnel, max(fresnel, totalInternalReflection), underside);

                float viewDistance = distance(input.positionWS, _WorldSpaceCameraPos);
                half distanceFactor = rcp(1.0h + viewDistance * _RoughnessScale);
                half smoothness = lerp(_DistantSmoothness, _Smoothness, distanceFactor);
                smoothness = lerp(smoothness, 0.0h, foam);
                half perceptualRoughness = 1.0h - smoothness;

                half3 reflectionDirection = reflect(-viewDirection, normalWS);
                half3 environmentReflection = GlossyEnvironmentReflection(
                    reflectionDirection, perceptualRoughness, 1.0h);
                half3 skyFallback = lerp(half3(0.035h, 0.11h, 0.19h), half3(0.42h, 0.62h, 0.78h), fresnel);
                half3 reflection = lerp(skyFallback, environmentReflection, 0.86h)
                    * fresnel * _ReflectionStrength;

                half3 halfVector = SafeNormalize(lightDirection + viewDirection);
                half specularPower = lerp(20.0h, 512.0h, smoothness);
                half sunSpecular = pow(saturate(dot(normalWS, halfVector)), specularPower)
                    * _SunGlitter * mainLight.shadowAttenuation * mainLight.distanceAttenuation
                    * lerp(1.0h, 0.35h, underside);

                half combinedHeight = displacement0.y * lod0
                    + displacement1.y * lod1 + displacement2.y * lod2;
                half sssHeight = max(combinedHeight - displacement0.y * lod0 * 0.8h - _SubsurfaceBase, 0.0h)
                    / max(_SubsurfaceScale, 0.1h);
                half3 transmissionHalf = normalize(-normalWS + lightDirection);
                half viewDotTransmission = pow(saturate(dot(viewDirection, -transmissionHalf)), 5.0h)
                    * 30.0h * _SubsurfaceStrength;
                half3 waterColor = lerp(
                    _DeepColor.rgb,
                    saturate(_DeepColor.rgb + _SubsurfaceColor.rgb * viewDotTransmission * sssHeight),
                    lod2);

                half3 undersideTint = waterColor * half3(0.55h, 0.82h, 0.90h)
                    + _SubsurfaceColor.rgb * 0.035h;
                half3 waterBody = lerp(waterColor, undersideTint, underside) * (1.0h - fresnel);
                half3 color = waterBody + reflection + sunSpecular * mainLight.color;
                color = lerp(color, _FoamColor.rgb, foam);
                color = MixFog(color, input.fogFactor);
                return half4(color, 1);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
