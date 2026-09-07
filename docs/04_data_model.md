# 04. 데이터 모델

> 인터뷰 영역 6 정리 · 상태: draft
> 기존 C++ 클래스(Player/Enemy/Item/Quest/Location)의 필드를 그대로 C# 모델로 옮기고, 직업(CharacterClass) 엔티티만 신규 추가.

## 엔티티 (ENT-*)
### ENT-101 · CharacterClass (신규)
| 필드 | 타입 | 설명 |
|------|------|------|
| classId | string | 고유 ID (예: "warrior", "rogue", "mage") |
| displayName | string | 화면에 표시할 이름 |
| portraitAsset | string | 초상화 이미지 리소스 참조 |
| baseHp / baseAttack / baseDefense | int | 시작 스탯 (직업별로 다름 — 스킬/성장 분기는 없음, 01_requirements Won't) |
| startingItems | Item[] (ENT-105 참조) | 시작 인벤토리 — 직업별로 다름(DEC-111, 2026-09-04 추가). 정확한 목록은 OQ-103에서 확정 |

### ENT-102 · Player (기존 `Player` 기반 + classId 추가)
| 필드 | 타입 | 설명 |
|------|------|------|
| classId | string | 선택한 직업 (ENT-101 참조) — 신규 |
| name / hp / maxHp / attack / defense / level / experience / gold | 기존과 동일 | `include/Player.h` 그대로 |

### ENT-103 · Enemy, ENT-104 · Location, ENT-105 · Item, ENT-106 · Quest
기존 웹 버전 기획(구 문서)과 동일 — 콘솔 버전의 `Enemy`/`Map::Location`/`Item`/`Quest` 필드를 그대로 사용. 필드 정의는 각 C++ 헤더(`include/Enemy.h`, `include/Map.h`, `include/Item.h`, `include/Quest.h`) 참조.

**아트 변형 (2026-09-04 추가, DEC-114):** `Enemy`에 `portraitVariants: string[]`를 하나 추가 — 고블린은 `art-assets/enemy_고블린_{약소형|날렵형|거대형|주술사형}.png` 4장 중 전투 시작 시 무작위 1장을 고른다(스탯은 그대로 하나, OQ-107 미결정). 던전 수호자는 `art-assets/enemy_던전수호자.png`(균형형) 고정 1장만 사용 — 나머지 3변종은 데이터 모델에 아직 연결하지 않음(OQ-108).

## 관계 · 소유 단위
```
Player 1 ── 1 CharacterClass (선택 시 확정, 이후 변경 불가)
Player 1 ── 1 Inventory (최대 5칸, Item 0..N)
Player 1 ── N Quest (현재는 1개 고정: "던전 탈출")
GameSession 1 ── 1 Player, 1 Map(Location 5개), 0..1 진행 중 Enemy(전투 중일 때만)
```
- 소유 단위: 로컬 실행 파일 하나 = 플레이어 한 명. 공유 데이터 없음(싱글플레이, 01_requirements Won't — 멀티플레이 제외).

## 저장 · 동기화
- 저장 위치: Unity `Application.persistentDataPath`에 로컬 JSON 파일 (콘솔 버전의 `saves/save1.txt` key=value 대신 JSON으로, 필드는 동일하게 유지)
- 저장 필드: 기존 `version, hp, max_hp, attack, defense, level, experience, gold, location, game_round, armory_looted, goblin_defeated, item_*, quest_*` (콘솔 버전 `version=2` 포맷, architecture-audit 2026-09-04 수정분 포함) + 신규 `class_id`
- 오프라인 가용성: 완전 오프라인(로컬 파일 I/O만 사용, REQ-NF-102)
- 동기화 시점: SCR-003의 "저장하고 종료" 선택 시에만 기록 — 자동 저장 없음(콘솔 버전과 동일한 "안전 지점 저장" 원칙 유지)
