// GDD 110 fill bar for UI Images: health hearts, stamina pips, anything that empties in steps.
// - Icon: the image that empties (the Image's sprite). Frame: optional overlay that never empties.
// - Fill 0-1 empties from right to left. Chunks splits the bar into equal parts (5 hearts = 5 chunks).
// - "Repeat one icon per chunk" lets you import a single heart and get a row of them.
// - Pure white (#FFFFFF) and transparent pixels are both treated as see-through, so plain JPGs work.
// - Unfilled look: Clip (hidden), Grey, Faded, Dark, or Debug Chunks (each chunk a rainbow color).
Shader "GDD/UI/Fill Icons"
{
    Properties
    {
        [PerRendererData] _MainTex ("Icon (empties)", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _FrameTex ("Frame (never empties)", 2D) = "white" {}
        [Toggle] _UseFrame ("Use frame", Float) = 0
        _Fill ("Fill", Range(0,1)) = 1
        _Chunks ("Chunks", Float) = 5
        [Toggle] _Tile ("Repeat one icon per chunk", Float) = 1
        [Enum(Clip,0,Grey,1,Faded,2,Dark,3,Debug Chunks,4)] _Mode ("Unfilled look", Float) = 1
        _GreyColor ("Grey look tint", Color) = (0.42,0.44,0.5,0.6)
        [Toggle] _WhiteIsClear ("Pure white is transparent", Float) = 1

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
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
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
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t { float4 vertex : POSITION; float4 color : COLOR; float2 texcoord : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 vertex : SV_POSITION; fixed4 color : COLOR; float2 texcoord : TEXCOORD0; float4 worldPosition : TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };

            sampler2D _MainTex, _FrameTex;
            fixed4 _Color, _GreyColor, _TextureSampleAdd;
            float4 _ClipRect;
            float _Fill, _Chunks, _Tile, _Mode, _WhiteIsClear, _UseFrame;

            v2f vert (appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(o.worldPosition);
                o.texcoord = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            fixed3 Rainbow (float h)
            {
                fixed3 c = saturate(abs(frac(h + fixed3(0, 2.0/3.0, 1.0/3.0)) * 6 - 3) - 1);
                return lerp(fixed3(1,1,1), c, 0.85);
            }

            fixed4 Keyed (fixed4 c)
            {
                if (_WhiteIsClear > 0.5 && c.r > 0.996 && c.g > 0.996 && c.b > 0.996) c.a = 0;
                return c;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float chunks = max(1, _Chunks);
                float2 uv = i.texcoord;
                float2 suv = _Tile > 0.5 ? float2(frac(uv.x * chunks), uv.y) : uv;
                float chunk = floor(min(uv.x, 0.9999) * chunks);

                fixed4 col = Keyed(tex2D(_MainTex, suv) + _TextureSampleAdd) * i.color;
                bool filled = uv.x <= _Fill + 0.0001;

                if (_Mode > 3.5)
                {
                    col.rgb = lerp(col.rgb, Rainbow(chunk / chunks), 0.7);
                    if (!filled) col.a *= 0.3;
                }
                else if (!filled)
                {
                    if (_Mode < 0.5) col.a = 0;
                    else if (_Mode < 1.5) { float l = dot(col.rgb, float3(0.3, 0.59, 0.11)); col.rgb = lerp(l.xxx, _GreyColor.rgb, 0.55); col.a *= _GreyColor.a; }
                    else if (_Mode < 2.5) col.a *= 0.22;
                    else { col.rgb *= 0.12; col.a *= 0.85; }
                }

                if (_UseFrame > 0.5)
                {
                    fixed4 fr = Keyed(tex2D(_FrameTex, suv));
                    col.rgb = lerp(col.rgb, fr.rgb, fr.a);
                    col.a = max(col.a, fr.a * i.color.a);
                }

                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(col.a - 0.001);
                #endif
                return col;
            }
            ENDCG
        }
    }
}
