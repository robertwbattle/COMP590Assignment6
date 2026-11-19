Shader "Mansion/ComfortVignette"
{
    // Soft-edged circular field-of-view restrictor for comfort during artificial locomotion
    // (after Fernandes & Feiner 2016, "Combating VR sickness through subtle dynamic field-of-view modification").
    // Drawn on a quad just in front of the eyes. The circle is measured per eye in normalised screen space, so it stays
    // centred in each eye with single-pass instanced stereo. _Strength (0..1) is driven from script by movement speed.
    Properties
    {
        _Color("Edge Colour", Color) = (0.098, 0.086, 0.098, 1)
        _Strength("Strength", Range(0, 1)) = 0
        _OpenRadius("Open Radius (no restriction)", Range(0.2, 1.5)) = 1.15
        _ClosedRadius("Radius at Full Strength", Range(0.1, 1.0)) = 0.55
        _Feather("Edge Softness", Range(0.01, 0.6)) = 0.28
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Overlay"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Strength;
                half _OpenRadius;
                half _ClosedRadius;
                half _Feather;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 clip : TEXCOORD0; // xy and w of clip space, divided per fragment
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.clip = float3(o.positionCS.xy, o.positionCS.w);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float2 ndc = i.clip.xy / max(i.clip.z, 1e-4);   // -1..1 across this eye's view
                float r = length(ndc);
                float radius = lerp(_OpenRadius, _ClosedRadius, saturate(_Strength));
                float a = smoothstep(radius, radius + _Feather, r) * saturate(_Strength * 4.0);
                return half4(_Color.rgb, a * _Color.a);
            }
            ENDHLSL
        }
    }
}
