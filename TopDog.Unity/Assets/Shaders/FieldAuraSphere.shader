Shader "TopDog/FieldAuraSphere"
{
    Properties
    {
        _MainTex ("Shell Tile", 2D) = "white" {}
        _NoiseTex ("Noise", 2D) = "gray" {}
        _Color ("Tint", Color) = (0.4, 0.75, 1, 0.22)
        _RimColor ("Rim", Color) = (0.7, 0.9, 1, 0.85)
        _RimPower ("Rim Power", Float) = 2.2
        _ShellFill ("Shell Fill", Float) = 0.28
        _NoiseStrength ("Noise Strength", Float) = 0.65
        _NoiseScroll ("Noise Scroll", Float) = 0.06
        _TileScroll ("Tile Scroll", Float) = 0.03
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest LEqual
        Cull Off

        // UniversalForward + SRPDefaultUnlit：手动 Camera.Render 在部分 URP 配置下只吃后者
        Pass
        {
            Name "FieldAuraShell"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _NoiseTex_ST;
                half4 _Color;
                half4 _RimColor;
                half _RimPower;
                half _ShellFill;
                half _NoiseStrength;
                half _NoiseScroll;
                half _TileScroll;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float3 positionWS : TEXCOORD3;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);
                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = normInputs.normalWS;
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(posInputs.positionWS);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Fresnel shell: strong limb, soft body fill (volume glass)
                float3 n = normalize(input.normalWS);
                float3 v = normalize(input.viewDirWS);
                half ndv = saturate(abs(dot(n, v)));
                half rim = pow(1.0h - ndv, _RimPower);
                half body = pow(1.0h - ndv, 1.15h);

                float t = _Time.y;
                float2 tileUv = input.uv + float2(t * _TileScroll, t * _TileScroll * 0.37);
                float2 noiseUv = input.uv * _NoiseTex_ST.xy + float2(t * _NoiseScroll, -t * _NoiseScroll * 0.55);

                half4 tile = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, tileUv);
                half4 noiseSamp = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUv);
                half noiseLum = saturate(dot(noiseSamp.rgb, half3(0.299h, 0.587h, 0.114h)));
                // Prefer alpha channel when present (SG masks often store noise there)
                half noise = lerp(noiseLum, noiseSamp.a, step(0.05h, noiseSamp.a));
                noise = lerp(1.0h, noise, _NoiseStrength);

                half4 tint = _Color;
                half3 rgb = tint.rgb * (0.45h + 0.55h * tile.rgb) * (0.55h + 0.45h * noise);
                rgb += _RimColor.rgb * rim * _RimColor.a;

                // Shell alpha: rim edge + translucent noise body (not a flat disc)
                half a = saturate(
                    rim * max(_RimColor.a, 0.55h)
                    + body * _ShellFill * noise * tint.a * 2.2h
                    + tile.a * body * 0.12h);
                a = saturate(a * (0.75h + 0.35h * noise));
                return half4(rgb, a);
            }
            ENDHLSL
        }

        // 部分 URP 手动 Camera.Render 只调度 SRPDefaultUnlit
        Pass
        {
            Name "FieldAuraShellUnlit"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _NoiseTex_ST;
                half4 _Color;
                half4 _RimColor;
                half _RimPower;
                half _ShellFill;
                half _NoiseStrength;
                half _NoiseScroll;
                half _TileScroll;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);
                output.positionCS = posInputs.positionCS;
                output.normalWS = normInputs.normalWS;
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(posInputs.positionWS);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 n = normalize(input.normalWS);
                float3 v = normalize(input.viewDirWS);
                half ndv = saturate(abs(dot(n, v)));
                half rim = pow(1.0h - ndv, _RimPower);
                half body = pow(1.0h - ndv, 1.15h);
                float t = _Time.y;
                float2 tileUv = input.uv + float2(t * _TileScroll, t * _TileScroll * 0.37);
                float2 noiseUv = input.uv * _NoiseTex_ST.xy + float2(t * _NoiseScroll, -t * _NoiseScroll * 0.55);
                half4 tile = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, tileUv);
                half4 noiseSamp = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUv);
                half noiseLum = saturate(dot(noiseSamp.rgb, half3(0.299h, 0.587h, 0.114h)));
                half noise = lerp(noiseLum, noiseSamp.a, step(0.05h, noiseSamp.a));
                noise = lerp(1.0h, noise, _NoiseStrength);
                half4 tint = _Color;
                half3 rgb = tint.rgb * (0.45h + 0.55h * tile.rgb) * (0.55h + 0.45h * noise);
                rgb += _RimColor.rgb * rim * _RimColor.a;
                half a = saturate(
                    rim * max(_RimColor.a, 0.55h)
                    + body * _ShellFill * noise * tint.a * 2.2h
                    + tile.a * body * 0.12h);
                a = saturate(a * (0.75h + 0.35h * noise));
                return half4(rgb, a);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
