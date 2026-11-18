Shader "Mansion/Haunted Mirror"
{
    Properties
    {
        _GlassColor("Glass Color", Color) = (0.09, 0.23, 0.23, 1)
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _GlassColor;
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
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float haze = saturate(1.0 - length((uv - 0.5) * float2(1.5, 2.0)));
                float sheen = 1.0 - smoothstep(0.05, 0.32, abs(uv.x + uv.y * 0.33 - 0.68));
                float mottling = sin(uv.x * 19.0 + uv.y * 5.0) * sin(uv.y * 23.0);
                half3 color = _GlassColor.rgb * (0.25 + 0.35 * haze + 0.20 * sheen + 0.04 * mottling);

                float2 ghostUV = float2(uv.x, 1.0 - uv.y);
                float head = 1.0 - smoothstep(0.65, 1.13, length((ghostUV - float2(0.53, 0.62)) / float2(0.09, 0.13)));
                float2 veilUV = ghostUV - float2(0.53, 0.44);
                veilUV.x += veilUV.y * 0.28;
                float veil = 1.0 - smoothstep(0.22, 1.20, length(veilUV / float2(0.19, 0.26)));
                float eyes = 1.0 - smoothstep(0.35, 1.0,
                    length(float2(abs(ghostUV.x - 0.53) - 0.032, ghostUV.y - 0.639) / float2(0.019, 0.012)));
                color += half3(0.33, 0.42, 0.39) * (0.29 * head + 0.25 * veil - 0.10 * eyes);

                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
