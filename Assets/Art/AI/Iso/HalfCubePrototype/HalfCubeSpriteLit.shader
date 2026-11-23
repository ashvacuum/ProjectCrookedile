Shader "Crookedile/2D/Half Cube Lit"
{
    Properties
    {
        _MainTex("Face Atlas", 2D) = "white" {}
        _NormalMap("Normal Map", 2D) = "bump" {}
        _SpecularMap("Specular Mask", 2D) = "black" {}
        _OcclusionMap("Ambient Occlusion", 2D) = "white" {}
        _OcclusionStrength("Occlusion Strength", Range(0, 1)) = 1
        _EmissionMap("Emission Mask", 2D) = "black" {}
        _Color("Tint", Color) = (1, 1, 1, 1)
        _RoofBaseColor("Roof Fill", Color) = (0.34, 0.45, 0.43, 1)
        _WallBaseColor("Wall Fill", Color) = (0.76, 0.69, 0.56, 1)
        _EmissionColor("Emission Color", Color) = (1, 0.78, 0.38, 1)
        _EmissionStrength("Emission Strength", Range(0, 4)) = 1
        [PerRendererData] _RoofAtlasRect("Roof Atlas Rect", Vector) = (0, 0.5, 0.5, 0.5)
        [PerRendererData] _LeftAtlasRect("Left Atlas Rect", Vector) = (0.5, 0.5, 0.5, 0.5)
        [PerRendererData] _RightAtlasRect("Right Atlas Rect", Vector) = (0, 0, 0.5, 0.5)
        [PerRendererData] _AtlasQuarterTurns("Atlas Quarter Turns", Float) = 0
        [PerRendererData] _AtlasFlipX("Atlas Flip X", Float) = 0
        [PerRendererData] _AtlasFlipY("Atlas Flip Y", Float) = 0
        [PerRendererData] _KeyLightDirection("Key Light Direction", Vector) = (-0.4, 0.35, -0.85, 0)
        [PerRendererData] _KeyLightColor("Key Light Color", Color) = (1, 0.88, 0.72, 1)
        [PerRendererData] _AmbientStrength("Ambient Strength", Range(0, 1)) = 0.45
        [PerRendererData] _DiffuseStrength("Diffuse Strength", Range(0, 2)) = 0.8
        _SpecularStrength("Specular Strength", Range(0, 2)) = 0.35
        _SpecularPower("Specular Sharpness", Range(1, 128)) = 24
        _FaceSpecularScale("Face Specular Scale (Roof, Left, Right)", Vector) = (0.7, 0.08, 0.2, 0)
        _CustomDiffuse("Custom Key Diffuse (0 = URP 2D lights only)", Range(0, 1)) = 1
        [PerRendererData] _NormalStrength("Normal Strength", Range(0, 2)) = 1
        _SpecularEdgeOnly("Specular Edge Only", Range(0, 1)) = 1
        _EdgeHighlightColor("Edge Highlight Color", Color) = (1, 0.82, 0.58, 1)
        _EdgeHighlightStrength("Edge Highlight Strength", Range(0, 2)) = 0.5
        _EdgeHighlightWidth("Edge Highlight Width", Range(0.005, 0.1)) = 0.025
        [PerRendererData] _NormalFlipX("Normal Flip X", Float) = 1
        [PerRendererData] _NormalFlipY("Normal Flip Y", Float) = 1
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShapeLightShared.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/CombinedShapeLightShared.hlsl"

            #pragma vertex LitVertex
            #pragma fragment LitFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY
            #pragma multi_compile _ SKINNED_SPRITE

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);
            TEXTURE2D(_SpecularMap);
            SAMPLER(sampler_SpecularMap);
            TEXTURE2D(_OcclusionMap);
            SAMPLER(sampler_OcclusionMap);
            TEXTURE2D(_EmissionMap);
            SAMPLER(sampler_EmissionMap);

            struct Attributes
            {
                COMMON_2D_INPUTS
                float4 tangent : TANGENT;
                float2 faceId : TEXCOORD1;
                half4 color : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_LIT_OUTPUTS
                half4 color : COLOR;
                half faceId : TEXCOORD2;
                half3 normalWS : TEXCOORD3;
                half3 tangentWS : TEXCOORD4;
                half3 bitangentWS : TEXCOORD5;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _RoofBaseColor;
                half4 _WallBaseColor;
                half4 _EmissionColor;
                half _EmissionStrength;
                half4 _EdgeHighlightColor;
                half _EdgeHighlightStrength;
                half _EdgeHighlightWidth;
                half _SpecularEdgeOnly;
                half _OcclusionStrength;
                half4 _FaceSpecularScale;
                half _CustomDiffuse;
            CBUFFER_END

            UNITY_INSTANCING_BUFFER_START(PerInstance)
                UNITY_DEFINE_INSTANCED_PROP(float4, _RoofAtlasRect)
                UNITY_DEFINE_INSTANCED_PROP(float4, _LeftAtlasRect)
                UNITY_DEFINE_INSTANCED_PROP(float4, _RightAtlasRect)
                UNITY_DEFINE_INSTANCED_PROP(float, _AtlasQuarterTurns)
                UNITY_DEFINE_INSTANCED_PROP(float, _AtlasFlipX)
                UNITY_DEFINE_INSTANCED_PROP(float, _AtlasFlipY)
                UNITY_DEFINE_INSTANCED_PROP(float4, _KeyLightDirection)
                UNITY_DEFINE_INSTANCED_PROP(float4, _KeyLightColor)
                UNITY_DEFINE_INSTANCED_PROP(float, _AmbientStrength)
                UNITY_DEFINE_INSTANCED_PROP(float, _DiffuseStrength)
                UNITY_DEFINE_INSTANCED_PROP(float, _SpecularStrength)
                UNITY_DEFINE_INSTANCED_PROP(float, _SpecularPower)
                UNITY_DEFINE_INSTANCED_PROP(float, _NormalStrength)
                UNITY_DEFINE_INSTANCED_PROP(float, _NormalFlipX)
                UNITY_DEFINE_INSTANCED_PROP(float, _NormalFlipY)
            UNITY_INSTANCING_BUFFER_END(PerInstance)

            float2 TransformAtlasUV(float2 uv, float quarterTurns, float flipX, float flipY)
            {
                if (quarterTurns > 0.5 && quarterTurns < 1.5)
                {
                    uv = float2(uv.y, 1.0 - uv.x);
                }
                else if (quarterTurns > 1.5 && quarterTurns < 2.5)
                {
                    uv = 1.0 - uv;
                }
                else if (quarterTurns > 2.5)
                {
                    uv = float2(1.0 - uv.y, uv.x);
                }

                if (flipX > 0.5)
                {
                    uv.x = 1.0 - uv.x;
                }

                if (flipY > 0.5)
                {
                    uv.y = 1.0 - uv.y;
                }

                return uv;
            }

            half3 TransformAtlasNormal(half3 normalTS, float quarterTurns, float flipX, float flipY)
            {
                normalTS.x *= flipX > 0.5 ? -1.0h : 1.0h;
                normalTS.y *= flipY > 0.5 ? -1.0h : 1.0h;

                if (quarterTurns > 0.5 && quarterTurns < 1.5)
                {
                    normalTS.xy = half2(-normalTS.y, normalTS.x);
                }
                else if (quarterTurns > 1.5 && quarterTurns < 2.5)
                {
                    normalTS.xy = -normalTS.xy;
                }
                else if (quarterTurns > 2.5)
                {
                    normalTS.xy = half2(normalTS.y, -normalTS.x);
                }

                return normalTS;
            }

            // Mesh UVs stay local so each renderer can select atlas cells independently.
            float2 GetAtlasUV(float2 uv, half faceId)
            {
                float4 rect = UNITY_ACCESS_INSTANCED_PROP(PerInstance, _RoofAtlasRect);
                if (faceId > 0.5h && faceId < 1.5h)
                {
                    rect = UNITY_ACCESS_INSTANCED_PROP(PerInstance, _LeftAtlasRect);
                }
                else if (faceId > 1.5h)
                {
                    rect = UNITY_ACCESS_INSTANCED_PROP(PerInstance, _RightAtlasRect);
                }

                float quarterTurns = UNITY_ACCESS_INSTANCED_PROP(PerInstance, _AtlasQuarterTurns);
                float flipX = UNITY_ACCESS_INSTANCED_PROP(PerInstance, _AtlasFlipX);
                float flipY = UNITY_ACCESS_INSTANCED_PROP(PerInstance, _AtlasFlipY);
                return rect.xy + TransformAtlasUV(uv, quarterTurns, flipX, flipY) * rect.zw;
            }

            Varyings LitVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                UNITY_SETUP_INSTANCE_ID(input);

                Varyings output = (Varyings)0;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv;
                output.lightingUV = half2(ComputeScreenPos(output.positionCS / output.positionCS.w).xy);
                output.color = input.color * _Color;
                output.faceId = input.faceId.x;
                output.normalWS = TransformObjectToWorldNormal(input.normal);
                output.tangentWS = TransformObjectToWorldDir(input.tangent.xyz);
                output.bitangentWS = normalize(cross(output.normalWS, output.tangentWS) * (input.tangent.w * GetOddNegativeScale()));
                return output;
            }

            half4 LitFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float2 atlasUV = GetAtlasUV(input.uv, input.faceId);
                half4 source = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, atlasUV);
                half4 normalSource = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, atlasUV);
                half specularMask = SAMPLE_TEXTURE2D(_SpecularMap, sampler_SpecularMap, atlasUV).r;
                half occlusionMask = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, atlasUV).r;
                half emissionMask = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, atlasUV).r;
                half3 fillColor = input.faceId < 0.5h ? _RoofBaseColor.rgb : _WallBaseColor.rgb;
                half3 albedo = lerp(fillColor, source.rgb, source.a) * input.color.rgb;

                half3 normalTS = UnpackNormal(normalSource);
                half normalStrength = UNITY_ACCESS_INSTANCED_PROP(PerInstance, _NormalStrength);
                normalTS = normalize(lerp(half3(0, 0, 1), normalTS, normalSource.a * normalStrength));
                normalTS = TransformAtlasNormal(
                    normalTS,
                    UNITY_ACCESS_INSTANCED_PROP(PerInstance, _AtlasQuarterTurns),
                    UNITY_ACCESS_INSTANCED_PROP(PerInstance, _AtlasFlipX),
                    UNITY_ACCESS_INSTANCED_PROP(PerInstance, _AtlasFlipY)
                );
                normalTS.x *= UNITY_ACCESS_INSTANCED_PROP(PerInstance, _NormalFlipX);
                normalTS.y *= UNITY_ACCESS_INSTANCED_PROP(PerInstance, _NormalFlipY);
                half3 normalWS = normalize(
                    input.tangentWS * normalTS.x
                    + input.bitangentWS * normalTS.y
                    + input.normalWS * normalTS.z
                );

                float3 keyDirection = normalize(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _KeyLightDirection).xyz);
                half3 keyColor = UNITY_ACCESS_INSTANCED_PROP(PerInstance, _KeyLightColor).rgb;
                half ambient = UNITY_ACCESS_INSTANCED_PROP(PerInstance, _AmbientStrength);
                half diffuseStrength = UNITY_ACCESS_INSTANCED_PROP(PerInstance, _DiffuseStrength);
                half specularStrength = UNITY_ACCESS_INSTANCED_PROP(PerInstance, _SpecularStrength);
                half specularPower = UNITY_ACCESS_INSTANCED_PROP(PerInstance, _SpecularPower);
                half diffuse = saturate(dot(normalWS, keyDirection));
                // The key Light2D also reaches CombinedShapeLightShared below; _CustomDiffuse A/Bs this extra N.L term.
                half3 shadedAlbedo = lerp(albedo, albedo * diffuse * diffuseStrength * keyColor, _CustomDiffuse);

                SurfaceData2D surfaceData;
                InputData2D inputData;
                half4 mask = half4(1, 1, 0, 0);
                InitializeSurfaceData(shadedAlbedo, input.color.a, mask, normalTS, surfaceData);
                InitializeInputData(input.uv, input.lightingUV, inputData);

                half3 viewDirection = half3(0, 0, -1);
                half3 halfDirection = normalize(keyDirection + viewDirection);
                half faceSpecularScale = input.faceId < 0.5h ? _FaceSpecularScale.x : input.faceId < 1.5h ? _FaceSpecularScale.y : _FaceSpecularScale.z;
                half roofFace = 1.0h - step(0.5h, input.faceId);
                half rightFace = step(1.5h, input.faceId);
                half roofLeftEdge = 1.0h - smoothstep(0.0h, _EdgeHighlightWidth, input.uv.y);
                half rightOuterEdge = 1.0h - smoothstep(0.0h, _EdgeHighlightWidth, 1.0h - input.uv.x);
                half edgeHighlight = max(roofLeftEdge * roofFace, rightOuterEdge * rightFace);
                half specularGate = lerp(1.0h, edgeHighlight, _SpecularEdgeOnly);
                half specular = pow(saturate(dot(normalWS, halfDirection)), specularPower) * specularMask * specularStrength * faceSpecularScale * specularGate;
                half occlusion = lerp(1.0h, occlusionMask, _OcclusionStrength);
                half4 litColor = CombinedShapeLightShared(surfaceData, inputData);
                litColor.rgb *= occlusion;
                litColor.rgb += albedo * ambient * occlusion;
                litColor.rgb += specular * keyColor;
                litColor.rgb += edgeHighlight * _EdgeHighlightColor.rgb * _EdgeHighlightStrength;
                litColor.rgb += emissionMask * _EmissionColor.rgb * _EmissionStrength;
                return litColor;
            }
            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode" = "NormalsRendering" }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/NormalsRenderingShared.hlsl"

            #pragma vertex NormalsVertex
            #pragma fragment NormalsFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ SKINNED_SPRITE

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);

            struct Attributes
            {
                COMMON_2D_NORMALS_INPUTS
                float2 faceId : TEXCOORD1;
                half4 color : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_NORMALS_OUTPUTS
                half4 color : COLOR;
                half faceId : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            UNITY_INSTANCING_BUFFER_START(PerInstance)
                UNITY_DEFINE_INSTANCED_PROP(float4, _RoofAtlasRect)
                UNITY_DEFINE_INSTANCED_PROP(float4, _LeftAtlasRect)
                UNITY_DEFINE_INSTANCED_PROP(float4, _RightAtlasRect)
                UNITY_DEFINE_INSTANCED_PROP(float, _AtlasQuarterTurns)
                UNITY_DEFINE_INSTANCED_PROP(float, _AtlasFlipX)
                UNITY_DEFINE_INSTANCED_PROP(float, _AtlasFlipY)
                UNITY_DEFINE_INSTANCED_PROP(float, _NormalStrength)
                UNITY_DEFINE_INSTANCED_PROP(float, _NormalFlipX)
                UNITY_DEFINE_INSTANCED_PROP(float, _NormalFlipY)
            UNITY_INSTANCING_BUFFER_END(PerInstance)

            float2 TransformAtlasUV(float2 uv, float quarterTurns, float flipX, float flipY)
            {
                if (quarterTurns > 0.5 && quarterTurns < 1.5)
                {
                    uv = float2(uv.y, 1.0 - uv.x);
                }
                else if (quarterTurns > 1.5 && quarterTurns < 2.5)
                {
                    uv = 1.0 - uv;
                }
                else if (quarterTurns > 2.5)
                {
                    uv = float2(1.0 - uv.y, uv.x);
                }

                if (flipX > 0.5)
                {
                    uv.x = 1.0 - uv.x;
                }

                if (flipY > 0.5)
                {
                    uv.y = 1.0 - uv.y;
                }

                return uv;
            }

            half3 TransformAtlasNormal(half3 normalTS, float quarterTurns, float flipX, float flipY)
            {
                normalTS.x *= flipX > 0.5 ? -1.0h : 1.0h;
                normalTS.y *= flipY > 0.5 ? -1.0h : 1.0h;

                if (quarterTurns > 0.5 && quarterTurns < 1.5)
                {
                    normalTS.xy = half2(-normalTS.y, normalTS.x);
                }
                else if (quarterTurns > 1.5 && quarterTurns < 2.5)
                {
                    normalTS.xy = -normalTS.xy;
                }
                else if (quarterTurns > 2.5)
                {
                    normalTS.xy = half2(normalTS.y, -normalTS.x);
                }

                return normalTS;
            }

            float2 GetAtlasUV(float2 uv, half faceId)
            {
                float4 rect = UNITY_ACCESS_INSTANCED_PROP(PerInstance, _RoofAtlasRect);
                if (faceId > 0.5h && faceId < 1.5h)
                {
                    rect = UNITY_ACCESS_INSTANCED_PROP(PerInstance, _LeftAtlasRect);
                }
                else if (faceId > 1.5h)
                {
                    rect = UNITY_ACCESS_INSTANCED_PROP(PerInstance, _RightAtlasRect);
                }

                float quarterTurns = UNITY_ACCESS_INSTANCED_PROP(PerInstance, _AtlasQuarterTurns);
                float flipX = UNITY_ACCESS_INSTANCED_PROP(PerInstance, _AtlasFlipX);
                float flipY = UNITY_ACCESS_INSTANCED_PROP(PerInstance, _AtlasFlipY);
                return rect.xy + TransformAtlasUV(uv, quarterTurns, flipX, flipY) * rect.zw;
            }

            Varyings NormalsVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                UNITY_SETUP_INSTANCE_ID(input);

                Varyings output = (Varyings)0;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv;
                output.normalWS = TransformObjectToWorldNormal(input.normal);
                output.tangentWS = TransformObjectToWorldDir(input.tangent.xyz);
                output.bitangentWS = normalize(cross(output.normalWS, output.tangentWS) * (input.tangent.w * GetOddNegativeScale()));
                output.color = input.color;
                output.faceId = input.faceId.x;
                return output;
            }

            half4 NormalsFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float2 atlasUV = GetAtlasUV(input.uv, input.faceId);
                half4 normalSource = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, atlasUV);
                half3 normalTS = UnpackNormal(normalSource);
                half normalStrength = UNITY_ACCESS_INSTANCED_PROP(PerInstance, _NormalStrength);
                normalTS = normalize(lerp(half3(0, 0, 1), normalTS, normalSource.a * normalStrength));
                normalTS = TransformAtlasNormal(
                    normalTS,
                    UNITY_ACCESS_INSTANCED_PROP(PerInstance, _AtlasQuarterTurns),
                    UNITY_ACCESS_INSTANCED_PROP(PerInstance, _AtlasFlipX),
                    UNITY_ACCESS_INSTANCED_PROP(PerInstance, _AtlasFlipY)
                );
                normalTS.x *= UNITY_ACCESS_INSTANCED_PROP(PerInstance, _NormalFlipX);
                normalTS.y *= UNITY_ACCESS_INSTANCED_PROP(PerInstance, _NormalFlipY);
                half4 color = half4(1, 1, 1, input.color.a);
                return NormalsRenderingShared(color, normalTS, input.tangentWS, input.bitangentWS, input.normalWS);
            }
            ENDHLSL
        }
    }
}
