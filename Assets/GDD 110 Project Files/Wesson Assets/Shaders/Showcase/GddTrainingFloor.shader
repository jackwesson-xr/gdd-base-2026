// Training-ground floor: world-space checker tiles with beveled edges, fine grid lines and glowing accent lines.
// Projects from the dominant axis, so any scaled cube, cylinder or plane tiles at 1 tile per _TileSize meters.
Shader "GDD/Training Floor"
{
    Properties
    {
        _ColorA ("Tile A", Color) = (0.86, 0.88, 0.93, 1)
        _ColorB ("Tile B", Color) = (0.80, 0.83, 0.90, 1)
        _LineColor ("Grid line", Color) = (1, 1, 1, 1)
        _AccentColor ("Accent line", Color) = (0.3, 0.75, 1, 1)
        _TileSize ("Tile size (m)", Float) = 1
        _MajorEvery ("Accent every N tiles", Float) = 4
        _LineWidth ("Grid line width", Range(0, 0.2)) = 0.02
        _MajorWidth ("Accent line width", Range(0, 0.4)) = 0.05
        _Bevel ("Tile bevel", Range(0.001, 0.5)) = 0.1
        _BevelDark ("Bevel darkness", Range(0, 1)) = 0.18
        _Gloss ("Gloss", Range(0, 1)) = 0.3
        _Emission ("Accent glow", Range(0, 4)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ColorA, _ColorB, _LineColor, _AccentColor;
                float _TileSize, _MajorEvery, _LineWidth, _MajorWidth, _Bevel, _BevelDark, _Gloss, _Emission;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float3 normalWS : TEXCOORD1; float fog : TEXCOORD2; };

            Varyings vert (Attributes v)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.fog = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            float GridLine (float2 uv, float width)
            {
                float2 d = abs(frac(uv - 0.5) - 0.5);
                float2 fw = max(fwidth(uv), 1e-4);
                float2 a = 1 - smoothstep(width - fw, width + fw, d);
                return max(a.x, a.y);
            }

            half4 frag (Varyings i) : SV_Target
            {
                float3 n = normalize(i.normalWS);
                float3 an = abs(n);
                float2 uv = an.y > max(an.x, an.z) ? i.positionWS.xz : (an.x > an.z ? i.positionWS.zy : i.positionWS.xy);
                uv /= max(_TileSize, 0.01);

                float2 cell = floor(uv);
                float checker = fmod(abs(cell.x + cell.y), 2.0);
                half3 baseCol = lerp(_ColorA.rgb, _ColorB.rgb, checker);
                float2 f = frac(uv);
                float edge = min(min(f.x, 1 - f.x), min(f.y, 1 - f.y));
                baseCol *= lerp(1 - _BevelDark, 1, smoothstep(0, _Bevel, edge));

                float minor = GridLine(uv, _LineWidth);
                float major = GridLine(uv / max(_MajorEvery, 1), _MajorWidth / max(_MajorEvery, 1));
                half3 albedo = lerp(baseCol, _LineColor.rgb, minor * 0.55);
                albedo = lerp(albedo, _AccentColor.rgb, major);

                Light light = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                half ndl = saturate(dot(n, light.direction));
                half atten = light.shadowAttenuation * light.distanceAttenuation;
                half3 diffuse = albedo * (light.color * ndl * atten + SampleSH(n));
                half3 view = normalize(GetWorldSpaceViewDir(i.positionWS));
                half3 h = normalize(light.direction + view);
                half spec = pow(saturate(dot(n, h)), 48) * _Gloss * atten;
                half3 col = diffuse + spec * light.color + _AccentColor.rgb * major * _Emission;
                col = MixFog(col, i.fog);
                return half4(col, 1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/SHADOWCASTER"
        UsePass "Universal Render Pipeline/Lit/DEPTHONLY"
        UsePass "Universal Render Pipeline/Lit/DEPTHNORMALS"
    }
    FallBack "Universal Render Pipeline/Lit"
}
