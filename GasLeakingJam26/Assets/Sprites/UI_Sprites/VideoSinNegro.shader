// UI: vuelve transparente el negro SOLO en las esquinas del vídeo (esquinas redondeadas de un MP4 sin alfa)
Shader "UI/VideoSinNegro"
{
    Properties
    {
        [PerRendererData] _MainTex ("Textura", 2D) = "white" {}
        _Color ("Tinte", Color) = (1,1,1,1)
        _Esquina ("Tamaño de la esquina (fracción del ancho)", Range(0.005, 0.25)) = 0.06
        _Umbral ("Umbral de negro", Range(0, 0.5)) = 0.1
        _Suavizado ("Suavizado del borde", Range(0.001, 0.3)) = 0.05
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata_t { float4 vertex : POSITION; float4 color : COLOR; float2 texcoord : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; fixed4 color : COLOR; float2 texcoord : TEXCOORD0; float4 worldPosition : TEXCOORD1; };
            sampler2D _MainTex; float4 _MainTex_TexelSize; fixed4 _Color; float _Esquina; float _Umbral; float _Suavizado; float4 _ClipRect;
            v2f vert (appdata_t v)
            {
                v2f o; o.worldPosition = v.vertex; o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord; o.color = v.color * _Color; return o;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.texcoord) * i.color;
                float2 uv = i.texcoord;
                // tamaño de la esquina en UV (cuadrada en píxeles aunque el vídeo no sea cuadrado)
                float ex = _Esquina;
                float ey = _Esquina * (_MainTex_TexelSize.w > 0 ? _MainTex_TexelSize.z / _MainTex_TexelSize.w : 1);
                bool enEsquina = (uv.x < ex || uv.x > 1 - ex) && (uv.y < ey || uv.y > 1 - ey);
                if (enEsquina)
                {
                    float brillo = max(c.r, max(c.g, c.b));
                    c.a *= smoothstep(_Umbral, _Umbral + _Suavizado, brillo);   // solo aquí: negro -> transparente
                }
                c.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                return c;
            }
            ENDCG
        }
    }
}