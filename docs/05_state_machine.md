# 05. 상태 머신 (State Machine)

> 인터뷰 영역 6 정리 — 시간에 따라 변하는 대상의 상태·전이 · 상태: draft
> 기존 C++ enum(GameState/BattleResult/QuestStatus)을 그대로 웹 상태로 매핑.

## STATE-001 · 게임 진행 상태 (`GameState`, `include/Game.h`)
### 상태값
| 상태 | 의미 | 사용자 표현 |
|------|------|-------------|
| MENU | 타이틀 화면 | SCR-001 |
| PLAYING | 탐험 중 | SCR-002 |
| (전투 중은 PLAYING 하위 화면 전환으로 처리, 별도 서버 상태 없음) | 전투 중 | SCR-003 |
| GAME_OVER | 패배로 종료 | SCR-006 |
| QUIT | 승리 또는 저장 후 종료 | SCR-007 또는 SCR-001 복귀 |

### 전이
```
MENU ──[새 게임/이어하기]→ PLAYING
PLAYING ──[HP 0]→ GAME_OVER
PLAYING ──[보스 처치]→ QUIT(승리) ──[타이틀로]→ MENU
PLAYING ──[저장하고 종료]→ QUIT(저장) ──[타이틀로]→ MENU
GAME_OVER ──[타이틀로]→ MENU
```

### 불변식 (INV)
- INV-01: GAME_OVER 상태에서는 저장을 제공하지 않는다(콘솔 버전과 동일 — 패배 후 이어하기 불가, 마지막 저장 지점부터만 재개 가능).
- INV-02: 전투 중에는 "저장하고 종료" 선택지를 노출하지 않는다.

## STATE-002 · 전투 결과 (`BattleResult`, `include/BattleSystem.h`)
### 상태값
| 상태 | 의미 | 사용자 표현 |
|------|------|-------------|
| (전투 진행 중, enum 없음) | 라운드 반복 중 | SCR-003 기본 표시 |
| PLAYER_WIN | 플레이어 승리 | "승리했습니다!" + 보상 |
| PLAYER_LOSE | 플레이어 패배 | "전투에서 쓰러졌습니다" → GAME_OVER |
| PLAYER_FLEE | 도망 성공 | "전투에서 벗어났습니다" → 이전 지역 복귀 |
| FLEE_FAILED | 도망 실패 | "도망에 실패했습니다!" → 적 반격 후 전투 계속 |

### 전이
```
전투 진행 ──[플레이어 HP 0]→ PLAYER_LOSE
전투 진행 ──[적 HP 0]→ PLAYER_WIN
전투 진행 ──[도망 선택, 55% 성공]→ PLAYER_FLEE
전투 진행 ──[도망 선택, 실패]→ FLEE_FAILED ──[적 반격 후]→ 전투 진행(계속)
```

### 불변식 (INV)
- INV-03: PLAYER_WIN/PLAYER_LOSE/PLAYER_FLEE는 종료 상태이며 이후 같은 전투로 되돌아가지 않는다.

## STATE-003 · 퀘스트 상태 (`QuestStatus`, `include/Quest.h`)
### 상태값
| 상태 | 의미 | 사용자 표현 |
|------|------|-------------|
| NOT_STARTED | 시작 전 | "시작 전" |
| IN_PROGRESS | 진행 중 | "진행 중 (0/1)" 등 진행도 표시 |
| COMPLETED | 목표 달성 | "완료" |
| FAILED | 실패(현재 게임에서는 사용 안 함, 자리만 존재) | "실패" |
| REWARDED | 보상 수령 완료 | "보상 수령" |

### 전이
```
NOT_STARTED ──[게임 시작]→ IN_PROGRESS ──[목표 달성]→ COMPLETED ──[보상 지급]→ REWARDED
```

### 불변식 (INV)
- INV-04: 보상은 COMPLETED 상태에서 한 번만 지급된다(중복 지급 금지).
