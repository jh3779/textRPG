/*
 * UIThemeSetupTool.cs
 *
 * 📝 역할: DEC-128 — docs/design-system/unity-mapping.html M-01이 권고하는
 * ScriptableObject(UITheme.asset) 승격을 실제로 수행하는 1회성 빌드 스크립트.
 * 손으로 .asset YAML을 작성하면 GUID·직렬화가 깨질 위험이 있어, Unity Editor API
 * (ScriptableObject.CreateInstance + AssetDatabase.CreateAsset/SaveAssets)로
 * 안전하게 생성한다.
 *
 * 실행: Unity 메뉴 "TextRPG/3. Create UITheme Asset" 또는 배치 모드에서
 * -executeMethod TextRPG.EditorTools.UIThemeSetupTool.CreateUIThemeAsset 로 실행한다.
 * 이미 asset이 있으면 값만 UIColors 기준으로 다시 채워 덮어쓴다(재실행해도 안전).
 *
 * 값 출처: UIColors.cs 상수를 그대로 복사한다 — 값의 정본은 UIColors 하나로
 * 유지하고, 이 asset은 그 값의 "실체화된 사본"이다(중복 기재로 인한 드리프트
 * 방지). Resources 폴더에 둬서 Resources.Load<UITheme>("UITheme")로 런타임
 * 로드가 가능하게 한다.
 */

using System.IO;
using TextRPG.UI;
using UnityEditor;
using UnityEngine;

namespace TextRPG.EditorTools
{
    public static class UIThemeSetupTool
    {
        private const string ResourcesDir = "Assets/Resources";
        private const string AssetPath = ResourcesDir + "/UITheme.asset";

        [MenuItem("TextRPG/3. Create UITheme Asset")]
        public static void CreateUIThemeAsset()
        {
            if (!Directory.Exists(ResourcesDir))
            {
                Directory.CreateDirectory(ResourcesDir);
                AssetDatabase.Refresh();
            }

            var theme = AssetDatabase.LoadAssetAtPath<UITheme>(AssetPath);
            bool isNew = theme == null;
            if (isNew)
            {
                theme = ScriptableObject.CreateInstance<UITheme>();
            }

            ApplyUIColorsValues(theme);

            if (isNew)
            {
                AssetDatabase.CreateAsset(theme, AssetPath);
            }
            else
            {
                EditorUtility.SetDirty(theme);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[UIThemeSetupTool] UITheme.asset {(isNew ? "생성" : "갱신")} 완료: {AssetPath}");
        }

        /// <summary>
        /// UIColors 정적 상수 값을 UITheme 인스턴스에 리플렉션 없이 그대로 대입한다.
        /// SerializedObject를 통해 private [SerializeField] 값을 직접 써서, 필드 기본값
        /// (선언 시점에 이미 UIColors를 참조하도록 초기화돼 있음)과 항상 일치함을
        /// 재확인·강제한다.
        /// </summary>
        private static void ApplyUIColorsValues(UITheme theme)
        {
            var so = new SerializedObject(theme);

            SetColor(so, "surface", UIColors.Surface);
            SetColor(so, "surfaceContainerHigh", UIColors.SurfaceContainerHigh);
            SetColor(so, "primary", UIColors.Primary);
            SetColor(so, "tertiary", UIColors.Tertiary);
            SetColor(so, "onSurface", UIColors.OnSurface);
            SetColor(so, "onSurfaceVariant", UIColors.OnSurfaceVariant);
            SetColor(so, "outlineVariant", UIColors.OutlineVariant);
            SetColor(so, "textBarBackground", UIColors.TextBarBackground);
            SetColor(so, "scrimBottom", UIColors.ScrimBottom);
            SetColor(so, "inkMark", UIColors.InkMark);
            SetColor(so, "quillInk", UIColors.QuillInk);
            SetColor(so, "foldLine", UIColors.FoldLine);

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetColor(SerializedObject so, string propertyName, Color32 value)
        {
            var prop = so.FindProperty(propertyName);
            prop.colorValue = value;
        }
    }
}
