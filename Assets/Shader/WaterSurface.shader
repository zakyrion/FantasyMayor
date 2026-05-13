// URP animated water surface shader for FantasyMayor hex tiles.
//
// Features:
//   - Two-layer scrolling normal maps for organic wave motion
//   - Directional flow control (FlowDirection + FlowSpeed)
//   - Shore foam via depth-buffer fade (requires URP Depth Texture enabled in renderer asset)
//   - Depth-based shallow/deep color blend
//   - Fresnel specular rim
//   - Configurable transparency
//
// All properties can be overridden per-renderer via MaterialPropertyBlock (see WaterView.cs).
// Note: MaterialPropertyBlock usage disables SRP Batcher for this renderer, which is acceptable
// since the entire water surface is one mesh and one draw call.

Shader "FantasyMayor/WaterSurface"
{
    Properties
    {
        _ShallowColor  ("Shallow Color",    Color)        = (0.35, 0.72, 0.85, 0.65)
        _DeepColor     ("Deep Color",       Color)        = (0.10, 0.28, 0.58, 0.92)
        _WaveNormalMap ("Wave Normal Map",  2D)           = "bump" {}
        _WaveSpeed     ("Wave Speed",       Float)        = 0.4
        _WaveScale     ("Wave Scale",       Vector)       = (2, 2, 0, 0)
        _WaveScale2    ("Wave Scale 2",     Vector)       = (1.5, 1.5, 0, 0)
        _FlowDirection ("Flow Direction",   Vector)       = (1, 0, 0, 0)
        _FlowSpeed     ("Flow Speed",       Float)        = 0.2
        _FoamStrength  ("Foam Strength",    Range(0,1))   = 0.75
        _FoamWidth     ("Foam Width",       Float)        = 0.4
        _FresnelPower  ("Fresnel Power",    Range(1,12))  = 4.0
        _Transparency  ("Transparency",     Range(0,1))   = 0.82
        _WaveAmplitude ("Wave Amplitude",   Float)        = 0.04
        _WaveFrequency ("Wave Frequency",   Float)        = 1.5
    }

    SubShader
    {
        Tags
        {
            "RenderType"      = "Transparent"
            "Queue"           = "Transparent"
            "RenderPipeline"  = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            // -----------------------------------------------------------------------
            // Uniforms
            // -----------------------------------------------------------------------

            CBUFFER_START(UnityPerMaterial)
                float4 _ShallowColor;
                float4 _DeepColor;
                float4 _WaveScale;
                float4 _WaveScale2;
                float4 _FlowDirection;
                float  _WaveSpeed;
                float  _FlowSpeed;
                float  _FoamStrength;
                float  _FoamWidth;
                float  _FresnelPower;
                float  _Transparency;
                float  _WaveAmplitude;
                float  _WaveFrequency;
            CBUFFER_END

            TEXTURE2D(_WaveNormalMap);
            SAMPLER(sampler_WaveNormalMap);

            // -----------------------------------------------------------------------
            // Structs
            // -----------------------------------------------------------------------

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 worldPos   : TEXCOORD1;
                float3 worldNormal: TEXCOORD2;
                float4 screenPos  : TEXCOORD3;
                float  fogCoord   : TEXCOORD4;
                float  shoreAlpha : TEXCOORD5;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // -----------------------------------------------------------------------
            // Vertex shader
            // -----------------------------------------------------------------------

            // Returns vertical displacement for a world XZ position at a given time.
            // Two sine layers in perpendicular directions for organic motion.
            float WaveDisplacement(float2 worldXZ, float2 flowDir, float2 perpDir, float t)
            {
                float d1 = dot(worldXZ, flowDir) * _WaveFrequency + t * _WaveSpeed;
                float d2 = dot(worldXZ, perpDir) * _WaveFrequency * 0.7 + t * _WaveSpeed * 1.3 + 1.5708;
                return (sin(d1) + sin(d2) * 0.6) * _WaveAmplitude;
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float  shoreAlpha = input.color.a;
                float2 flowDir    = normalize(_FlowDirection.xy + float2(0.0001, 0.0));
                float2 perpDir    = float2(-flowDir.y, flowDir.x);
                float  t          = _Time.y;

                // World XZ needed for wave function — derive from object space position.
                // For a flat mesh this is a reliable approximation before the full transform.
                float3 worldXZ3   = TransformObjectToWorld(input.positionOS.xyz);
                float2 worldXZ    = worldXZ3.xz;

                // Vertex displacement: attenuate to zero at shore so waves don't break past the fade edge.
                float disp        = WaveDisplacement(worldXZ, flowDir, perpDir, t) * shoreAlpha;
                float4 displacedOS = input.positionOS + float4(0.0, disp, 0.0, 0.0);

                // Analytical normal from partial derivatives of the displacement function.
                // d(disp)/dx and d(disp)/dz give the slope in X and Z.
                float eps = 0.01;
                float dydx = (WaveDisplacement(worldXZ + float2(eps, 0), flowDir, perpDir, t)
                            - WaveDisplacement(worldXZ - float2(eps, 0), flowDir, perpDir, t))
                            / (2.0 * eps) * shoreAlpha;
                float dydz = (WaveDisplacement(worldXZ + float2(0, eps), flowDir, perpDir, t)
                            - WaveDisplacement(worldXZ - float2(0, eps), flowDir, perpDir, t))
                            / (2.0 * eps) * shoreAlpha;
                // Local-space slope normal, then transform to world.
                float3 displNormalOS = normalize(float3(-dydx, 1.0, -dydz));
                float3 displNormalWS = TransformObjectToWorldNormal(displNormalOS);

                VertexPositionInputs posInputs = GetVertexPositionInputs(displacedOS.xyz);

                output.positionCS  = posInputs.positionCS;
                output.worldPos    = posInputs.positionWS;
                output.worldNormal = displNormalWS;
                output.uv          = input.uv;
                output.screenPos   = ComputeScreenPos(posInputs.positionCS);
                output.fogCoord    = ComputeFogFactor(posInputs.positionCS.z);
                output.shoreAlpha  = shoreAlpha;
                return output;
            }

            // -----------------------------------------------------------------------
            // Fragment shader
            // -----------------------------------------------------------------------

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // --- Depth fade (shore foam + shallow/deep blend) ---
                float2 screenUV      = input.screenPos.xy / input.screenPos.w;
                float  rawDepth      = SampleSceneDepth(screenUV);
                float  sceneEyeDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                float3 viewPos       = TransformWorldToView(input.worldPos);
                float  surfEyeDepth  = -viewPos.z;
                float  depthDiff     = sceneEyeDepth - surfEyeDepth;
                // depthFade = 0 at shore, 1 in deep water
                float  depthFade     = saturate(depthDiff / max(_FoamWidth, 0.0001));

                // --- Two-layer normal map scroll ---
                float2 flowDir  = normalize(_FlowDirection.xy + float2(0.0001, 0.0));
                float2 perpDir  = float2(-flowDir.y, flowDir.x);
                float  shoreAlpha = input.shoreAlpha;

                float2 baseScroll = flowDir * _Time.y * _WaveSpeed;
                float2 flowScroll = flowDir * _Time.y * _FlowSpeed;

                float2 uv1 = input.uv * _WaveScale.xy  + baseScroll + flowScroll;
                float2 uv2 = input.uv * _WaveScale2.xy + perpDir * _Time.y * _WaveSpeed * 0.7 + flowScroll * 0.5;

                float3 n1 = UnpackNormal(SAMPLE_TEXTURE2D(_WaveNormalMap, sampler_WaveNormalMap, uv1));
                float3 n2 = UnpackNormal(SAMPLE_TEXTURE2D(_WaveNormalMap, sampler_WaveNormalMap, uv2));
                // Blend tangent-space normals, then perturb the vertex-displacement normal.
                // Water mesh is flat (world normal = up), so TBN is: T=(1,0,0), B=(0,0,1), N=(0,1,0).
                float3 blendedTN   = normalize(n1 + n2);
                float3 nmWorldPert = normalize(float3(blendedTN.x, blendedTN.z, blendedTN.y));
                // Displace normal carries geometric wave normals; normal map adds fine surface detail.
                float3 worldNormal = normalize(input.worldNormal + nmWorldPert * 0.4);

                // --- Color: depth-based blend + foam ---
                float3 waterColor = lerp(_ShallowColor.rgb, _DeepColor.rgb, depthFade);
                float  foamMask   = (1.0 - depthFade) * _FoamStrength;
                waterColor = lerp(waterColor, float3(1.0, 1.0, 1.0), foamMask);

                // --- Fresnel specular rim ---
                float3 viewDir  = GetWorldSpaceViewDir(input.worldPos);
                float  fresnel  = pow(1.0 - saturate(dot(viewDir, worldNormal)), max(_FresnelPower, 1.0));
                waterColor += fresnel * 0.25;

                // --- Main light half-lambert diffuse ---
                Light  mainLight  = GetMainLight();
                float  NdotL      = saturate(dot(worldNormal, mainLight.direction));
                float  diffuse    = NdotL * 0.5 + 0.5;
                waterColor *= mainLight.color.rgb * diffuse;

                // --- Alpha: transparent in deep water, opaque at foam edge, fades to 0 at shore edge ---
                float alpha = lerp(_Transparency, 1.0, foamMask) * shoreAlpha;

                // --- Fog ---
                float3 finalColor = MixFog(waterColor, input.fogCoord);

                return half4(finalColor, alpha);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
