Shader "Mansion/MirrorWhisper"
{
    // A misty hint that floats just in front of the haunted mirror's glass: three upright book spines,
    // left to right in _Color1.._Color3, each with a thin gilt band near the top. It fades in only when the
    // viewer leans close (within _RevealNear, gone beyond _RevealFar). Fully procedural, no textures.
    Properties
    {
        _Color1("Spine 1 (left)", Color) = (0.431, 0.118, 0.133, 1)
        _Color2("Spine 2 (middle)", Color) = (0.290, 0.333, 0.376, 1)
        _Color3("Spine 3 (right)", Color) = (0.180, 0.290, 0.212, 1)
        _GiltColor("Gilt Band", Color) = (0.72, 0.58, 0.32, 1)
        _Intensity("Max Alpha", Range(0, 1)) = 0.8
        _RevealNear("Fully Visible Within (m)", Float) = 1.0
        _RevealFar("Hidden Beyond (m)", Float) = 1.8
        _Flicker("Flicker", Range(0, 1)) = 0.35
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color1;
                half4 _Color2;
                half4 _Color3;
                half4 _GiltColor;
                half _Intensity;
                float _RevealNear;
                float _RevealFar;
                half _Flicker;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.uv = input.uv;
                return output;
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float a = Hash21(i);
                float b = Hash21(i + float2(1.0, 0.0));
                float c = Hash21(i + float2(0.0, 1.0));
                float d = Hash21(i + float2(1.0, 1.0));
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            // Signed distance to a rounded rectangle with half-size b and corner radius r.
            float RoundedBox(float2 p, float2 b, float r)
            {
                float2 q = abs(p) - b + r;
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;
            }

            // One spine glyph. Accumulates colour weighted by coverage, plus total coverage.
            void AddSpine(float2 uv, float centreX, float phase, float3 tint, float mist, float t,
                inout float3 rgb, inout float coverage)
            {
                float2 centre = float2(centreX + sin(t * 0.45 + phase) * 0.004,
                    0.5 + sin(t * 0.31 + phase * 1.7) * 0.003);
                float2 p = uv - centre;
                float d = RoundedBox(p, float2(0.055, 0.29), 0.022);

                float body = 1.0 - smoothstep(-0.012, 0.02, d);
                float halo = exp(-max(d, 0.0) * 28.0) * 0.22;
                float band = body * (1.0 - smoothstep(0.004, 0.012, abs(p.y - 0.215)));

                float flicker = 1.0 - _Flicker
                    * (0.5 + 0.5 * sin(t * (1.7 + phase * 0.6) + phase * 4.1))
                    * (0.5 + 0.5 * sin(t * 7.3 + phase * 2.3));

                float w = (body * (0.55 + 0.45 * mist) + halo) * flicker;
                float g = band * flicker;
                rgb += tint * w + _GiltColor.rgb * g;
                coverage += w + g * 0.35;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.uv;
                float t = _Time.y;

                float mist = ValueNoise(uv * float2(6.0, 9.0) + float2(0.0, t * 0.15)) * 0.6
                    + ValueNoise(uv * 13.0 - float2(t * 0.08, t * 0.05)) * 0.4;

                float3 rgb = 0.0;
                float coverage = 0.0;
                AddSpine(uv, 0.30, 0.0, _Color1.rgb, mist, t, rgb, coverage);
                AddSpine(uv, 0.50, 2.1, _Color2.rgb, mist, t, rgb, coverage);
                AddSpine(uv, 0.70, 4.3, _Color3.rgb, mist, t, rgb, coverage);

                float3 color = rgb / max(coverage, 1e-3);
                color *= 0.85 + 0.35 * mist;

                float vignette = smoothstep(0.0, 0.2, uv.x) * smoothstep(1.0, 0.8, uv.x)
                    * smoothstep(0.0, 0.15, uv.y) * smoothstep(1.0, 0.85, uv.y);

                float dist = distance(_WorldSpaceCameraPos, input.positionWS);
                float reveal = saturate((_RevealFar - dist) / max(_RevealFar - _RevealNear, 1e-4));
                reveal = reveal * reveal * (3.0 - 2.0 * reveal);

                float alpha = saturate(coverage) * vignette * reveal * _Intensity;
                return half4(saturate(color), alpha);
            }
            ENDHLSL
        }
    }
}
