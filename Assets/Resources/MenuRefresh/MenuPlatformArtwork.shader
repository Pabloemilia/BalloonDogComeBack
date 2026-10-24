Shader "BalloonDog/MenuPlatformArtwork"
{
    Properties
    {
        _MainTex("Approved Platform", 2D) = "white" {}
        _ContactShadow("Contact Shadow Centre and Radius", Vector) = (.5,.60,.16,.095)
        _ShadowStrength("Contact Shadow Strength", Range(0,1)) = .20
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            Cull Off ZWrite Off ZTest LEqual
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _ContactShadow;
                half _ShadowStrength;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                // A soft oval lies on the standing surface, centred under the
                // feet. It fades fully before reaching the rim or white wall.
                float2 offset = (input.uv - _ContactShadow.xy) / max(_ContactShadow.zw, .0001);
                half falloff = 1 - smoothstep(0, 1, dot(offset, offset));
                color.rgb *= 1 - _ShadowStrength * falloff * falloff;
                return color;
            }
            ENDHLSL
        }
    }
}
