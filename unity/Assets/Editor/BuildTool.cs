/*
 * BuildTool.cs
 *
 * 📝 역할: DEC-130 — macOS 스탠드얼론(.app) 빌드 파이프라인.
 * 지금까지 이 게임은 한 번도 실제로 빌드되어 사람이 화면으로 본 적이 없었다
 * (전부 배치모드 로직/PlayMode 자동화 테스트뿐). 이 스크립트는 QA를 위해
 * 실제로 실행 가능한 .app을 생성하는 최소 빌드 파이프라인이다.
 *
 * 실행(CLI, 배치모드):
 *   Unity -batchmode -nographics -quit -executeMethod TextRPG.EditorTools.BuildTool.BuildMacStandalone \
 *     -projectPath unity -buildTarget osx
 *
 * 실행(에디터 메뉴): Build/Build macOS Standalone (DEC-130)
 *
 * 산출물: unity/Builds/macOS/DungeonGate.app
 *   (productName은 ProjectSettings.asset의 productName을 그대로 사용 — "DungeonGate")
 *
 * 아키텍처: 아래 SetArchitecture 호출로 ARM64를 지정하지만, 이 로컬 환경(Unity
 * 6000.5.6f1 + Mono 백엔드)에서 실제로 확인한 결과 산출물은 x86_64+arm64
 * Universal 바이너리로 생성됐다(DEC-130에서 `lipo -info`로 실측 확인).
 * Universal이 Intel/Apple Silicon 양쪽 Mac에서 모두 동작해 로컬 QA에는
 * 오히려 더 적합하므로 그대로 둔다 — 문제 없음.
 */

using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace TextRPG.EditorTools
{
    public static class BuildTool
    {
        private const string SceneMainPath = "Assets/Scenes/Main.unity";
        private const string OutputDir = "Builds/macOS";

        [MenuItem("Build/Build macOS Standalone (DEC-130)")]
        public static void BuildMacStandalone()
        {
            string productName = PlayerSettings.productName;
            if (string.IsNullOrEmpty(productName))
            {
                productName = "DungeonGate";
            }

            // ARM64를 지정 시도하지만, 이 환경(Mono 백엔드)에서는 실제로 Universal(x86_64+arm64)
            // 바이너리가 생성됨을 확인했다(DEC-130) — Intel/Apple Silicon 양쪽에서 동작해 문제 없음.
            try
            {
                PlayerSettings.SetArchitecture(NamedBuildTarget.Standalone, 1 /* ARM64 요청 */);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[BuildTool] SetArchitecture 호출 실패(무시하고 기본값으로 진행): {e.Message}");
            }

            string outputDirAbs = Path.Combine(Directory.GetCurrentDirectory(), OutputDir);
            if (!Directory.Exists(outputDirAbs))
            {
                Directory.CreateDirectory(outputDirAbs);
            }

            string appPath = Path.Combine(OutputDir, productName + ".app");

            var options = new BuildPlayerOptions
            {
                scenes = new[] { SceneMainPath },
                locationPathName = appPath,
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.None,
            };

            Debug.Log($"[BuildTool] 빌드 시작 → {appPath}");
            UnityEditor.Build.Reporting.BuildReport report = BuildPipeline.BuildPlayer(options);
            UnityEditor.Build.Reporting.BuildSummary summary = report.summary;

            Debug.Log($"[BuildTool] 빌드 결과: {summary.result}, 소요 시간: {summary.totalTime}, " +
                      $"총 크기: {summary.totalSize} bytes, 에러 {summary.totalErrors}건, 경고 {summary.totalWarnings}건");

            if (summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                throw new Exception($"[BuildTool] 빌드 실패: {summary.result}");
            }

            Debug.Log("[BuildTool] BUILD SUCCEEDED");
        }
    }
}
