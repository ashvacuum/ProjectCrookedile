// Lights the half-cube city (city-generator.md §6): one sun set as globals by CitySun, and height-aware shadows that
// step toward the sun across the per-cell height texture CityMapView publishes. URP's 2D lights do not touch it.
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
        _AmbientStrength("Ambient Strength", Range(0, 2)) = 0.45
        _DiffuseStrength("Sun Strength", Range(0, 2)) = 0.8
        _SpecularStrength("Specular Strength", Range(0, 2)) = 0.35
        _SpecularPower("Specular Sharpness", Range(1, 128)) = 24
        _FaceSpecularScale("Face Specular Scale (Roof, Left, Right)", Vector) = (0.7, 0.08, 0.2, 0)
        _SpecularEdgeOnly("Specular Edge Only", Range(0, 1)) = 1
        _EdgeHighlightColor("Edge Highlight Color", Color) = (1, 0.82, 0.58, 1)
        _EdgeHighlightStrength("Edge Highlight Strength", Range(0, 2)) = 0.5
        _EdgeHighlightWidth("Edge Highlight Width", Range(0.005, 0.1)) = 0.025
        [PerRendererData] _RoofAtlasRect("Roof Atlas Rect", Vector) = (0, 0.5, 0.5, 0.5)
        [PerRendererData] _LeftAtlasRect("Left Atlas Rect", Vector) = (0.5, 0.5, 0.5, 0.5)
        [PerRendererData] _RightAtlasRect("Right Atlas Rect", Vector) = (0, 0, 0.5, 0.5)
        [PerRendererData] _AtlasQuarterTurns("Atlas Quarter Turns", Float) = 0
        [PerRendererData] _AtlasFlipX("Atlas Flip X", Float) = 0
        [PerRendererData] _AtlasFlipY("Atlas Flip Y", Float) = 0
        [PerRendererData] _NormalStrength("Normal Strength", Range(0, 2)) = 1
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #pragma vertex LitVertex
            #pragma fragment LitFragment
            #pragma multi_compile_instancing

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

            // Globals from CitySun and CityMapView; never material properties, so no material can override them.
            TEXTURE2D(_CityHeightMap);       // per cell: building wall height in screen units, texel (row, column)
            float4 _CitySunDirection;        // view space, toward the sun
            float4 _CitySunDirectionGrid;    // (row, column, up) in cell units, toward the sun
            float4 _CitySunColor;
            float4 _CityAmbientColor;
            float _CityEmissionScale;
            float4 _CityShadow;              // strength, step (cells), reach (cells), bias (cells)
            float4 _CityGrid;                // half width, half height, screen height per cell of 3D height
            float4 _CityMapOrigin;           // world position of cell (0, 0)
            float4 _CityHeightMapSize;       // rows, columns, tallest building (cells); rows 0 = no map, no shadows
            float4 _CityClouds;              // coverage (0 = none), softness, density, size (cells)
            float4 _CityCloudWind;           // xy = drift in cells per second along (row, column)
            TEXTURE2D(_CityGlowMap);         // window light over the grid, texel x = row; filtered
            SAMPLER(sampler_CityGlowMap);
            float4 _CityGlow;                // height fade (cells), enabled (0 = no glow map)

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                float2 faceData : TEXCOORD1; // face id (0 roof or ground, 1 left wall, 2 right wall), height
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                float faceId : TEXCOORD1;
                float3 gridPosition : TEXCOORD2; // row, column, height in cells
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
                half _AmbientStrength;
                half _DiffuseStrength;
                half _SpecularStrength;
                half _SpecularPower;
                half4 _FaceSpecularScale;
                half _SpecularEdgeOnly;
                half4 _EdgeHighlightColor;
                half _EdgeHighlightStrength;
                half _EdgeHighlightWidth;
                half _OcclusionStrength;
            CBUFFER_END

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

            // A point drawn `height` screen units above the ground at world xy stands on exactly one grid cell.
            float3 WorldToGrid(float3 positionWS, float height)
            {
                float2 local = positionWS.xy - _CityMapOrigin.xy - float2(0.0, height);
                float alongWidth = local.x / max(_CityGrid.x, 1e-5);
                float alongHeight = local.y / max(_CityGrid.y, 1e-5);
                return float3((alongWidth + alongHeight) * 0.5, (alongWidth - alongHeight) * 0.5, height / max(_CityGrid.z, 1e-5));
            }

            // The surface's outward normal in (row, column, up): roof and ground up, left wall -row, right wall +column.
            float3 GridNormal(half faceId)
            {
                return faceId < 0.5h ? float3(0, 0, 1) : faceId < 1.5h ? float3(-1, 0, 0) : float3(0, 1, 0);
            }

            // 1 in sunlight, 1 - strength in shadow. Steps from the surface toward the sun; any cell taller than the ray
            // at that point blocks it. Without a map (rows 0) nothing is shadowed.
            // ponytail: one sample per step gives cell-sharp edges; jitter a few samples if they read as stair-stepped.
            half SunVisibility(float3 gridPosition, half faceId)
            {
                float3 toSun = _CitySunDirectionGrid.xyz;
                if (toSun.z <= 0.0)
                {
                    return 0.0h;
                }

                if (_CityHeightMapSize.x < 1.0)
                {
                    return 1.0h;
                }

                float3 origin = gridPosition + GridNormal(faceId) * _CityShadow.w;
                float stepCells = max(_CityShadow.y, 0.02);
                int steps = (int)min(512.0, _CityShadow.z / stepCells);
                [loop]
                for (int index = 1; index <= steps; index++)
                {
                    float3 probe = origin + toSun * (index * stepCells);
                    if (probe.z > _CityHeightMapSize.z)
                    {
                        break;
                    }

                    int2 cell = int2(round(probe.xy));
                    if (cell.x < 0 || cell.y < 0 || cell.x >= (int)_CityHeightMapSize.x || cell.y >= (int)_CityHeightMapSize.y)
                    {
                        break;
                    }

                    float blocker = LOAD_TEXTURE2D(_CityHeightMap, cell).r / max(_CityGrid.z, 1e-5);
                    if (blocker > probe.z)
                    {
                        return 1.0h - (half)_CityShadow.x;
                    }
                }

                return 1.0h;
            }

            float2 CloudGradient(float2 lattice)
            {
                lattice = float2(dot(lattice, float2(127.1, 311.7)), dot(lattice, float2(269.5, 183.3)));
                return frac(sin(lattice) * 43758.5453) * 2.0 - 1.0;
            }

            // Perlin-style gradient noise, roughly -0.7 to 0.7.
            float CloudNoise(float2 position)
            {
                float2 lattice = floor(position);
                float2 local = frac(position);
                float2 blend = local * local * (3.0 - 2.0 * local);
                float bottom = lerp(dot(CloudGradient(lattice), local), dot(CloudGradient(lattice + float2(1, 0)), local - float2(1, 0)), blend.x);
                float top = lerp(dot(CloudGradient(lattice + float2(0, 1)), local - float2(0, 1)), dot(CloudGradient(lattice + 1.0), local - 1.0), blend.x);
                return lerp(bottom, top, blend.y);
            }

            // 1 in clear sky, down to 1 - density under thick cloud. Nothing cloud-shaped is drawn: two layers of drifting
            // noise over the grid dim direct sunlight with soft edges. Each point reads the clouds along its ray to the
            // sun, so a roof and the ground it shades agree and the pattern leans as the sun moves.
            // ponytail: the drift is wind x shader time, so changing the wind jumps the pattern; accumulate an offset on
            // the CPU if forecasts change wind mid-scene.
            half CloudShade(float3 gridPosition)
            {
                float3 toSun = _CitySunDirectionGrid.xyz;
                if (_CityClouds.x <= 0.0 || toSun.z <= 0.0)
                {
                    return 1.0h;
                }

                float2 position = gridPosition.xy - toSun.xy * (gridPosition.z / max(toSun.z, 0.1));
                float2 drift = _CityCloudWind.xy * _Time.y;
                float size = max(_CityClouds.w, 0.5);
                // The second layer is smaller and slower, so shapes change as they drift instead of sliding as one sheet.
                float noise = CloudNoise((position - drift) / size) * 0.65 + CloudNoise((position - drift * 0.6) / (size * 0.45) + 17.3) * 0.35;
                float density = saturate(noise * 0.75 + 0.5);
                float threshold = 1.0 - _CityClouds.x;
                float cloud = smoothstep(threshold - _CityClouds.y * 0.5, threshold + _CityClouds.y * 0.5 + 1e-4, density);
                return (half)(1.0 - cloud * _CityClouds.z);
            }

            // Baked window light at this point, fading out with height so it lights streets and low walls, not roofs.
            half3 WindowGlow(float3 gridPosition)
            {
                if (_CityGlow.y < 0.5 || _CityHeightMapSize.x < 1.0)
                {
                    return 0.0h;
                }

                float2 uv = (gridPosition.xy + 0.5) / _CityHeightMapSize.xy;
                half fade = (half)saturate(1.0 - gridPosition.z / max(_CityGlow.x, 0.01));
                return (half3)SAMPLE_TEXTURE2D(_CityGlowMap, sampler_CityGlowMap, uv).rgb * fade;
            }

            Varyings LitVertex(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);

                Varyings output = (Varyings)0;
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                float3 positionWS = TransformObjectToWorld(input.positionOS);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.uv = input.uv;
                output.color = input.color * _Color;
                output.faceId = input.faceData.x;
                // Heights are authored in object space; a scaled object scales them with its geometry.
                float heightScale = length(float3(UNITY_MATRIX_M._m01, UNITY_MATRIX_M._m11, UNITY_MATRIX_M._m21));
                output.gridPosition = WorldToGrid(positionWS, input.faceData.y * heightScale);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.tangentWS = TransformObjectToWorldDir(input.tangentOS.xyz);
                output.bitangentWS = normalize(cross(output.normalWS, output.tangentWS) * (input.tangentOS.w * GetOddNegativeScale()));
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
                half3 normal = normalize(input.tangentWS * normalTS.x + input.bitangentWS * normalTS.y + input.normalWS * normalTS.z);

                half3 toSun = (half3)_CitySunDirection.xyz;
                half3 sunColor = (half3)_CitySunColor.rgb;
                half visibility = SunVisibility(input.gridPosition, input.faceId) * CloudShade(input.gridPosition);
                half diffuse = saturate(dot(normal, toSun)) * visibility;
                half occlusion = lerp(1.0h, occlusionMask, _OcclusionStrength);
                half3 color = albedo * occlusion * ((half3)_CityAmbientColor.rgb * _AmbientStrength + sunColor * diffuse * _DiffuseStrength);

                half3 halfDirection = normalize(toSun + half3(0, 0, -1));
                half faceSpecularScale = input.faceId < 0.5h ? _FaceSpecularScale.x : input.faceId < 1.5h ? _FaceSpecularScale.y : _FaceSpecularScale.z;
                half roofFace = 1.0h - step(0.5h, input.faceId);
                half rightFace = step(1.5h, input.faceId);
                half roofLeftEdge = 1.0h - smoothstep(0.0h, _EdgeHighlightWidth, input.uv.y);
                half rightOuterEdge = 1.0h - smoothstep(0.0h, _EdgeHighlightWidth, 1.0h - input.uv.x);
                half edgeHighlight = max(roofLeftEdge * roofFace, rightOuterEdge * rightFace);
                half specularGate = lerp(1.0h, edgeHighlight, _SpecularEdgeOnly);
                half specular = pow(saturate(dot(normal, halfDirection)), _SpecularPower) * specularMask * _SpecularStrength * faceSpecularScale * specularGate;
                color += (specular + edgeHighlight * _EdgeHighlightStrength * _EdgeHighlightColor.rgb) * sunColor * visibility;
                color += emissionMask * _EmissionColor.rgb * _EmissionStrength * _CityEmissionScale;
                color += albedo * occlusion * WindowGlow(input.gridPosition) * _CityEmissionScale;
                return half4(color, input.color.a);
            }
            ENDHLSL
        }
    }
}
