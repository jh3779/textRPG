/*
 * UIColors.cs
 *
 * 📝 역할: docs/design-system/unity-mapping.html M-01 "디자인 토큰 → Unity 자산"의
 * 값 정본(source of truth). 원래는 ScriptableObject 승격 전 축소 구현으로 이
 * 정적 상수만 있었으나, DEC-128에서 UITheme(ScriptableObject) 클래스와 실제
 * Assets/Resources/UITheme.asset 인스턴스를 추가해 승격을 완료했다
 * (unity/Assets/Scripts/UI/UITheme.cs, unity/Assets/Editor/UIThemeSetupTool.cs).
 *
 * DEC-128 판단: 이 클래스는 삭제하지 않고 유지한다. ProjectSetupTool.cs(씬을
 * 빌드하는 에디터 배치 스크립트)는 계속 이 정적 상수를 직접 참조한다 — 빌드
 * 시점에 정적으로 값을 읽어 GameObject에 굽는 용도라 자산 로드를 거칠 필요가
 * 없다. UITheme.asset은 이 값들을 그대로 복사한 "실체화된 사본"이며, 앞으로
 * 런타임에 팔레트를 인스펙터 참조나 Resources.Load<UITheme>("UITheme")로 읽어야
 * 하는 신규 UI 컴포넌트가 쓰도록 마련해 둔 것이다(현재 *PanelController들은
 * 색상을 씬에 이미 구운 값으로 쓰고 있어 강제 전환하지 않았다 — 과설계 금지,
 * 순수 리팩터링 범위 유지). 값이 어긋나지 않도록 UITheme.cs 필드 기본값과
 * UIThemeSetupTool의 asset 생성 로직 둘 다 이 클래스의 상수를 참조한다.
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

        // ------------------------------------------------------------------
        // DEC-127 신규: 잉크마크·깃펜·접힘선 시각효과용 색상.
        // docs/design-system/unity-mapping.html M-01/M-02, docs/06_open_questions.md DEC-127 참조.
        // ------------------------------------------------------------------

        // .inkmark .ink-loop / .ink-wash (DEC-118) — 진남색, "플레이어가 표시"
        public static readonly Color32 InkMark = new Color32(0x2B, 0x3F, 0x7A, 0xFF);
        // .qw-pen (DEC-121) — 기존 잉크브라운(.tc-icon/.silhouette 아이콘과 동일 톤), "세계가 기록"
        public static readonly Color32 QuillInk = new Color32(0x6B, 0x4A, 0x1E, 0xFF);
        // .foldline (DEC-121) — 같은 잉크브라운 값의 알파만 낮춰 재사용(새 색상 추가 안 함)
        public static readonly Color32 FoldLine = new Color32(0x6B, 0x4A, 0x1E, 90);

        // ------------------------------------------------------------------
        // DEC-133 신규: 전투 공격 이펙트(피격 플래시·데미지 숫자 팝업) 전용 색상.
        // docs/07_visual_style.md DEC-133, docs/06_open_questions.md DEC-133 참조.
        // ------------------------------------------------------------------

        // 피격 플래시 — 밝은 흰빛이 도는 붉은 틴트. 포트레이트 Image.color를 이 값으로 잠깐
        // 바꿨다가 원래 색(보통 흰색)으로 되돌린다("밝은 흰색/붉은색" 요구사항을 한 값으로 근사).
        public static readonly Color32 HitFlash = new Color32(0xFF, 0x6B, 0x6B, 0xFF);
        // 데미지 숫자 팝업 텍스트 색은 새로 만들지 않고 기존 위험색(Tertiary, #FF8A80)을 재사용한다.
        // 회복(포션 등) 숫자 팝업 전용 — 기존 팔레트에 초록 계열이 없어 신규로 추가.
        public static readonly Color32 HealNumber = new Color32(0x6C, 0xC5, 0x6C, 0xFF);
    }
}
