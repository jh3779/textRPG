/*
 * UIColors.cs
 *
 * 📝 역할: docs/design-system/unity-mapping.html M-01 "디자인 토큰 → Unity 자산"의
 * 축소 구현. 정본 문서는 ScriptableObject(UITheme.asset) 하나로 토큰을 옮기라고
 * 권고하지만, 이번 작업에서는 Unity Editor 배치 스크립트로 .asset 파일을 안전하게
 * 수기 생성하기 어려워(잘못된 GUID/직렬화로 깨진 에셋이 될 위험) 우선 정적 색상
 * 상수로만 옮겨두었다 — ScriptableObject로의 승격은 다음 작업으로 남겨둔다(최종 보고 참조).
 *
 * 값 출처: docs/design-system/styles.css [data-ds-theme="dark"] 블록
 * ("실제 게임 톤과 동일", 07_visual_style.md 원본).
 */

using UnityEngine;

namespace TextRPG.UI
{
    public static class UIColors
    {
        // --ds-surface (다크)
        public static readonly Color32 Surface = new Color32(0x0F, 0x0D, 0x1A, 0xFF);
        // --ds-surface-container-high
        public static readonly Color32 SurfaceContainerHigh = new Color32(0x24, 0x1F, 0x30, 0xFF);
        // --ds-primary (금색 포인트)
        public static readonly Color32 Primary = new Color32(0xD4, 0xAF, 0x37, 0xFF);
        // --ds-on-surface
        public static readonly Color32 OnSurface = new Color32(0xE8, 0xE1, 0xF0, 0xFF);
        // --ds-on-surface-variant
        public static readonly Color32 OnSurfaceVariant = new Color32(0xC7, 0xBE, 0xDA, 0xFF);
        // --ds-tertiary (HP/위험 전용)
        public static readonly Color32 Tertiary = new Color32(0xFF, 0x8A, 0x80, 0xFF);
        // --ds-outline-variant
        public static readonly Color32 OutlineVariant = new Color32(0x4A, 0x40, 0x58, 0xFF);

        // .textbar 배경 (rgba(15,13,26,.66))
        public static readonly Color32 TextBarBackground = new Color32(0x0F, 0x0D, 0x1A, 168);
        // .fullbleed::after 하단 그라디언트 근사 단색(스크립트 생성 단순화 — 실제로는 그라디언트)
        public static readonly Color32 ScrimBottom = new Color32(0x06, 0x05, 0x0C, 224);
    }
}
