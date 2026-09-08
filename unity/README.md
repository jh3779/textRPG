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
탐색(+ 인라인 전투) → 결과 화면까지 이어지는 최소 골격이 동작한다.

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
    RegressionSmokeTest.cs   포팅 수치 회귀 스모크 테스트(-executeMethod로 실행 가능).
  Art/             art-assets/에서 복사해온 확정 MVP 아트(원본은 저장소 루트 art-assets/).
  Scenes/Main.unity
```

## 회귀 테스트 실행 (CLI)

```
Unity -batchmode -nographics -projectPath . \
  -executeMethod TextRPG.EditorTools.RegressionSmokeTest.RunAll -quit
```

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

**실행 확인 시 참고 — TMP 텍스트 미렌더링 버그(확정)**: Player 로그는
`~/Library/Logs/textRPG Project/DungeonGate/Player.log`에 남는다. `Main.unity`의
TMP 컴포넌트 **29개 전부** `m_fontAsset: {fileID: 0}`(미할당) 상태이고, 프로젝트
어디에도 `TMP Settings.asset`이 없다(`find`로 전체 검색 확인). 그 결과 앱 실행 시
Player.log에 `TMP_Settings.get_defaultFontAsset()` 발 `NullReferenceException`이
**25회** 재현됨을 확인했다 — 가능성이 아니라 확정된 버그다. 앱 자체는 크래시하지
않지만 화면의 모든 TMP 텍스트가 렌더링되지 않을 것으로 확정적으로 예상된다.
다음 작업으로 TMP Settings 에셋 생성 + 각 TMP 컴포넌트에 폰트 할당이 필요하다
(아래 "알려진 미완성 / 다음 작업" 목록 최우선 항목 참조).

## 알려진 미완성 / 다음 작업

- **[최우선] TMP 텍스트 미렌더링 버그(DEC-130에서 확정)**: `Main.unity`의 TMP
  컴포넌트 29개 전부 폰트 애셋이 미할당(`m_fontAsset: {fileID: 0}`)이고 프로젝트에
  `TMP Settings.asset`도 없다 — Player.log에 `TMP_Settings.get_defaultFontAsset()`
  `NullReferenceException`이 25회 재현되는 것으로 확정 확인했다. 화면의 모든 TMP
  텍스트가 안 보일 것으로 예상된다. TMP Settings 에셋 생성 + 각 컴포넌트 폰트
  할당이 다음 작업으로 필요하다.
- BattlePanel 전용 비주얼(VersusStage, 좌우 대립 구도 아트, C-11)은 만들지 않았다 —
  현재는 ExplorePanel 안에서 텍스트/버튼만 바뀌는 방식으로 전투를 처리한다.
- 던전 보드 타일 맵(C-06, DEC-110의 타일 클릭 이동)은 만들지 않았다 — 지금은
  카드형 텍스트 버튼만 있다.
- M-01(디자인 토큰 → `UITheme.asset` ScriptableObject)은 정적 색상 상수
  (`Scripts/UI/UIColors.cs`)로만 축소 구현했다 — 실제 ScriptableObject 에셋 승격은
  Unity Editor에서 사람이 직접 만드는 것을 권장(배치 스크립트로 안전하게 생성하기
  까다로움).
- 양피지 9-slice, 잉크 마크 선택 애니메이션(DEC-118), 배경 마스킹 디테일 등 고급
  비주얼 폴리시는 스코프 밖(요청서 "하지 말 것" 참조).
- 정식 Unity Test Framework(EditMode/PlayMode 테스트 어셈블리)로 승격하지 않고
  `-executeMethod`로 실행하는 간이 스크립트로만 회귀 테스트를 작성했다.
