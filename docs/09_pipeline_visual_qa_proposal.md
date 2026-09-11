# 파이프라인 개선 제안: 라이브 스크린샷 기반 시각 검증 도입

작성일: 2026-09-10
상태: **승인 및 반영 완료** — 2026-09-11 사용자 승인, `~/.claude/agents/work-game.md`·
`review-agent.md`·`review-verify-agent.md`에 반영 완료(하네스 DEC-034 기록됨)
범위: `/작업지시` 하네스 파이프라인(work-game → review-agent → review-verify-agent) 중
**게임 UI/비주얼에 영향을 주는 작업**에 한정

이 문서 자체는 실제 코드/설정 변경을 포함하지 않는다(제안서 원문 보존).
아래 "제안 3" 절의 diff는 **2026-09-11 사용자 승인 후 그대로
`~/.claude/agents/work-game.md` 등에 실제 반영 완료**됐다 — 반영된 최종
문구는 각 에이전트 파일을 직접 참고할 것.

---

## 1. 문제 정의 — 왜 지금 이 제안을 하는가

이번 세션 동안 review-agent/review-verify-agent 루프는 **코드·좌표 계산만으로
UI를 검증**해왔다. 이 방식의 한계가 실제로 여러 번 드러났다:

| 사례 | 무슨 일이 있었나 |
|---|---|
| DEC-131 (텍스트 겹침 버그) | 코드 리뷰로는 못 잡았고, **사용자가 실제 빌드를 실행해 스크린샷을 보내온 뒤에야** 발견·수정됨 |
| DEC-133 (전투 이펙트) | 배경 겹침(24%) 문제를 "알려진 한계"로 문서화만 하고 실측 확인 없이 다음 라운드로 넘김 |
| DEC-136 (깃펜 VFX 알파 정리) | review-verify-agent가 픽셀 알파값 변화까지 계산해 "실루엣에 구멍이 생겼다"고 잡아냈지만, 이건 예외적으로 **PIL로 픽셀을 직접 계산**했기 때문이지, 렌더링된 화면을 본 게 아니었다 |
| DEC-141 (캐릭터 아트 교체) | work-game 스스로 "마스킹 셰이더가 새 아트의 튀어나온 무기/지팡이 끝을 과도하게 페이드시킬 수 있다"는 리스크를 문서에 남겼지만 **"스크린샷 캡처 능력이 없어 확인 불가"**로 남겨둠 |
| **DEC-140 (선택 핀)** | review-agent와 review-verify-agent가 **각각 독립적으로 좌표를 직접 재계산**해 "핀이 카드/옆 카드와 안 겹친다"고 8/10·10/10으로 통과시켰다. 그런데 오늘 실제로 라이브 에디터에서 스크린샷을 찍어보니, **핀이 카드 사각형이 아니라 캐릭터 삽화(카드 경계보다 위로 튀어나온 초상화)의 머리 위에 겹쳐 보이는 문제**가 즉시 눈에 띄었다. 좌표 계산 자체는 다 맞았다 — "카드 rect"와 "그 위에 그려진 그림이 실제로 차지하는 시각적 영역"이 다르다는 걸 코드만 봐서는 알 수 없었을 뿐이다. |

**핵심 교훈**: 좌표 계산과 실제 렌더링 결과는 다른 질문에 대한 답이다. 코드
리뷰는 "설계한 대로 배치됐는가"는 검증할 수 있어도, "그렇게 배치된 결과가
보기에 자연스러운가/겹쳐 보이는가"는 실제로 렌더링해서 봐야만 답할 수 있다.
지금까지는 이 마지막 단계가 전부 사용자 몫이었다.

## 2. 새로 확보된 능력

오늘(DEC-144, 병합 대기 중) `unity-redesign`에 아래가 편입됐다:

- `com.unity.pipeline` 패키지 + Unity 공식 `unity` CLI(별도 설치,
  `curl -fsSL https://public-cdn.cloud.unity3d.com/hub/prod/cli/install.sh | UNITY_CLI_CHANNEL=beta bash`)
- 이걸로 **살아있는 Unity 에디터를 명령줄에서 직접 구동**할 수 있다: 씬 열기,
  Play 모드 진입, 버튼 클릭 시뮬레이션(`unity command eval`로 C# 즉석 실행),
  그리고 **`unity command capture_game_view --source screen ...`로 실제
  렌더링된 화면을 PNG로 저장** — 에이전트가 그 파일을 `Read` 도구로 직접
  "본다".
- 상시 QA용 카메라(`QACaptureCamera`, DEC-144)를 씬에 영구로 추가해 별도
  임시 조치 없이 바로 캡처 가능하게 해뒀다(자세한 사용법은
  `docs/08_visual_qa_screenshot.md` 참고).
- Unity 6000.5.x의 알려진 `com.unity.ai.assistant` AssetDatabase 무한루프
  버그(CoplayDev/unity-mcp #1219)는 이 프로젝트(6000.5.6f1, macOS arm64)
  환경에서는 재현되지 않음을 별도 실험 브랜치에서 확인했다.

## 3. 제안 — 파이프라인 3개 에이전트에 반영할 내용 (초안)

### 3-1. `work-game.md` — "UI/UX 작업 시" 절 아래에 추가

```markdown
## 라이브 스크린샷 자체 검증 (Unity, com.unity.pipeline 편입된 프로젝트에 한함)

프로젝트에 `com.unity.pipeline` 패키지가 있고(`unity/Packages/manifest.json`
확인) `unity` CLI가 사용 가능하면, UI/비주얼에 영향을 주는 작업을 마친 뒤
**"컴파일된다"/"좌표상 안 겹친다"로 끝내지 말고 실제로 렌더링해서 봐라**:

1. `unity status`로 연결 가능한 에디터가 있는지 확인, 없으면
   `unity open <프로젝트 경로>` 후 `unity status`가 "ready"가 될 때까지 대기
2. 필요하면 `unity command open_scene --path <씬 경로>`
3. `unity command editor_play`로 Play 모드 진입
4. 확인하려는 화면까지 `unity command eval`로 버튼 클릭 등을 재현
5. **`unity command editor_focus`를 캡처 직전에 반드시 먼저 호출**(안 하면
   스테일 프레임이 캡처됨 — 실제로 겪은 문제)
6. `unity command capture_game_view --source screen --save_path <프로젝트
   내부 상대경로>`로 캡처 후 `Read` 도구로 직접 열어봐서 확인
7. 캡처 후 `unity command editor_stop`, 테스트용 캡처 파일은 커밋하지 말고
   삭제(또는 `.gitignore`된 임시 폴더 사용)

이 절차로 실제 발견한 문제(레이아웃 겹침, 마스킹 잘림 등)는 코드 수정 후 다시
캡처해서 확인하고, **완료 보고에 "라이브 스크린샷으로 확인함" 여부와 확인
내용을 명시**해라. 라이브 에디터 연결이 안 되는 환경(순수 batchmode-only
CI 등)이면 "라이브 검증 불가 — 좌표 계산만으로 판단함"이라고 명시적으로
밝히고 넘어가라(안 되는데 된 것처럼 보고하지 않는다).
```

### 3-2. `review-agent.md` — "리뷰 절차" 2번(주변 코드까지 읽기) 뒤에 추가

```markdown
2-1. **UI/비주얼에 영향을 주는 변경이고, 대상 프로젝트가 Unity +
     `com.unity.pipeline`을 갖췄다면, 좌표 계산만으로 "안 겹침"/"정상
     배치"를 단정하지 마라.** 가능하면 3-1(work-game.md)과 동일한 절차로
     직접 라이브 스크린샷을 찍어 실제 렌더링 결과를 눈으로 확인하고, 그
     스크린샷 파일 경로를 근거로 인용해라. work-game의 좌표 계산이 맞다는
     것과, 그 결과가 실제로 자연스럽게 보인다는 것은 별개의 질문이다 — 후자는
     스크린샷 없이는 답할 수 없다.
```

### 3-3. `review-verify-agent.md` — "감점 사유"에 추가

```markdown
- 라이브 에디터 연결이 가능한 상황이었는데도, 시각적으로 확인 가능한 사안
  (레이아웃 겹침, 마스킹/클리핑 등)을 스크린샷 없이 좌표 계산만으로
  단정한 경우(review-agent 본인이든, work-game 자체 보고를 그대로 믿은
  경우든) — "코드상 계산은 맞다"와 "실제로 그렇게 보인다"는 별개이므로,
  후자를 확인할 수단이 있었는데 안 쓴 것은 근거 부족으로 취급한다.
```

## 4. 적용 범위와 제약

- **Unity + `com.unity.pipeline`이 있는 프로젝트에만 적용.** Unreal 기반
  프로젝트(project-human-p, lanecoach-ai)는 별도 조사 필요 — 이번 제안에
  포함 안 함.
- **모든 작업에 강제하지 않는다.** 순수 로직/백엔드 변경처럼 시각적 표면이
  없는 작업까지 매번 에디터를 열고 대기하게 만들면 배보다 배꼽이 커진다 —
  "UI/비주얼에 영향을 주는 변경"일 때만 트리거.
- **베타 도구다.** `com.unity.ai.assistant`/`com.unity.pipeline`/`unity`
  CLI 전부 pre-release. 이번 검증은 이 프로젝트의 특정 환경(6000.5.6f1,
  macOS arm64)에서만 이뤄졌다 — 다른 Unity 버전/OS에서는 재검증 필요.
- **라이브 에디터를 열고 대기하는 시간이 batchmode보다 오래 걸린다.**
  work-game/review-agent 모두 이 절차를 쓰면 작업 시간이 늘어난다 — 시각
  검증이 실제로 의미 있는 변경(레이아웃/시각효과)에만 선택적으로 쓰는 게
  맞다.

## 5. 실행 계획

1. (이 문서로 제안 완료)
2. 사용자가 승인하면 위 3-1/3-2/3-3의 diff를 실제로
   `~/.claude/agents/work-game.md`, `~/.claude/agents/review-agent.md`,
   `~/.claude/agents/review-verify-agent.md`에 반영
3. 반영 후 다음 UI 관련 DEC부터 실제로 이 절차가 잘 작동하는지 관찰하고,
   필요하면 문구를 다듬는다(예: 캡처 절차가 너무 장황하면 축약)
4. 승인 여부와 관계없이, textRPG 프로젝트 자체의 사용법 문서
   (`docs/08_visual_qa_screenshot.md`)는 DEC-144로 이미 별도 편입됨 — 이건
   "이 프로젝트에서 어떻게 쓰는가"이고, 이 문서는 "하네스 파이프라인 자체를
   바꿀 것인가"라는 상위 결정이라 구분했다.
