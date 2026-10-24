Shader "BalloonDog/MenuStudioBalloon"
{
    Properties
    {
        _BaseMap("Skin Texture", 2D) = "white" {}
        _BaseColor("Skin Color", Color) = (1,1,1,1)
        _Silhouette("Collection Silhouette", Float) = 0
        _SilhouetteColor("Silhouette Color", Color) = (.02,.055,.12,1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Back ZWrite On
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _SilhouetteColor;
                half _Silhouette;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; half3 normalWS : TEXCOORD1; float2 uv : TEXCOORD2; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                if (_Silhouette > .5) return half4(_SilhouetteColor.rgb, 1);
                half3 n = normalize(input.normalWS);
                half3 v = GetWorldSpaceNormalizeViewDir(input.positionWS);
                half3 key = normalize(half3(-.6, .9, -.7));
                half3 fill = normalize(half3(.8, .4, .5));
                half3 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb * _BaseColor.rgb;
                half diffuse = .50 + .42 * saturate(dot(n, key)) + .20 * saturate(dot(n, fill));
                half specular = pow(saturate(dot(n, normalize(key + v))), 90) * .95;
                half softbox = pow(saturate(dot(n, normalize(fill + v))), 40) * .30;
                half rim = pow(1 - saturate(dot(n, v)), 4) * .28;
                return half4(baseColor * diffuse + (specular + softbox).xxx + rim * half3(.55,.85,1), 1);
            }
            ENDHLSL
        }
    }
}
