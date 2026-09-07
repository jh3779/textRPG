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

## 알려진 미완성 / 다음 작업

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
