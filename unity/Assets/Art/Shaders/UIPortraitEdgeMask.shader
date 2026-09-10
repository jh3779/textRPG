// UIPortraitEdgeMask.shader
//
// 📝 역할: DEC-116(docs/07_visual_style.md) — "인물 이미지 가장자리는 부드럽게 페이드시켜
// 배경과 자연스럽게 섞는다"를 실제로 구현하는 최소 셰이더. DEC-137에서 처음 실제로 연결됐다.
//
// Unity uGUI의 Image.material로 꽂아 쓰는 Unlit 셰이더 — Unity 표준 UI-Default 셰이더(Built-in
// Render Pipeline, com.unity.ugui 패키지의 UI-Default.shader)를 그대로 베이스로 삼아, 알파 채널에
// 가장자리 그라디언트가 들어있는 별도 마스크 텍스처(_MaskTex)를 곱해 최종 알파를 낮추는 부분만
// 추가했다. Stencil/ClipRect/AlphaClip 블록은 RectMask2D/Mask 컴포넌트와 함께 써도 깨지지 않도록
// 표준 UI 셰이더의 보일러플레이트를 그대로 유지한다(과설계 아님 — Unity 기본 셰이더 계약 준수).
//
// 이번 작업(DEC-137)에서 실제로 이 셰이더가 붙는 곳은 정확히 2곳뿐이다: ExplorePanelController의
// PlayerPortrait(마스크: mask_character_softedge.png)와 EnemyPortrait(마스크: mask_enemy_softedge.png).
// 범용 마스킹 프레임워크가 아니다 — ProjectSetupTool.CreatePortraitEdgeMaskMaterial()이 이 두 곳에만
// 머티리얼을 만들어 붙인다.
Shader "TextRPG/UI/PortraitEdgeMask"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _MaskTex ("Edge Mask (Alpha 채널 사용)", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

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
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
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
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 texcoord      : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            sampler2D _MaskTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);

                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;

                // DEC-116/DEC-137 핵심: 마스크 텍스처의 알파 채널(가장자리로 갈수록 0에 가까워지는
                // 그라디언트)을 원본 알파에 곱해 가장자리를 페이드시킨다. 마스크 텍스처가 비어있으면
                // (기본값 "white")이 곱셈은 사실상 no-op이 되어 원본 그대로 보인다 — 안전한 기본값.
                half maskAlpha = tex2D(_MaskTex, IN.texcoord).a;
                color.a *= maskAlpha;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}
