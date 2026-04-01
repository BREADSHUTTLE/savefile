Shader "UI/ImageBlur"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _BlurSize ("Blur Size", Range(0, 300)) = 30
        
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            float _BlurSize;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 uv = IN.texcoord;
                float2 texelSize = _MainTex_TexelSize.xy;
                
                // 원본 픽셀의 알파값 저장
                half originalAlpha = tex2D(_MainTex, uv).a;
                
                // Kawase Blur 스타일 - 여러 거리에서 대각선 샘플링
                // 계단현상 없이 부드러운 블러 효과
                float blurScale = _BlurSize * 0.01;
                
                half3 color = tex2D(_MainTex, uv).rgb * 0.12;
                
                // 레이어 1 - 가장 가까운 거리 (세밀한 블러)
                float2 o1 = texelSize * blurScale * 1.0;
                color += tex2D(_MainTex, saturate(uv + float2(o1.x, o1.y))).rgb * 0.09;
                color += tex2D(_MainTex, saturate(uv + float2(-o1.x, o1.y))).rgb * 0.09;
                color += tex2D(_MainTex, saturate(uv + float2(o1.x, -o1.y))).rgb * 0.09;
                color += tex2D(_MainTex, saturate(uv + float2(-o1.x, -o1.y))).rgb * 0.09;
                
                // 레이어 2 - 중간 거리
                float2 o2 = texelSize * blurScale * 2.5;
                color += tex2D(_MainTex, saturate(uv + float2(o2.x, o2.y))).rgb * 0.07;
                color += tex2D(_MainTex, saturate(uv + float2(-o2.x, o2.y))).rgb * 0.07;
                color += tex2D(_MainTex, saturate(uv + float2(o2.x, -o2.y))).rgb * 0.07;
                color += tex2D(_MainTex, saturate(uv + float2(-o2.x, -o2.y))).rgb * 0.07;
                
                // 레이어 3 - 먼 거리
                float2 o3 = texelSize * blurScale * 5.0;
                color += tex2D(_MainTex, saturate(uv + float2(o3.x, o3.y))).rgb * 0.05;
                color += tex2D(_MainTex, saturate(uv + float2(-o3.x, o3.y))).rgb * 0.05;
                color += tex2D(_MainTex, saturate(uv + float2(o3.x, -o3.y))).rgb * 0.05;
                color += tex2D(_MainTex, saturate(uv + float2(-o3.x, -o3.y))).rgb * 0.05;
                
                // 레이어 4 - 더 먼 거리 (넓은 블러)
                float2 o4 = texelSize * blurScale * 9.0;
                color += tex2D(_MainTex, saturate(uv + float2(o4.x, o4.y))).rgb * 0.03;
                color += tex2D(_MainTex, saturate(uv + float2(-o4.x, o4.y))).rgb * 0.03;
                color += tex2D(_MainTex, saturate(uv + float2(o4.x, -o4.y))).rgb * 0.03;
                color += tex2D(_MainTex, saturate(uv + float2(-o4.x, -o4.y))).rgb * 0.03;
                
                // 축 방향 샘플 추가 (부드러움 증가)
                float2 o5 = texelSize * blurScale * 4.0;
                color += tex2D(_MainTex, saturate(uv + float2(o5.x, 0))).rgb * 0.04;
                color += tex2D(_MainTex, saturate(uv + float2(-o5.x, 0))).rgb * 0.04;
                color += tex2D(_MainTex, saturate(uv + float2(0, o5.y))).rgb * 0.04;
                color += tex2D(_MainTex, saturate(uv + float2(0, -o5.y))).rgb * 0.04;
                
                // 총 가중치: 0.12 + 0.36 + 0.28 + 0.20 + 0.12 + 0.16 = 1.24
                color /= 1.24;
                
                half4 finalColor;
                finalColor.rgb = (color + _TextureSampleAdd.rgb) * IN.color.rgb;
                finalColor.a = originalAlpha * IN.color.a;

                #ifdef UNITY_UI_CLIP_RECT
                finalColor.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (finalColor.a - 0.001);
                #endif

                return finalColor;
            }
            ENDCG
        }
    }
}
