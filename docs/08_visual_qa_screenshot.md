# 08. 시각 QA — `unity` CLI + Unity MCP 라이브 에디터 스크린샷

> 상태: confirmed (2026-09-10, DEC-144) — 실험 브랜치 `experiment/unity-claude-mcp`(병합 안 됨,
> 커밋 `9f9d696`)에서 먼저 검증한 뒤 이번 작업으로 프로젝트 표준 도구로 정식 편입.

## 배경 — 왜 필요한가

이 프로젝트는 그동안 "에이전트가 UI를 직접 눈으로 볼 수 없다"는 근본적 한계를 안고
작업해왔다. 코드 리뷰·정적 분석으로 레이아웃 앵커·색상값을 확인할 수는 있어도,
실제로 렌더링됐을 때 요소가 겹치는지·잘리는지·의도한 자리에 있는지는 매번 사람이
빌드를 실행해 직접 봐야 확인 가능했다(예: DEC-131/DEC-132에서 "사용자가 스크린샷으로
직접 확인·보고"해야 발견된 겹침 버그들).

Unity 공식 `com.unity.pipeline` 패키지 + 별도 `unity` CLI 도구는 라이브 에디터를
명령줄/코드로 제어할 수 있게 해주고, 그중 `capture_game_view`는 **실제로 렌더링된
게임 화면을 PNG로 캡처**한다. 이번 세션 중 이 방법으로 직업 선택 화면(SCR-002)의
선택 핀(DEC-140)이 카드 콘텐츠(캐릭터 초상화 머리 부분)와 겹쳐 보이는 배치 결함을
스크린샷으로 실제 확인한 사례가 있었다(실험 세션 중 오케스트레이터 보고 — 이 결함
자체의 수정은 이번 DEC-144 작업 범위가 아니며, 선택 핀 배치는 건드리지 않았다).
이 사례가 "코드 분석만으로는 확정할 수 없던" 시각적 결함을 에이전트 스스로 처음
발견한 사례라, 앞으로의 UI 작업 표준 QA 절차로 편입한다.

## 알려진 제약(먼저 확인 필요)

- **Camera 필요**: `capture_game_view --source screen`은 씬에 `Camera`가 최소 1개
  있어야 정상 렌더링된다. 이 프로젝트는 순수 Screen Space Overlay Canvas 구조라
  원래 카메라가 필요 없었는데(실제 빌드에선 문제없음), 카메라가 아예 없으면 캡처
  결과가 solid cyan 한 장으로만 나온다. DEC-144로 `ProjectSetupTool.BuildMainScene`에
  `QACaptureCamera`(clearFlags=SolidColor, backgroundColor=`UIColors.Surface`
  `#0F0D1A`, cullingMask=Nothing)를 씬에 영구 배치해 해결했다 — 매번 임시 카메라를
  추가/삭제할 필요 없이 씬을 재생성하기만 하면 항상 존재한다.
- **베타 단계 도구**: `com.unity.ai.assistant`(2.17.0-pre.1), `com.unity.pipeline`
  (0.6.0-exp.1), `unity` CLI(1.0.0-beta.9) 전부 pre-release/experimental 버전이다.
- **Unity 6000.5.x AssetDatabase 버그(CoplayDev/unity-mcp #1219)**: 이 프로젝트
  환경(Unity 6000.5.6f1, macOS arm64)에서는 재현되지 않음을 확인했다(batchmode
  임포트/컴파일 정상). 다른 환경(예: Linux CI, 다른 Unity 6000.5.x 마이너 버전)에서는
  재현될 수 있으므로, 이 문서의 절차를 다른 환경에 그대로 적용하기 전에 별도
  검증이 필요하다.

## 설치

```bash
# unity CLI 설치 (베타 채널)
curl -fsSL https://public-cdn.cloud.unity3d.com/hub/prod/cli/install.sh | UNITY_CLI_CHANNEL=beta bash

# 프로젝트에 pipeline 패키지가 이미 매니페스트에 있으면(unity/Packages/manifest.json에
# com.unity.pipeline 존재 확인) 아래 명령은 스킵 가능 — 최초 1회, 또는 매니페스트에
# 아직 없을 때만 실행
unity pipeline install --project-path unity
```

`unity/Packages/manifest.json`에는 DEC-144로 다음 두 패키지가 이미 추가돼 있다:

```json
"com.unity.ai.assistant": "2.17.0-pre.1",
"com.unity.pipeline": "0.6.0-exp.1"
```

## 사용법

1. 에디터를 연다.
   ```bash
   unity open unity
   # 또는 Unity Hub로 unity/ 폴더를 직접 연다
   ```
2. 연결을 확인한다.
   ```bash
   unity status
   ```
3. 필요하면 씬을 연다(보통 `Main.unity`가 이미 열려 있음).
   ```bash
   unity command open_scene --path Assets/Scenes/Main.unity
   ```
4. 플레이 모드로 진입한다.
   ```bash
   unity command editor_play
   ```
5. 버튼 클릭 등 상호작용이 필요하면 `eval`로 C#을 즉석 실행한다.
   ```bash
   unity command eval --code 'GameObject.Find("NewGameButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();'
   ```
6. **캡처 직전에 반드시 `editor_focus`를 먼저 호출한다.** 포커스를 주지 않으면
   스테일(오래된) 프레임이 캡처되는 문제를 실험 중 실제로 겪었다.
   ```bash
   unity command editor_focus
   unity command capture_game_view --source screen --width 1280 --height 800 \
     --save_path Assets/_qa_captures/scr002_class_select.png
   ```

이제 씬에 `QACaptureCamera`가 항상 존재하므로, 임시 카메라를 추가하지 않고도
바로 정상적인 화면이 캡처된다.

## 주의사항

- **저장 경로는 반드시 프로젝트 루트 내부**여야 한다 — 외부 절대경로(`/tmp/...` 등)는
  거부된다.
- 캡처용으로 만든 테스트 이미지는 **커밋하지 않는다**. `unity/Assets/_qa_captures/`를
  표준 캡처 폴더로 쓰고 `unity/.gitignore`에 등록해뒀다 — 확인 후 삭제하거나 그대로
  둬도 커밋되지 않는다.
- 캡처 결과를 근거로 코드 수정을 제안할 때는, 무엇을 어떤 명령으로 캡처했는지
  간단히 남겨야 한다(재현 가능하게).

## 알려진 제약(반복 강조)

이 도구 체인은 베타 단계다. 이번 검증은 이 프로젝트 환경(Unity 6000.5.6f1, macOS
arm64) 하나에서만 이뤄졌다 — 다른 OS(Linux 등)나 다른 Unity 6000.5.x 마이너
버전에서는 위에서 언급한 AssetDatabase 무한루프 버그가 재현될 가능성을 배제할 수
없다. CI(GitHub Actions, DEC-143)에 이 절차를 그대로 편입하기 전에는 별도 검증이
필요하다.
