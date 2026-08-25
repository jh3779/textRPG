# 04. 데이터 모델

> 인터뷰 영역 6 정리 · 상태: draft
> 기존 C++ 클래스(Player/Enemy/Item/Quest/Location)를 그대로 API 응답 모델로 옮긴 것 — 새로 발명한 필드 없음.

## 엔티티 (ENT-*)
### ENT-001 · Player (`include/Player.h` 기반)
| 필드 | 타입 | 설명 |
|------|------|------|
| name | string | 플레이어 이름 (현재 "모험가" 고정) |
| hp / maxHp | int | 현재/최대 체력 |
| attack | int | 공격력 |
| defense | int | 방어력 |
| level | int | 레벨 |
| experience | int | 경험치 |
| gold | int | 소유 골드 |

### ENT-002 · Enemy (`include/Enemy.h` 기반)
| 필드 | 타입 | 설명 |
|------|------|------|
| name | string | "고블린" / "던전 수호자" |
| hp / maxHp | int | 현재/최대 체력 |
| attack | int | 공격력 |
| defense | int | 방어력 |
| experienceReward / goldReward | int | 처치 시 보상 |

### ENT-003 · Location (`include/Map.h`의 `Location` struct 기반)
| 필드 | 타입 | 설명 |
|------|------|------|
| name | string | 지역 이름 (5개 고정: 던전 입구/갈림길/낡은 무기고/어두운 통로/보스의 방) |
| description | string | 지역 설명 텍스트 |
| hasEnemy | bool | 전투 이벤트 존재 여부 |
| isDeadEnd | bool | 막다른 길 여부 |

### ENT-004 · Item (`include/Item.h` 기반)
| 필드 | 타입 | 설명 |
|------|------|------|
| name | string | 아이템 이름 |
| type | enum | WEAPON / ARMOR / POTION / CONSUMABLE |
| value | int | 효과 값(데미지/방어력/회복량 등) |
| price | int | 판매 가격 |
| description | string | 설명 |

### ENT-005 · Quest (`include/Quest.h` 기반)
| 필드 | 타입 | 설명 |
|------|------|------|
| questId | string | 고유 ID (예: "quest001") |
| title / description | string | 제목/설명 |
| type | enum | KILL_ENEMY / COLLECT_ITEM / EXPLORE / REACH_GOAL |
| status | enum | STATE-003 참조 |
| targetCount / currentCount | int | 목표/현재 진행도 |
| rewardExp / rewardGold | int | 보상 |

## 관계 · 소유 단위
```
Player 1 ── 1 Inventory (최대 5칸, Item 0..N)
Player 1 ── N Quest (현재는 1개 고정: "던전 탈출")
GameSession 1 ── 1 Player, 1 Map(Location 5개 중 currentLocationIndex), 0..1 진행 중 Enemy(전투 중일 때만)
```
- 소유/공유 단위: 개인(브라우저) 단위. 다른 사용자와 공유되는 데이터 없음.
- 접근 규칙: 인증 없음 — 브라우저 세션이 곧 플레이어 식별자(서버는 세션 토큰만으로 상태 구분).

## 저장 · 동기화
- 저장 위치: **브라우저 `localStorage`** (서버는 진행 중 상태만 메모리에 들고, 영구 저장은 하지 않음 — REQ-NF-003)
- 저장 포맷: 기존 `saves/save1.txt`의 key=value 필드를 그대로 JSON으로 직렬화 — `version, hp, max_hp, attack, defense, level, experience, gold, location, game_round, armory_looted, goblin_defeated` (DEC-003)
- 오프라인 가용성: 없음 — 서버(게임 로직 API)가 응답해야 모든 화면 전환이 가능(REQ-NF-002)
- 동기화 시점: SCR-002의 "저장하고 종료" 선택 시에만 서버→클라이언트로 상태를 받아 localStorage에 기록(자동 저장 없음, 콘솔 버전과 동일한 "안전 지점 저장" 원칙 유지)
