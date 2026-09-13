# textRPG — Unity 프로젝트 (DungeonGate)

DEC-108: 이 폴더는 별도 저장소가 아니라 메인 저장소(`textRPG/`) 안의 서브폴더다.
콘솔 C++ 버전(`../main.cpp`, `../include/`, `../src/`)과 **병행 개발**한다(DEC-109) —
콘솔 버전은 절대 수정하지 않는다.

## 열기

Unity Hub에서 이 `unity/` 폴더를 프로젝트로 열면 된다. 실제로 Unity Editor
**6000.5.6f1**로 생성된 정식 프로젝트다(로컬 `/Applications/Unity/Hub/Editor/`에
설치돼 있던 유일한 버전을 그대로 사용 — 별도 LTS를 새로 내려받지는 않았다,
`docs/06_open_questions.md` DEC-119 참조). UI 시스템은 **uGUI**로 확정(OQ-101 해결).

메인 씬: `Assets/Scenes/Main.unity`. Play 버튼을 누르면 타이틀 → 직업 선택 →
탐색(대립구도 전투 + 던전 보드맵 포함) → 결과 화면까지 전체 플로우가 동작한다.
자세한 현재 상태는 아래 "현재 상태" 절 참고.

## 폴더 구조

```
Assets/
  Scripts/
    GameLogic/     콘솔 C++ 로직 포팅. UnityEngine을 참조하지 않는 순수 C#.
    Persistence/   세이브/로드(JSON, Application.persistentDataPath). UnityEngine 의존.
    UI/            MonoBehaviour + uGUI. GameLogic을 호출만 하고 규칙은 갖지 않는다.
  Editor/
    ProjectSetupTool.cs      씬/프리팹을 코드로 조립하는 1회성 빌드 스크립트.
                             (재실행하면 Main.unity를 덮어쓰고 다시 생성함 — 수동으로
                             씬을 고친 뒤 재실행하면 그 수정 사항은 사라짐)
    RegressionSmokeTest.cs   포팅 수치 회귀 스모크 테스트(-executeMethod로 실행 가능,
                             이름과 달리 Unity Test Framework EditMode가 아니라
                             커스텀 러너다 — 아래 "테스트 실행" 참고).
    BuildTool.cs             macOS 스탠드얼론 빌드(DEC-130).
  Tests/PlayMode/  진짜 Unity Test Framework PlayMode 테스트(DEC-126, 20개).
  Art/             art-assets/에서 복사해온 확정 아트(원본은 저장소 루트 art-assets/).
    Shaders/       커스텀 UI 셰이더(포트레이트 가장자리 마스킹 등, DEC-116/137).
  Scenes/Main.unity
```

## 테스트 실행 (CLI)

두 가지 테스트가 서로 다르다 — 헷갈리기 쉬우니 구분해서 실행한다.

**① 회귀 스모크 테스트(45개, 커스텀 러너 — `-executeMethod`)**:
```
Unity -batchmode -nographics -projectPath . \
  -executeMethod TextRPG.EditorTools.RegressionSmokeTest.RunAll -quit
```

**② 진짜 Unity Test Framework PlayMode 테스트(20개, DEC-126)**:
```
Unity -batchmode -nographics -projectPath . \
  -runTests -testPlatform PlayMode -testResults <출력경로>.xml
```
`-runTests`와 `-quit`을 **절대 같이 쓰지 않는다** — 같이 쓰면 캐시된 이전 결과가
그대로 XML에 찍히는 경쟁 조건이 실제로 재현된 적이 있다(DEC-140 리뷰 참고).

## macOS 빌드 방법 (DEC-130)

QA용 스탠드얼론 `.app`을 만들려면 `unity/Assets/Editor/BuildTool.cs`를 쓴다.

에디터 메뉴에서: 상단 메뉴 `Build → Build macOS Standalone (DEC-130)`.

CLI(배치모드)에서:

```
/Applications/Unity/Hub/Editor/<버전>/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit \
  -executeMethod TextRPG.EditorTools.BuildTool.BuildMacStandalone \
  -projectPath unity -buildTarget osx
```

산출물: `unity/Builds/macOS/DungeonGate.app` (`Builds/`는 `.gitignore`에 이미
등록돼 있어 커밋되지 않는다). 이 환경(Apple Silicon, Mono 백엔드)에서는 요청한
아키텍처 지정과 무관하게 실제로는 x86_64+arm64 Universal 바이너리가 생성됐다
(`lipo -info`로 실측 확인, DEC-130). 원인은 미상이 아니라 **문서화된 제약**이다
— Unity 매뉴얼상 Player Settings의 Architecture 설정은 에디터의 "Build And Run"
경로에만 적용되고, `BuildTool.cs`는 `BuildOptions.None`(순수 Build, Run 아님)을
쓰기 때문에 이 설정이 애초에 반영되지 않는다. Intel/Apple Silicon 양쪽에서
동작하는 Universal 바이너리가 나오는 것 자체는 로컬 QA에는 문제 없다.

**Gatekeeper 경고가 뜨면**: 로컬에서 빌드한 언사인드 앱이라 macOS가 "확인되지
않은 개발자" 경고를 띄울 수 있다. 이때는 Finder에서 앱을 **우클릭 → 열기**로
한 번 실행하면 그 이후로는 정상적으로 더블클릭 실행이 된다(코드 서명/공증은
하지 않음 — Apple Developer 계정 불필요한 로컬 QA 용도이므로 범위 밖).

**실행 로그**: Player 로그는
`~/Library/Logs/textRPG Project/DungeonGate/Player.log`에 남는다.

## 현재 상태 (2026-09-10, DEC-144 기준)

타이틀 → 직업 선택 → 탐색(대립구도 전투 포함) → 결과 화면까지 전체 플로우가
실제로 동작하고, 아래는 전부 구현 완료 상태다(예전엔 이 섹션이 "미완성" 목록
이었는데, 여러 라운드에 걸쳐 다 해결돼서 지금은 "완료" 목록으로 바꿨다 — 옛
기록은 `docs/06_open_questions.md`의 DEC-131/132/139 등 참고):

- 한글 TMP 폰트 렌더링(DEC-131, DynamicOS 모드로 시스템 폰트 fallback)
- 전투 대립구도(플레이어 vs 몬스터, DEC-132) + 피격 플래시·데미지 팝업(DEC-133)
- 던전 보드맵(카드 버튼이 아니라 실제 노드/경로 시각화, DEC-139)
- 디자인 토큰의 `UITheme.asset` ScriptableObject 승격(DEC-128)
- 잉크마크 선택 효과(DEC-127) — 단, 직업 선택 화면만 DEC-140에서 "카드 상단 핀"
  방식으로 대체(잉크마크 자체는 다른 화면 재적용 가능성 남겨두고 컴포넌트 보존)
- 정식 Unity Test Framework PlayMode 테스트(DEC-126, 20개, 위 "테스트 실행" 참고)
- macOS 스탠드얼론 빌드(DEC-130), GitHub Actions CI(C++ 코어 + pre-commit, DEC-143)
- Unity 공식 MCP 라이브 에디터 제어 + 스크린샷 캡처(DEC-144, `docs/08_visual_qa_screenshot.md`)

## 알려진 미완성 / 제약 (2026-09-10 기준 실제로 남은 것)

- **선택 핀이 캐릭터 초상화와 겹쳐 보이는 문제(DEC-140 후속, 미수정)**: 직업
  선택 카드의 핀은 "카드 사각형(rect)" 기준으로는 6px 여백을 두고 배치돼
  좌표 계산상 안전하지만, 캐릭터 삽화 자체가 카드 rect보다 위로 튀어나와
  있어서(DEC-141 신규 아트) 실제 렌더링 결과는 핀이 캐릭터 머리 위에 겹쳐
  보인다. DEC-144에서 확보한 라이브 스크린샷 캡처로 처음 발견됐고, 아직
  고치지 않았다.
- **가장자리 마스킹 셰이더와 신규 캐릭터 아트의 상호작용 미확인(DEC-141)**:
  검·지팡이 등 돌출부가 마스크 블롭 밖으로 나가 페이드/클리핑될 가능성이
  코드 분석으로는 있다고 판단됐지만, 실제 렌더링으로 확정 확인은 안 됐다.
- 깃펜 필기 VFX 프레임(`vfx_quill_*.png`) 6장에 체스보드 알파 잔재가 남아있음
  (DEC-135 확정, DEC-136에서 고치려다 실루엣이 더 망가져서 되돌림 — 알려진
  한계로 수용, `art-assets/README.md` 참고).
- OQ-105/106(AI 아트 자동 생성 파이프라인)은 계속 보류 — 지금은 수동 업로드
  방식으로 우회 중.
