Shader "Hidden/TopDog/FieldAuraManual"
{
    // 空心双面球壳：先背面后正面；半透斑驳，可透过正面见背面色块
    Properties
    {
        _MainTex ("Stripe", 2D) = "white" {}
        _NoiseTex ("Noise (unused)", 2D) = "gray" {}
        _Color ("Tint", Color) = (0.4, 0.75, 1, 0.42)
        _RimColor ("Rim", Color) = (0.75, 0.95, 1, 0.9)
        _RimPower ("Rim Power", Float) = 1.6
        _ShellFace ("Face Alpha", Float) = 0.55
        _StripeStrength ("Stripe Strength", Float) = 1.0
        _Contrast ("Stripe Contrast", Float) = 10
        _Scroll ("Scroll", Float) = 0.04
        _BackFaceBoost ("Back Face Boost", Float) = 1.25
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest LEqual
        Lighting Off
        Fog { Mode Off }

        CGINCLUDE
        #include "UnityCG.cginc"

        sampler2D _MainTex;
        float4 _MainTex_ST;
        fixed4 _Color;
        fixed4 _RimColor;
        float _RimPower;
        float _ShellFace;
        float _StripeStrength;
        float _Contrast;
        float _Scroll;
        float _BackFaceBoost;
        // CommandBuffer 手动画时由宿主写入（勿依赖未更新的 _WorldSpaceCameraPos）
        float3 _FxCamWorldPos;

        struct appdata
        {
            float4 vertex : POSITION;
            float3 normal : NORMAL;
            float2 uv : TEXCOORD0;
        };

        struct v2f
        {
            float4 pos : SV_POSITION;
            float2 uv : TEXCOORD0;
            float3 worldNormal : TEXCOORD1;
            float3 worldPos : TEXCOORD2;
        };

        v2f vert(appdata v)
        {
            v2f o;
            o.pos = UnityObjectToClipPos(v.vertex);
            o.uv = TRANSFORM_TEX(v.uv, _MainTex);
            o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
            o.worldNormal = UnityObjectToWorldNormal(v.normal);
            return o;
        }

        fixed4 frag_shell(v2f i, bool isBack)
        {
            // 背面：法线翻转向视点，缘光/斑驳随轨道正确转动
            float3 n = normalize(i.worldNormal);
            if (isBack)
            {
                n = -n;
            }

            float3 v = normalize(_FxCamWorldPos - i.worldPos);
            float ndv = saturate(dot(n, v));
            float rim = pow(1.0 - ndv, _RimPower);

            float t = _Time.y * _Scroll;
            float2 uv = i.uv + float2(t, t * 0.37);
            fixed4 stripe = tex2D(_MainTex, uv);
            stripe.rgb = saturate((stripe.rgb - 0.5) * _Contrast + 0.5);
            float stripeLum = dot(stripe.rgb, fixed3(0.299, 0.587, 0.114));
            float stripeA = max(stripe.a, stripeLum);

            fixed3 rgb = lerp(_Color.rgb, stripe.rgb, saturate(_StripeStrength));
            // 保留条纹本色，少乘一层 tint，避免斑驳发灰发淡
            rgb = lerp(rgb, stripe.rgb * _Color.rgb, 0.35);
            rgb += _RimColor.rgb * rim * _RimColor.a * 0.9;

            // 正面仍半透可见背面；缘/斑驳保持，中心（正对视点）透明度固定约 0.1
            float faceMul = isBack ? _BackFaceBoost : 0.92;
            float a = saturate(
                rim * 0.9
                + ndv * _ShellFace * (0.55 + 0.7 * stripeA)
                + stripeA * 0.35);
            a *= saturate(0.45 + 0.55 * stripeA);
            a = saturate(a * lerp(0.7, 1.15, _Color.a) * faceMul);
            float centerA = 0.1;
            a = lerp(a, centerA, pow(ndv, 3.0));
            return fixed4(rgb, a);
        }
        ENDCG

        Pass
        {
            Cull Front
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            fixed4 frag(v2f i) : SV_Target { return frag_shell(i, true); }
            ENDCG
        }

        Pass
        {
            Cull Back
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            fixed4 frag(v2f i) : SV_Target { return frag_shell(i, false); }
            ENDCG
        }
    }
    FallBack Off
}
