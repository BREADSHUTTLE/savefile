Shader "UI/Outline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        [Header(Outline)]
        _OutlineColor ("Outline Color", Color) = (1, 1, 1, 1)
        _OutlineWidth ("Outline Width", Range(1, 10)) = 2
        [Toggle] _UseDirectionalColors ("Use Directional Colors", Float) = 0
        _TopColor ("Top Color", Color) = (1, 1, 1, 1)
        _BottomColor ("Bottom Color", Color) = (1, 1, 1, 1)
        _LeftColor ("Left Color", Color) = (1, 1, 1, 1)
        _RightColor ("Right Color", Color) = (1, 1, 1, 1)
        [Toggle] _OutlineTop ("Top", Float) = 1
        [Toggle] _OutlineBottom ("Bottom", Float) = 1
        [Toggle] _OutlineLeft ("Left", Float) = 1
        [Toggle] _OutlineRight ("Right", Float) = 1
        
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
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
            #pragma target 2.0
            
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            fixed4 _OutlineColor;
            float _OutlineWidth;
            float _UseDirectionalColors;
            fixed4 _TopColor;
            fixed4 _BottomColor;
            fixed4 _LeftColor;
            fixed4 _RightColor;
            float _OutlineTop;
            float _OutlineBottom;
            float _OutlineLeft;
            float _OutlineRight;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 texColor = tex2D(_MainTex, i.texcoord);
                fixed4 col = texColor * i.color;
                float originalAlpha = col.a;
                float vertexAlpha = i.color.a;  // UIGradient에서 수정된 알파
                
                if (originalAlpha < 0.01)
                    return col;
                
                // 아웃라인
                float2 texelSize = _MainTex_TexelSize.xy * _OutlineWidth;
                
                float minAlpha = 1.0;
                float aT = 1.0, aB = 1.0, aL = 1.0, aR = 1.0;
                
                if (_OutlineTop > 0.5)
                {
                    aT = tex2D(_MainTex, i.texcoord + float2(0, texelSize.y)).a;
                    minAlpha = min(minAlpha, aT);
                }

                if (_OutlineBottom > 0.5)
                {
                    aB = tex2D(_MainTex, i.texcoord + float2(0, -texelSize.y)).a;
                    minAlpha = min(minAlpha, aB);
                }

                if (_OutlineLeft > 0.5)
                {
                    aL = tex2D(_MainTex, i.texcoord + float2(-texelSize.x, 0)).a;
                    minAlpha = min(minAlpha, aL);
                }
                
                if (_OutlineRight > 0.5)
                {
                    aR = tex2D(_MainTex, i.texcoord + float2(texelSize.x, 0)).a;
                    minAlpha = min(minAlpha, aR);
                }
                
                // 대각선 (인접한 두 방향이 활성화된 경우만)
                float diagScale = 0.707;
                float2 diagOffset = texelSize * diagScale;
                float aTL = 1.0, aTR = 1.0, aBL = 1.0, aBR = 1.0;
                
                if (_OutlineTop > 0.5 && _OutlineLeft > 0.5)
                {
                    aTL = tex2D(_MainTex, i.texcoord + float2(-diagOffset.x, diagOffset.y)).a;
                    minAlpha = min(minAlpha, aTL);
                }
                if (_OutlineTop > 0.5 && _OutlineRight > 0.5)
                {
                    aTR = tex2D(_MainTex, i.texcoord + float2(diagOffset.x, diagOffset.y)).a;
                    minAlpha = min(minAlpha, aTR);
                }
                if (_OutlineBottom > 0.5 && _OutlineLeft > 0.5)
                {
                    aBL = tex2D(_MainTex, i.texcoord + float2(-diagOffset.x, -diagOffset.y)).a;
                    minAlpha = min(minAlpha, aBL);
                }
                if (_OutlineBottom > 0.5 && _OutlineRight > 0.5)
                {
                    aBR = tex2D(_MainTex, i.texcoord + float2(diagOffset.x, -diagOffset.y)).a;
                    minAlpha = min(minAlpha, aBR);
                }
                
                float edge = smoothstep(0.5, 0.0, minAlpha);
                
                // 아웃라인 색상 계산
                fixed3 outlineColor;
                float outlineAlpha;
                
                if (_UseDirectionalColors > 0.5)
                {
                    // UV 기반 방향 계산 (0.5, 0.5 중심)
                    float2 dir = i.texcoord - float2(0.5, 0.5);
                    dir = normalize(dir + float2(0.0001, 0.0001));
                    
                    // 각 방향 가중치 (부드러운 전환)
                    float wTop = saturate(dir.y) * smoothstep(0.9, 0.1, aT);
                    float wBottom = saturate(-dir.y) * smoothstep(0.9, 0.1, aB);
                    float wRight = saturate(dir.x) * smoothstep(0.9, 0.1, aR);
                    float wLeft = saturate(-dir.x) * smoothstep(0.9, 0.1, aL);
                    
                    // 부드러운 블렌딩
                    float totalWeight = wTop + wBottom + wLeft + wRight + 0.0001;
                    float inv = 1.0 / totalWeight;
                    
                    outlineColor = _TopColor.rgb * wTop * inv
                                 + _BottomColor.rgb * wBottom * inv
                                 + _LeftColor.rgb * wLeft * inv
                                 + _RightColor.rgb * wRight * inv;
                    outlineAlpha = _TopColor.a * wTop * inv
                                 + _BottomColor.a * wBottom * inv
                                 + _LeftColor.a * wLeft * inv
                                 + _RightColor.a * wRight * inv;
                }
                else
                {
                    outlineColor = _OutlineColor.rgb;
                    outlineAlpha = _OutlineColor.a;
                }
                
                // 아웃라인 색상 블렌딩
                col.rgb = lerp(col.rgb, outlineColor, edge * outlineAlpha);
                
                // 아웃라인 영역은 vertex alpha(UIGradient) 영향 없이 outlineAlpha 사용
                // 내부는 기존대로 vertex alpha 적용
                col.a = lerp(col.a, texColor.a * outlineAlpha, edge);
                
                return col;
            }
            ENDCG
        }
    }
}
