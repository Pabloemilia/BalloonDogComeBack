// Pause-menu-only finish. Original sprite pixels, alpha and 9-slice geometry
// are untouched; this lowers baked highlight contrast during UI rendering.
Shader "BalloonDog/UI/Pause Button Satin"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _SatinColor ("Satin Base Color", Color) = (0.04,0.62,0.91,1)
        _Softness ("Highlight Softening", Range(0,1)) = 0.55
        _BottomDepth ("Bottom Edge Depth", Range(0,0.4)) = 0
        _DepthUVRange ("Depth Sprite UV Y (min, max)", Vector) = (0,1,0,0)
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
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }
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
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "PauseSatin"
            CGPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"

            struct VertexInput
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct FragmentInput
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 mask : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _TextureSampleAdd;
            fixed4 _Color;
            fixed4 _SatinColor;
            half _Softness;
            half _BottomDepth;
            float4 _DepthUVRange;
            float4 _ClipRect;
            float _UIMaskSoftnessX;
            float _UIMaskSoftnessY;

            FragmentInput vert(VertexInput input)
            {
                FragmentInput output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.texcoord = TRANSFORM_TEX(input.texcoord, _MainTex);
                output.color = input.color * _Color;

                float4 clipRect = clamp(_ClipRect, -2e10, 2e10);
                float2 pixelSize = output.vertex.w;
                pixelSize /= abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));
                output.mask = float4(
                    input.vertex.xy * 2 - clipRect.xy - clipRect.zw,
                    0.25 / (0.25 * float2(_UIMaskSoftnessX, _UIMaskSoftnessY)
                        + abs(pixelSize)));
                return output;
            }

            fixed4 frag(FragmentInput input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                fixed4 color = tex2D(_MainTex, input.texcoord) + _TextureSampleAdd;

                // Only the solid button body is softened. The faint shadow and
                // antialiased edge retain their original RGB and alpha. Use the
                // source alpha, not the fade/tint alpha, so transitions stay stable.
                half bodyWeight = smoothstep(0.5h, 0.95h, color.a);
                color.rgb = lerp(color.rgb, _SatinColor.rgb,
                    saturate(_Softness) * bodyWeight);

                // Enabled only by the mint material; zero is an exact no-op for
                // the blue buttons. Shade the lower body, never its alpha/shadow.
                // These standalone, full-texture sprites use UV Y 0..1. Keep
                // the range explicit for atlas subrects; avoid object-space Y
                // because Canvas batching can transform vertex positions.
                float bodyY = saturate((input.texcoord.y - _DepthUVRange.x) /
                    max(_DepthUVRange.y - _DepthUVRange.x, 0.00001));
                half bottomWeight = 1.0h - smoothstep(0.025h, 0.20h, bodyY);
                color.rgb *= 1.0h - saturate(_BottomDepth) * bottomWeight * bodyWeight;

                // Preserve normal/highlighted/pressed/disabled tints and fades.
                color *= input.color;
                #ifdef UNITY_UI_CLIP_RECT
                half2 mask = saturate((_ClipRect.zw - _ClipRect.xy
                    - abs(input.mask.xy)) * input.mask.zw);
                color.a *= mask.x * mask.y;
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001h);
                #endif

                // Premultiply only at output, matching the blend mode above.
                color.rgb *= color.a;
                return color;
            }
            ENDCG
        }
    }
    Fallback "UI/Default"
}
