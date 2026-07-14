Shader "TopDog/FieldAuraSphere"
{
    Properties
    {
        _MainTex ("Field Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (0.4, 0.75, 1, 0.28)
        _RimColor ("Rim", Color) = (0.7, 0.9, 1, 0.55)
        _RimPower ("Rim Power", Float) = 2.8
        _ScrollSpeed ("Scroll Speed", Float) = 0.08
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half4 _RimColor;
                half _RimPower;
                half _ScrollSpeed;
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
                float2 scrollUv = input.uv + float2(_Time.y * _ScrollSpeed, _Time.y * _ScrollSpeed * 0.35);
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, scrollUv);
                half ndv = saturate(dot(normalize(input.normalWS), normalize(input.viewDirWS)));
                half rim = pow(1.0h - ndv, _RimPower);
                half4 baseCol = _Color * tex;
                baseCol.rgb += _RimColor.rgb * rim * _RimColor.a;
                baseCol.a = saturate(baseCol.a + rim * 0.35h);
                return baseCol;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
