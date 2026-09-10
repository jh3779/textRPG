/*
 * UITheme.cs
 *
 * 📝 역할: docs/design-system/unity-mapping.html M-01 "디자인 토큰 → Unity 자산"이
 * 권고하는 ScriptableObject 승격(DEC-128). UIColors.cs에 정적 상수로만 있던 색상
 * 토큰을 인스펙터에서 노출·수정 가능한 ScriptableObject 필드로 옮겼다.
 *
 * 값 출처: unity/Assets/Scripts/UI/UIColors.cs (docs/design-system/styles.css
 * [data-ds-theme="dark"] 블록, 07_visual_style.md 원본과 동일).
 *
 * 실제 .asset 인스턴스: unity/Assets/Resources/UITheme.asset
 * (생성: unity/Assets/Editor/UIThemeSetupTool.cs → CreateUIThemeAsset(),
 * UIColors 상수 값을 그대로 복사해 채운다 — 값 출처는 UIColors 하나로 유지해
 * 두 곳에 같은 16진수를 중복 기재하다 값이 어긋나는 위험을 없앤다).
 *
 * DEC-128 판단: ProjectSetupTool.cs(에디터 배치 스크립트, 씬 빌드 시 정적으로
 * 값을 읽음)는 계속 UIColors 정적 상수를 그대로 사용한다 — 이 asset을 굳이
 * AssetDatabase.LoadAssetAtPath로 불러와 쓰게 바꾸는 것은 과설계다. 반면 앞으로
 * 런타임에 팔레트를 인스펙터에서 조정하거나 Resources.Load<UITheme>("UITheme")로
 * 읽어야 하는 신규 런타임 UI 컴포넌트는 이 asset을 참조하면 된다(현재 시점
 * *PanelController들은 색상을 코드 상수로 직접 참조하지 않고 ProjectSetupTool이
 * 빌드 시점에 이미 구운 씬 값을 그대로 쓰므로, 기존 컨트롤러를 이 asset을 쓰도록
 * 강제로 바꾸지 않았다 — 순수 리팩터링 범위 유지, 시각적 변화 없음).
 */

using UnityEngine;

namespace TextRPG.UI
{
    [CreateAssetMenu(fileName = "UITheme", menuName = "TextRPG/UI Theme")]
    public class UITheme : ScriptableObject
    {
        [Header("Surface")]
        [SerializeField] private Color surface = UIColors.Surface;
        [SerializeField] private Color surfaceContainerHigh = UIColors.SurfaceContainerHigh;

        [Header("Accent")]
        [SerializeField] private Color primary = UIColors.Primary;
        [SerializeField] private Color tertiary = UIColors.Tertiary;

        [Header("On-Surface Text")]
        [SerializeField] private Color onSurface = UIColors.OnSurface;
        [SerializeField] private Color onSurfaceVariant = UIColors.OnSurfaceVariant;

        [Header("Outline")]
        [SerializeField] private Color outlineVariant = UIColors.OutlineVariant;

        [Header("Overlay")]
        [SerializeField] private Color textBarBackground = UIColors.TextBarBackground;
        [SerializeField] private Color scrimBottom = UIColors.ScrimBottom;

        [Header("DEC-127 잉크마크/깃펜/접힘선")]
        [SerializeField] private Color inkMark = UIColors.InkMark;
        [SerializeField] private Color quillInk = UIColors.QuillInk;
        [SerializeField] private Color foldLine = UIColors.FoldLine;

        public Color Surface => surface;
        public Color SurfaceContainerHigh => surfaceContainerHigh;
        public Color Primary => primary;
        public Color Tertiary => tertiary;
        public Color OnSurface => onSurface;
        public Color OnSurfaceVariant => onSurfaceVariant;
        public Color OutlineVariant => outlineVariant;
        public Color TextBarBackground => textBarBackground;
        public Color ScrimBottom => scrimBottom;
        public Color InkMark => inkMark;
        public Color QuillInk => quillInk;
        public Color FoldLine => foldLine;
    }
}
