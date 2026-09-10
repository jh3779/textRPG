# 05. 상태 머신 (State Machine)

> 인터뷰 영역 6 정리 · 상태: draft
> 콘솔 버전 enum(GameState/BattleResult/QuestStatus)을 그대로 Unity 상태로 매핑, GameState에 직업 선택 상태만 추가.

## STATE-101 · 게임 진행 상태 (기존 `GameState` + 신규 상태)
### 상태값
| 상태 | 의미 | 사용자 표현 |
|------|------|-------------|
| TITLE | 타이틀 화면 | SCR-001 |
| CLASS_SELECT | 직업 선택 중 (신규) | SCR-002 |
| PLAYING | 탐험 중 | SCR-003 |
| BATTLE | 전투 중 (탐험의 하위 화면 전환) | SCR-004 |
| GAME_OVER | 패배로 종료 | SCR-007 |
| VICTORY | 승리로 종료 | SCR-008 |

### 전이
```
TITLE ──[새 게임]→ CLASS_SELECT ──[직업 확정]→ PLAYING
TITLE ──[이어하기]→ PLAYING (직업 이미 복원됨, CLASS_SELECT 건너뜀)
PLAYING ──[HP 0]→ GAME_OVER
PLAYING ──[보스 처치]→ VICTORY
PLAYING ──[저장하고 종료]→ TITLE
GAME_OVER / VICTORY ──[타이틀로]→ TITLE
```

### 불변식 (INV)
- INV-01: CLASS_SELECT에서 직업을 확정하지 않으면 PLAYING으로 진행할 수 없다.
- INV-02: 이어하기는 세이브에 저장된 classId를 그대로 사용하며, 직업을 다시 고를 수 없다(변경 불가 — 04_data_model ENT-102).
- INV-03: 전투 중에는 저장을 제공하지 않는다.

## STATE-102 · 전투 결과 (기존 `BattleResult` 그대로)
콘솔 버전과 동일: PLAYER_WIN / PLAYER_LOSE / PLAYER_FLEE / FLEE_FAILED. 전이 규칙은 `include/BattleSystem.h` 그대로 포팅(수치·확률 변경 없음, 00_project_brief 핵심 가치).

## STATE-103 · 퀘스트 상태 (기존 `QuestStatus` 그대로)
콘솔 버전과 동일: NOT_STARTED → IN_PROGRESS → COMPLETED → REWARDED (FAILED는 현재 미사용, 콘솔 버전과 동일하게 자리만 유지).
