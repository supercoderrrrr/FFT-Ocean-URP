Shader "FFT Ocean/URP/Underwater Fullscreen"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "FFT Ocean Underwater"

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Fragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            TEXTURE2D(_CausticsTexture);
            SAMPLER(sampler_CausticsTexture);

            float _WaterSurfaceHeight;
            float _CausticsProjectionHeight;
            float _CameraWaterDepth;
            float _WaterlineFeather;
            float3 _Absorption;
            float4 _ScatteringColor;
            float _ScatteringDensity;
            float _MaximumVisibility;
            float _DistortionStrength;
            float _DistortionScale;
            float _DistortionSpeed;
            float _CausticsIntensity;
            float _CausticsScale;
            float _CausticsContrast;
            float _CausticsDepthFade;
            float _CausticsSpeed;
            float _CausticsRelativeMotion;
            float _HasCausticsTexture;
            float4 _CausticsTint;
            float _SuspendedParticles;
            float _LightShaftStrength;
            float3 _SunRayDirection;
            float4 _UnderwaterSunColor;

            float CausticPattern(float3 worldPosition)
            {
                float receiverDepth = max(_CausticsProjectionHeight - worldPosition.y, 0.0);
                float2 nominalDirection = _SunRayDirection.xz / max(-_SunRayDirection.y, 0.08);
                float2 projectedXZ = worldPosition.xz - nominalDirection * receiverDepth;

                // Continuous symmetric counter-scrolling
                // Equal and opposite offsets preserve the world-space anchor
                float2 travelDirection = normalize(float2(0.83, 0.56));
                float2 relativeOffset = travelDirection * _Time.y
                    * _CausticsSpeed * _CausticsRelativeMotion;
                float2 baseUV = projectedXZ * _CausticsScale;
                float2 uvA = baseUV + relativeOffset;
                float sampleA = SAMPLE_TEXTURE2D_GRAD(
                    _CausticsTexture,
                    sampler_CausticsTexture,
                    uvA,
                    ddx(uvA),
                    ddy(uvA)).r;

                float2 uvB = baseUV - relativeOffset + float2(0.31, 0.67);
                float sampleB = SAMPLE_TEXTURE2D_GRAD(
                    _CausticsTexture,
                    sampler_CausticsTexture,
                    uvB,
                    ddx(uvB),
                    ddy(uvB)).r;

                // Relative layer motion keeps the projected centre stationary
                float focus = min(sampleA, sampleB);
                focus = pow(saturate(focus), max(_CausticsContrast, 0.1));
                float sunAngle = smoothstep(0.08, 0.35, -_SunRayDirection.y);
                return focus * sunAngle * _HasCausticsTexture;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float rawDepth = SampleSceneDepth(uv);
#if UNITY_REVERSED_Z
                bool isSky = rawDepth <= 0.00001;
#else
                bool isSky = rawDepth >= 0.99999;
#endif
                float3 worldPosition = ComputeWorldSpacePosition(uv, rawDepth, UNITY_MATRIX_I_VP);
                float3 rayDirection = normalize(worldPosition - _WorldSpaceCameraPos);
                float sceneDistance = min(distance(worldPosition, _WorldSpaceCameraPos), _MaximumVisibility);
                if (isSky)
                    sceneDistance = _MaximumVisibility;

                float waterPath = 0.0;
                if (_CameraWaterDepth >= 0.0)
                {
                    waterPath = sceneDistance;
                    if (rayDirection.y > 0.0001)
                    {
                        float exitDistance = _CameraWaterDepth / rayDirection.y;
                        waterPath = min(sceneDistance, max(exitDistance, 0.0));
                    }
                }
                else if (rayDirection.y < -0.0001)
                {
                    float entryDistance = (-_CameraWaterDepth) / -rayDirection.y;
                    waterPath = max(sceneDistance - entryDistance, 0.0);
                }

                float waterMask = smoothstep(0.0, max(_WaterlineFeather, 0.001), waterPath);
                float2 distortion = float2(
                    sin((uv.y + _Time.y * _DistortionSpeed) / max(_DistortionScale, 0.001)),
                    cos((uv.x - _Time.y * _DistortionSpeed * 0.83) / max(_DistortionScale, 0.001)));
                distortion *= _DistortionStrength * waterMask;
                half3 sourceColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + distortion).rgb;

                float3 transmission = exp(-_Absorption * waterPath);
                float scatterAmount = 1.0 - exp(-_ScatteringDensity * waterPath);
                half3 underwaterColor = sourceColor * transmission
                    + _ScatteringColor.rgb * scatterAmount;

                float depthBelowSurface = max(_WaterSurfaceHeight - worldPosition.y, 0.0);
                float causticReceiverDepth = max(_CausticsProjectionHeight - worldPosition.y, 0.0);
                float geometryMask = isSky ? 0.0 : step(worldPosition.y, _WaterSurfaceHeight);
                float causticFade = exp(-causticReceiverDepth / max(_CausticsDepthFade, 0.001));
                float caustics = 0.0;
                UNITY_BRANCH
                if (geometryMask > 0.5 && causticReceiverDepth > 0.05)
                {
                    caustics = CausticPattern(worldPosition) * causticFade
                        * _CausticsIntensity;
                }
                underwaterColor += caustics * _UnderwaterSunColor.rgb * _CausticsTint.rgb
                    * sqrt(saturate(transmission));

                float alignment = pow(saturate(dot(rayDirection, _SunRayDirection)), 18.0);
                underwaterColor += _UnderwaterSunColor.rgb * alignment
                    * scatterAmount * _LightShaftStrength;

                float waterline = 1.0 - saturate(abs(waterPath - _WaterlineFeather * 0.5)
                    / max(_WaterlineFeather * 0.5, 0.001));
                underwaterColor += waterline * 0.025 * _UnderwaterSunColor.rgb;
                return half4(lerp(sourceColor, underwaterColor, waterMask), 1.0);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
