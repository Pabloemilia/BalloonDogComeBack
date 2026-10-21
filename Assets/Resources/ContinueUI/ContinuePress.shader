Shader "BalloonDog/ContinuePress"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Press ("Press", Range(0,1)) = 0
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
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="False" }
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 vertex : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; float4 local : TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };
            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float _Press;
            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.local = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                // All coordinates refer to the unchanged 1536x1024 approved artwork.
                float2 p = float2(i.uv.x * 1536.0, (1.0 - i.uv.y) * 1024.0);
                float2 center = float2(769.0, 680.0);
                float2 q = abs(p - center) - (float2(517.0, 100.0) - 58.0);
                float sd = length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - 58.0;
                // No tint outside the face; blend the edge inward to avoid hard seams.
                float face = 1.0 - smoothstep(-7.0, 0.0, sd);
                float press = saturate(_Press);
                float2 source = p - center;
                source.y -= 4.0 * press;
                source /= float2(1.0 - .012 * press, 1.0 - .035 * press);
                source += center;
                float2 pressedUV = float2(source.x / 1536.0, 1.0 - source.y / 1024.0);
                fixed4 normal = tex2D(_MainTex, i.uv) + _TextureSampleAdd;
                fixed4 pressed = tex2D(_MainTex, pressedUV) + _TextureSampleAdd;
                // Nine percent shading retains the bright green and white text.
                pressed.rgb *= 1.0 - .09 * press;
                fixed4 color = lerp(normal, pressed, face * press) * i.color;
                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(i.local.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - .001);
                #endif
                return color;
            }
            ENDCG
        }
    }
    Fallback "UI/Default"
}
