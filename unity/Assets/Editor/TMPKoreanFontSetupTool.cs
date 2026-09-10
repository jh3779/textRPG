/*
 * TMPKoreanFontSetupTool.cs
 *
 * 역할: DEC-131 후속 — TMP Settings/기본 폰트(LiberationSans SDF)를 채워
 * NullReferenceException은 없앴지만, LiberationSans SDF는 라틴 문자만 지원해서
 * 이 게임의 한글 UI 텍스트가 전부 빈 네모(tofu, U+25A1)로 깨지는 문제(사용자가
 * 실제 빌드를 실행해 "글씨가 다 깨짐"으로 확인)를 고치는 스크립트.
 *
 * 방식: 시스템 폰트 파일을 프로젝트에 복사/임베드하지 않는다. 대신 TextMeshPro가
 * 공식 지원하는 AtlasPopulationMode.DynamicOS(= TMP_FontAsset.CreateFontAsset(
 * familyName, styleName, pointSize) 오버로드, com.unity.ugui 패키지의
 * Runtime/TMP/TMP_FontAsset.cs 492~500행 참고)로 macOS에 기본 내장된 시스템
 * 한글 폰트("Apple SD Gothic Neo")를 "이름으로 참조"만 하는 폰트 에셋을 만든다.
 * 즉 글리프 외곽선 데이터를 리포지토리나 빌드에 복사해 넣지 않고, 런타임에
 * OS(CoreText)에서 그 이름의 설치된 폰트를 찾아 그때그때 SDF를 생성한다.
 *
 * 이렇게 한 이유(정직하게 남김):
 *   ① 라이선스 문제 회피 — Apple 번들 폰트 파일(.ttc/.ttf) 자체를 Assets/에 복사해
 *      빌드에 포함시키면 Apple 폰트 라이선스(자사 플랫폼 소프트웨어에서만 사용
 *      허용, 제3자 앱 에셋으로 재배포 금지) 위반 소지가 있다. DynamicOS 모드는
 *      폰트 파일을 전혀 복사하지 않고 "이 이름의 폰트가 설치돼 있으면 써라"는
 *      런타임 참조만 남기므로 이 위험이 없다(웹 브라우저가 시스템 폰트를
 *      참조하는 것과 같은 방식).
 *   ② 한글은 완성형 음절만 11,172자라 정적(Static) SDF 아틀라스로 전부 구우면
 *      아틀라스가 비대해지고 배치 시간도 오래 걸린다 — DynamicOS는 실제 화면에
 *      나오는 글자만 그때그때 렌더링하므로 가볍다.
 *
 * **알려진 한계(정직하게 남김, 완전한 해결 아님)**: DynamicOS 모드는 빌드를 실행하는
 * 그 컴퓨터에 "Apple SD Gothic Neo"가 실제로 설치돼 있어야 동작한다. 지금은 로컬
 * macOS QA 빌드 목적이라 이 컴퓨터엔 항상 있지만(macOS 기본 내장 폰트), 다른 사람
 * 컴퓨터에 배포하거나 Windows/다른 OS로 빌드하면 그 폰트가 없어 다시 깨질 수 있다.
 * `docs/design-system/`에 이미 계획된 정식 폰트(Pretendard 등)를 프로젝트에 라이선스
 * 확보 후 정적으로 포함시키는 것이 영구 해결책이며, 이번 조치는 그 전까지의
 * 임시(로컬 QA용) 땜질이다.
 *
 * 실행: 배치 모드에서 (CLI에 -quit을 넣지 말 것 — 아래 이유 참고)
 * -executeMethod TextRPG.EditorTools.TMPKoreanFontSetupTool.SetupKoreanFallbackBatch
 */

using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace TextRPG.EditorTools
{
    public static class TMPKoreanFontSetupTool
    {
        private const string FontAssetDir = "Assets/Fonts";
        private const string FontAssetPath = FontAssetDir + "/AppleSDGothicNeo SDF.asset";
        private const string PrimaryFamilyName = "Apple SD Gothic Neo";
        private const string FallbackFamilyName = "AppleGothic";

        [MenuItem("TextRPG/5. Setup Korean Fallback Font (DEC-131)")]
        public static void SetupKoreanFallback()
        {
            Run(exitWhenDone: false);
        }

        /// <summary>배치모드 진입점. CreateFontAsset(familyName, ...)은 동기 호출이라
        /// -quit CLI 플래그와 함께 써도 안전하지만, 일관성을 위해 다른 DEC-131 도구와
        /// 같은 self-exit 패턴을 제공한다.</summary>
        public static void SetupKoreanFallbackBatch()
        {
            Run(exitWhenDone: true);
        }

        private static void Run(bool exitWhenDone)
        {
            if (AssetDatabase.FindAssets("t:TMP_Settings").Length == 0)
            {
                Debug.LogError("[TMPKoreanFontSetupTool] TMP_Settings 에셋이 없습니다 — 먼저 TMPSettingsSetupTool을 실행하세요.");
                ExitIfNeeded(exitWhenDone, 1);
                return;
            }

            TMP_FontAsset koreanFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (koreanFont == null)
            {
                koreanFont = CreateKoreanFontAsset();
            }

            if (koreanFont == null)
            {
                Debug.LogError("[TMPKoreanFontSetupTool] 한글 폰트 에셋 생성 실패 — 시스템에 " +
                    $"'{PrimaryFamilyName}'/'{FallbackFamilyName}' 폰트가 없는 것으로 보입니다.");
                ExitIfNeeded(exitWhenDone, 1);
                return;
            }

            RegisterAsGlobalFallback(koreanFont);

            bool hasHangulDirect = koreanFont.HasCharacter('가', false, true);
            bool hasLatinDirect = koreanFont.HasCharacter('A', false, true);

            // 씬의 TMP_Text들이 실제로 겪는 경로(자신의 폰트가 비어 있으면 defaultFontAsset로
            // 폴백)와 동일하게, defaultFontAsset 기준으로 searchFallbacks:true를 확인한다.
            TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;
            bool hasHangulViaDefault = defaultFont != null && defaultFont.HasCharacter('가', true, true);

            Debug.Log($"[TMPKoreanFontSetupTool] 한글 폴백 폰트 등록 완료: {FontAssetPath} " +
                $"(직접 HasCharacter('가')={hasHangulDirect}, HasCharacter('A')={hasLatinDirect}, " +
                $"defaultFontAsset 경유 HasCharacter('가', searchFallbacks)={hasHangulViaDefault})");

            ExitIfNeeded(exitWhenDone, hasHangulDirect && hasHangulViaDefault ? 0 : 1);
        }

        private static TMP_FontAsset CreateKoreanFontAsset()
        {
            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(PrimaryFamilyName, "Regular", 90);
            if (fontAsset == null)
            {
                Debug.LogWarning($"[TMPKoreanFontSetupTool] '{PrimaryFamilyName}'를 찾지 못해 '{FallbackFamilyName}'로 재시도합니다.");
                fontAsset = TMP_FontAsset.CreateFontAsset(FallbackFamilyName, "Regular", 90);
            }

            if (fontAsset == null)
            {
                return null;
            }

            fontAsset.name = "AppleSDGothicNeo SDF";

            if (!System.IO.Directory.Exists(FontAssetDir))
            {
                System.IO.Directory.CreateDirectory(FontAssetDir);
                AssetDatabase.Refresh();
            }

            AssetDatabase.CreateAsset(fontAsset, FontAssetPath);

            // CreateFontAssetInstance()가 만든 아틀라스 Texture2D/Material은 fontAsset의
            // 필드로만 참조돼 있을 뿐 아직 디스크에 저장된 별도 에셋이 아니다 — 메인 에셋
            // 파일의 서브 에셋으로 명시적으로 추가해야 저장 후 다시 로드했을 때도 참조가
            // 살아있다(TMPro_FontAssetCreatorWindow가 정적 폰트를 저장할 때 쓰는 것과 같은
            // 패턴). 이걸 빠뜨리면 재로드 시 "m_AtlasTextures가 할당되지 않았다"는
            // UnassignedReferenceException이 난다(실제로 재현·확인).
            if (fontAsset.atlasTexture != null)
            {
                AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
            }
            if (fontAsset.material != null)
            {
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        }

        private static void RegisterAsGlobalFallback(TMP_FontAsset koreanFont)
        {
            // ① 전역 폴백 목록(TMP_Settings.fallbackFontAssets) — 기본 폰트에 없는 폴백
            // 목록이 없거나 부족할 때 프로젝트 전체에서 마지막으로 참고하는 안전망.
            List<TMP_FontAsset> fallbacks = TMP_Settings.fallbackFontAssets;
            if (fallbacks == null)
            {
                fallbacks = new List<TMP_FontAsset>();
            }

            // 재실행 과정에서 예전에 지웠다 다시 만든 에셋 등으로 null(끊어진 참조)이
            // 남아있을 수 있다 — HasCharacter_Internal의 폴백 순회 for문이
            // `list[i] != null`을 종료조건으로 쓰기 때문에, 리스트 맨 앞에 null이 하나만
            // 있어도 그 뒤의 유효한 항목(우리 한글 폰트)까지 전부 무시된다(실제로 재현·확인
            // — null 정리 전에는 defaultFontAsset 경유 HasCharacter가 False로 나왔다).
            fallbacks.RemoveAll(f => f == null);

            if (!fallbacks.Contains(koreanFont))
            {
                fallbacks.Add(koreanFont);
            }

            TMP_Settings.fallbackFontAssets = fallbacks;

            // ② defaultFontAsset(LiberationSans SDF) 자신의 fallbackFontAssetTable에도 등록.
            // TMP_FontAsset.HasCharacter(searchFallbacks:true)와 실제 렌더링 시 글리프 조회
            // 경로 둘 다 "그 폰트 자신의 fallbackFontAssetTable"을 우선 확인하므로, 전역
            // 목록만으로는 검증(HasCharacter)과 실제 화면 렌더링 둘 다 확실히 보장되지 않는다
            // — 두 경로 모두에 명시적으로 걸어 확실하게 만든다.
            TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;
            if (defaultFont != null)
            {
                List<TMP_FontAsset> defaultFallbacks = defaultFont.fallbackFontAssetTable;
                if (defaultFallbacks == null)
                {
                    defaultFallbacks = new List<TMP_FontAsset>();
                }

                defaultFallbacks.RemoveAll(f => f == null);

                if (!defaultFallbacks.Contains(koreanFont))
                {
                    defaultFallbacks.Add(koreanFont);
                }

                defaultFont.fallbackFontAssetTable = defaultFallbacks;
                EditorUtility.SetDirty(defaultFont);
            }
            else
            {
                Debug.LogWarning("[TMPKoreanFontSetupTool] TMP_Settings.defaultFontAsset이 비어있어 " +
                    "기본 폰트 자체의 fallbackFontAssetTable에는 등록하지 못했습니다(전역 목록에만 등록됨).");
            }

            EditorUtility.SetDirty(TMP_Settings.instance);
            AssetDatabase.SaveAssets();
        }

        private static void ExitIfNeeded(bool exitWhenDone, int code)
        {
            if (exitWhenDone)
            {
                EditorApplication.Exit(code);
            }
        }
    }
}
