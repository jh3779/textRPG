/*
 * TMPSettingsSetupTool.cs
 *
 * 역할: DEC-131 — DEC-130 macOS 빌드 스모크 테스트에서 확정된 버그(TMP Settings
 * 에셋이 프로젝트에 아예 없어서 모든 TextMeshProUGUI.Awake()가
 * `NullReferenceException: TMP_Settings.get_defaultFontAsset()`을 던지고,
 * 화면의 TMP 텍스트가 렌더링되지 않는 문제)를 고치는 1회성 배치 스크립트.
 *
 * Unity 표준 "Window/TextMeshPro/Import TMP Essential Resources" 메뉴가 하는 일
 * (TMPro.TMP_PackageUtilities.ImportProjectResourcesMenu, com.unity.ugui 패키지의
 * Editor/TMP/TMP_PackageUtilities.cs 참고)과 동일하게, 패키지에 번들된
 * "TMP Essential Resources.unitypackage"(LiberationSans SDF 기본 폰트 +
 * TMP Settings.asset 포함)를 Assets/TextMesh Pro/ 아래로 임포트한다.
 *
 * 다만 원본 메뉴는 AssetDatabase.ImportPackage(path, interactive:true)를 쓰는데,
 * 이는 GUI 임포트 확인 창을 띄우는 방식이라 -batchmode -nographics 조합에서는
 * 창을 만들 수 없어 조용히 아무 일도 하지 않고 넘어가는 것을 실측으로 확인했다
 * (실행은 성공(exit 0)하지만 Assets/TextMesh Pro 폴더가 전혀 생기지 않음).
 * 그래서 이 스크립트는 interactive:false로 동일한 .unitypackage를 비대화형
 * 임포트한다 — 배치모드에서도 확실히 즉시 완료된다.
 *
 * 실행: Unity 메뉴 "TextRPG/4. Import TMP Essential Resources (DEC-131)" 또는
 * 배치 모드에서 (CLI에 -quit을 넣지 말 것!)
 * -executeMethod TextRPG.EditorTools.TMPSettingsSetupTool.ImportEssentialsBatch 로 실행.
 * 이미 TMP_Settings 에셋이 있으면 재실행해도 안전(스킵하고 즉시 종료).
 *
 * 중요: AssetDatabase.ImportPackage는 (interactive true/false 둘 다) 비동기이며
 * PackageImport가 여러 에디터 업데이트 틱에 걸쳐 처리된다. CLI에 -quit을 같이
 * 넘기면 executeMethod가 리턴하자마자 프로세스가 죽어버려 임포트가 끝까지
 * 진행되지 못하는 것을 실측으로 확인했다(Assets/TextMesh Pro 폴더가 전혀 생기지
 * 않음). 그래서 -quit CLI 플래그는 쓰지 않고, 이 스크립트가
 * AssetDatabase.importPackageCompleted / importPackageFailed 콜백을 등록해뒀다가
 * 임포트가 실제로 끝난 시점에 스스로 EditorApplication.Exit()를 호출해 종료한다.
 */

using TMPro;
using UnityEditor;
using UnityEngine;

namespace TextRPG.EditorTools
{
    public static class TMPSettingsSetupTool
    {
        [MenuItem("TextRPG/4. Import TMP Essential Resources (DEC-131)")]
        public static void ImportEssentials()
        {
            RunImport(exitWhenDone: false);
        }

        /// <summary>
        /// 배치모드 전용 진입점. CLI에는 -quit을 넣지 말 것 — 이 메서드가 임포트
        /// 완료를 기다렸다가 스스로 EditorApplication.Exit()를 호출한다.
        /// </summary>
        public static void ImportEssentialsBatch()
        {
            RunImport(exitWhenDone: true);
        }

        private static void RunImport(bool exitWhenDone)
        {
            string[] existing = AssetDatabase.FindAssets("t:TMP_Settings");
            if (existing.Length > 0)
            {
                string existingPath = AssetDatabase.GUIDToAssetPath(existing[0]);
                Debug.Log($"[TMPSettingsSetupTool] TMP_Settings 에셋이 이미 존재해 스킵합니다: {existingPath}");
                if (exitWhenDone)
                {
                    EditorApplication.Exit(0);
                }
                return;
            }

            string packageFullPath = TMPro.EditorUtilities.TMP_EditorUtility.packageFullPath;
            string essentialResourcesPath = packageFullPath + "/Package Resources/TMP Essential Resources.unitypackage";

            Debug.Log($"[TMPSettingsSetupTool] TMP Essential Resources 비대화형 임포트 시작: {essentialResourcesPath}");

            if (exitWhenDone)
            {
                AssetDatabase.importPackageCompleted += OnImportCompleted;
                AssetDatabase.importPackageFailed += OnImportFailed;
                AssetDatabase.importPackageCancelled += OnImportCancelled;
            }

            // interactive:false — 배치모드(-nographics)에서 GUI 확인 창 없이 임포트.
            // 비동기라서 이 호출 직후엔 아직 에셋이 없을 수 있다 — 완료는 콜백에서 확인한다.
            AssetDatabase.ImportPackage(essentialResourcesPath, false);
        }

        private static void OnImportCompleted(string packageName)
        {
            Unregister();
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();

            string[] result = AssetDatabase.FindAssets("t:TMP_Settings");
            if (result.Length == 0)
            {
                Debug.LogError("[TMPSettingsSetupTool] importPackageCompleted 콜백까지 왔지만 TMP_Settings 에셋을 찾지 못했습니다 — 실패.");
                EditorApplication.Exit(1);
                return;
            }

            string settingsPath = AssetDatabase.GUIDToAssetPath(result[0]);
            string fontName = TMP_Settings.defaultFontAsset != null ? TMP_Settings.defaultFontAsset.name : "null";
            Debug.Log($"[TMPSettingsSetupTool] TMP_Settings 에셋 생성 확인: {settingsPath}, defaultFontAsset={fontName}");
            EditorApplication.Exit(fontName == "null" ? 1 : 0);
        }

        private static void OnImportFailed(string packageName, string errorMessage)
        {
            Unregister();
            Debug.LogError($"[TMPSettingsSetupTool] 패키지 임포트 실패: {packageName} — {errorMessage}");
            EditorApplication.Exit(1);
        }

        private static void OnImportCancelled(string packageName)
        {
            Unregister();
            Debug.LogError($"[TMPSettingsSetupTool] 패키지 임포트가 취소됨: {packageName}");
            EditorApplication.Exit(1);
        }

        private static void Unregister()
        {
            AssetDatabase.importPackageCompleted -= OnImportCompleted;
            AssetDatabase.importPackageFailed -= OnImportFailed;
            AssetDatabase.importPackageCancelled -= OnImportCancelled;
        }
    }
}
