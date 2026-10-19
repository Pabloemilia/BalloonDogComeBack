Shader "BalloonDog/Screen Cloud Skybox"
{
    Properties
    {
        _MainTex ("Cloud Background", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

            struct Varyings
            {
                float4 position : SV_POSITION;
                float4 screenPosition : TEXCOORD0;
            };

            Varyings vert(float4 vertex : POSITION)
            {
                Varyings output;
                output.position = UnityObjectToClipPos(vertex);
                output.screenPosition = ComputeScreenPos(output.position);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.screenPosition.xy / input.screenPosition.w;
                // Cover the viewport without stretching the supplied landscape art.
                // Portrait screens crop the sides; wide screens crop top and bottom.
                float viewportAspect = _ScreenParams.x / _ScreenParams.y;
                float imageAspect = _MainTex_TexelSize.z / _MainTex_TexelSize.w;
                float2 crop = float2(min(1.0, viewportAspect / imageAspect),
                                     min(1.0, imageAspect / viewportAspect));
                uv = (uv - 0.5) * crop + 0.5;
                return half4(tex2D(_MainTex, uv).rgb, 1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
