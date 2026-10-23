Shader "BalloonDog/Air Puff"
{
    Properties
    {
        _BaseColor ("Air Tint", Color) = (0.88, 0.97, 1, 0.38)
        _Ring ("Hollow Ring", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _Ring;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half radius = length(input.uv * 2.0 - 1.0);
                half wisp = 1.0 - smoothstep(0.05, 1.0, radius);
                half ring = smoothstep(0.48, 0.7, radius) * (1.0 - smoothstep(0.76, 1.0, radius));
                half opacity = lerp(wisp * wisp, ring, _Ring);
                return half4(_BaseColor.rgb, _BaseColor.a * opacity);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
