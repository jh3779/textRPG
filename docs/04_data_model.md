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
| baseHp / baseAttack / baseDefense | int | 시작 스탯 (직업별로 다름 — 스킬/성장 분기는 없음, 01_requirements Won't). **2026-09-08(DEC-123, Unity 한정) 갱신: 이 값들은 "기본 스탯 + 시작 무기 보너스"가 이미 합산된 최종값**이다 — 아래 신규 필드 참조 |
| startingItems | Item[] (ENT-105 참조) | 시작 인벤토리 — 직업별로 다름(DEC-111, 2026-09-04 추가). 정확한 목록은 OQ-103에서 확정. **2026-09-08부터 이 중 시작 무기(장검/단검 2자루/지팡이)는 실제로 스탯에 영향을 준다(DEC-123)** — 아래 참조 |
| baseMana | int | 신규(DEC-123, Unity 한정). 최대 마나(시작 무기 보너스 포함 최종값). 전사10/도적15/마법사32 |
| attackSpeed | int | 신규(DEC-123). 공격속도(시작 무기 보너스 포함 최종값). 매 턴 플레이어와 적의 값을 비교해 선공을 정한다(높은 쪽 선공, 동률이면 플레이어 우선). 전사-1/도적+3/마법사+1 |
| hasDoubleAttack | bool | 신규(DEC-123). true면 기본 공격이 1회가 아니라 2회 독립 타격(도적 쌍검 패시브만 true) |
| manaSkill / manaSkillName / manaSkillCost | enum / string / int | 신규(DEC-123). 직업별 마나 소모 스킬 — 전사 강타(5)/도적 맹독 일격(7)/마법사 화염구(10) |

**무기 보너스 반영 표(DEC-123, 검증용 — RegressionSmokeTest.TestCharacterClassStats와 대조):**

| 직업 | HP | ATK | DEF | Mana | 공격속도 |
|---|---|---|---|---|---|
| 전사(장검) | 114 | 13 | 5 | 10 | -1 |
| 도적(단검 2자루, 기본 공격 2타) | 85 | 16 | 2 | 15 | +3 |
| 마법사(지팡이) | 75 | 19 | 1 | 32 | +1 |

> DEC-102/DEC-111("스킬트리 없음, 시작 스탯+아이템만")과의 관계는 `06_open_questions.md` DEC-123 참조 —
> 이번 확장은 콘솔 정본이 아니라 **Unity 버전에 한해서만** 그 원칙을 수정한 것이다.

### ENT-102 · Player (기존 `Player` 기반 + classId 추가)
| 필드 | 타입 | 설명 |
|------|------|------|
| classId | string | 선택한 직업 (ENT-101 참조) — 신규 |
| name / hp / maxHp / attack / defense / level / experience / gold | 기존과 동일 | `include/Player.h` 그대로 |
| mana / maxMana | int | 신규(DEC-123, Unity 한정). 캐릭터 생성 시 CharacterClass.baseMana로 가득 채워지며, **이후로는 HP처럼 전투 간 이월되는 지속 자원**이다(전투 승패와 무관하게 자동으로 풀회복되지 않음). 회복 수단은 마나 물약(아이템 사용)·휴식하기(탐색 화면, 지역당 1회, 최대 마나의 40%)·마나 회복(전투 중, 마나가 아니라 "마나 결정" 재료 1개 소모, 최대 마나의 45%) 3가지뿐이다 |
| attackSpeed | int | 신규(DEC-123). CharacterClass.attackSpeed에서 복사. 콘솔 원본 기본 생성자(직업 없음)는 0 |
| hasDoubleAttack | bool | 신규(DEC-123). CharacterClass.hasDoubleAttack에서 복사 |

### ENT-103 · Enemy, ENT-104 · Location, ENT-105 · Item, ENT-106 · Quest
기존 웹 버전 기획(구 문서)과 동일 — 콘솔 버전의 `Enemy`/`Map::Location`/`Item`/`Quest` 필드를 그대로 사용. 필드 정의는 각 C++ 헤더(`include/Enemy.h`, `include/Map.h`, `include/Item.h`, `include/Quest.h`) 참조.

**아트 변형 (2026-09-04 추가, DEC-114):** `Enemy`에 `portraitVariants: string[]`를 하나 추가.

**2026-09-08 갱신(DEC-125, OQ-108 해결): 던전 수호자는 4개 포트레이트 변종 중 조우 시점에 무작위로 1개를 고른다 — 스탯은 절대 바꾸지 않는다.** `GameSession.StartBattleWithGuardian()`이 보스 조우마다(회차당 1회, 보스의 방은 지역 5·마지막) 아래 4개 파일명 중 하나를 무작위로 골라 `portraitVariants`(단일 확정 포트레이트 1개)에 담는다. HP/ATK/DEF/공격속도/EXP/골드/이름은 4개 변종 전부 원본(`src/Game.cpp: Enemy guardian("던전 수호자", 55, 10, 3, 120, 70)`) 그대로 완전히 동일하다 — 고블린(DEC-124)과 달리 스탯 차등화 요구가 없었기 때문이다.

| 포트레이트 파일명 | HP | ATK | DEF | 공격속도 | 비고 |
|---|---|---|---|---|---|
| `enemy_던전수호자_균형형.png` | 55 | 10 | 3 | 0 | 기존 MVP 고정 아트(구 `enemy_던전수호자.png`와 MD5 동일 — 단순 복제본이었음, 파일 자체는 정리 안 함) |
| `enemy_던전수호자_중장형.png` | 55 | 10 | 3 | 0 | 시각만 다름(DEC-114 원안) |
| `enemy_던전수호자_기동형.png` | 55 | 10 | 3 | 0 | 시각만 다름(DEC-114 원안) |
| `enemy_던전수호자_마도형.png` | 55 | 10 | 3 | 0 | 시각만 다름(DEC-114 원안) |

**2026-09-08 갱신(DEC-124, OQ-107 해결): 고블린은 4개 시각 변종이 스탯도 다른 별개의 적이다.** 기존 "그림만 다름"(ASM-104) 가정은 폐기됐다. `GameSession.StartBattleWithGoblin()`이 조우 시점에 4개 중 하나를 무작위로 골라 아래 스탯 + 해당 변종 전용 포트레이트로 `Enemy`를 생성한다. `portraitVariants`는 더 이상 4개 후보를 담은 풀이 아니라, 선택이 끝난 뒤 그 변종에 대응하는 **단일 확정 포트레이트 1개**만 담는다(던전 수호자와 동일한 "단일 확정" 표현 방식).

| 변종 | HP | ATK | DEF | 공격속도(`Enemy.AttackSpeed`, DEC-123) | 포트레이트 | 컨셉 |
|---|---|---|---|---|---|---|
| 약소형 | 20 | 5 | 0 | 0 | `enemy_고블린_약소형.png` | 가장 약한 잡몹 |
| 날렵형 | 25 | 7 | 1 | +2 | `enemy_고블린_날렵형.png` | 빠르지만 얇음(선공 확률 ↑) |
| 거대형 | 45 | 9 | 3 | -1 | `enemy_고블린_거대형.png` | 느리지만 단단하고 세게 침 |
| 주술사형 | 25 | 10 | 0 | 0 | `enemy_고블린_주술사형.png` | 방어 포기하고 화력에 올인 |

EXP/골드 보상(60/25, 원본 `src/Game.cpp: Enemy goblin("고블린", 30, 7, 1, 60, 25)`와 동일)은 4개 변종 모두 공통으로 유지한다 — 보상 밸런스까지 새로 설계하지 않는다(과설계 금지). 이름(`GetName()`)도 4개 변종 전부 "고블린"으로 동일하게 유지한다(전투 로그·UI 문구가 변종별로 갈라지지 않도록).

**신규(2026-09-08, DEC-123, Unity 한정):**
- `Enemy.attackSpeed: int` — 기본값 0. BattleSystem이 매 턴 `player.attackSpeed`와 비교해 선공을 정한다.
- **신규 아이템 2종(ENT-105 Item)**: "마나 물약"(POTION, value=25 — "아이템 사용" 시 이름에 "마나"가 포함되어 있으면 HP 대신 마나를 회복하는 최소 분기로 처리, 낡은 무기고에서 획득) / "마나 결정"(CONSUMABLE, value=1 — 전투 중 "마나 회복" 행동이 소모하는 재료, 고블린 처치 보상으로 지급). 둘 다 콘솔 원본에는 대응 데이터가 없는 신규 콘텐츠.

### ENT-107 · WeaponDefinition / ENT-108 · ArmorDefinition (신규, DEC-129 — Unity 한정)

DEC-123까지는 무기 보너스가 `CharacterClassDatabase`에 "최종 스탯"으로 하드코딩돼 있었다. DEC-129부터는 무기/방어구를 여러 개 보유·교체할 수 있어야 하므로, 보너스 자체를 별도 정본 테이블(`WeaponDatabase.cs`/`ArmorDatabase.cs`)로 분리했다. `CharacterClass`(ENT-101)는 이제 "장비 제외 기본(raw) 스탯"(`baseHpRaw/baseAttackRaw/baseManaRaw/baseDefense`)과 "시작 시 자동 장착하는 기본 무기 이름"(`defaultWeaponName`)만 갖고, 기존 프로퍼티 이름(`BaseHp`/`BaseAttack`/`BaseMana`/`AttackSpeed`/`HasDoubleAttack`)은 "기본 무기를 낀 최종값"을 계산해 반환하는 방식으로 바뀌었다(값 자체는 DEC-123과 동일 — 회귀 테스트로 검증).

| 필드(WeaponDefinition) | 타입 | 설명 |
|------|------|------|
| name | string | 무기 이름(Item.name과 동일한 키로 매칭) |
| requiredClassId | string | 이 무기를 장착할 수 있는 유일한 직업(크로스 직업 장착 차단) |
| hpDelta / attackDelta / speedDelta / manaDelta | int | 장착 시 기본(raw) 스탯에 더해지는 보정치(음수 가능) |
| hasDoubleAttack | bool | true면 기본 공격이 2회 독립 타격(도적 계열 무기만) |
| shopPrice | int | 무기고 구매 가격(기존 무기는 사실상 미사용 — 시작 시 무료 지급) |

| 필드(ArmorDefinition) | 타입 | 설명 |
|------|------|------|
| name | string | 방어구 이름(Item.name과 매칭) |
| defDelta / speedDelta / hpDelta / manaDelta | int | 장착 시 기본(raw) 스탯에 더해지는 보정치. **attackDelta 필드 자체가 없다 — 방어구는 ATK에 절대 관여할 수 없다**(타입 레벨 차단) |
| shopPrice | int | 무기고 구매 가격 |

**무기 2종/직업(기존+신규, DEC-129 확정)**: 전사 장검(기존, HP+4/ATK+3/AtkSpd-1)·대검(신규, HP+8/ATK+6/AtkSpd-3) / 도적 단검(기존, AtkSpd+3/HP-5/2타)·독아 단검(신규, AtkSpd+2/HP-3/ATK+3, 2타 유지) / 마법사 지팡이(기존, AtkSpd+1/ATK+2/마나+2)·수정 지팡이(신규, ATK+4/마나+5/AtkSpd-1).

**방어구 2종(신규 슬롯, 직업 무관 범용)**: 가죽 갑옷(DEF+3/HP+8) / 강화 판금 갑옷(DEF+7/HP+15/AtkSpd-2/마나-3). 콘솔 원본은 물론 DEC-123에도 없던 완전 신규 슬롯이라, 게임 시작 시엔 어떤 직업이든 방어구 미착용(맨몸) 상태다.

**Player(ENT-102) 신규 필드**: `equippedWeaponName: string`(항상 하나는 장착), `equippedArmorName: string?`(null이면 맨몸). `Player.RecomputeStats()`가 raw 기준선 + 현재 장착 무기 보너스 + 현재 장착 방어구 보너스를 합산해 매번 최종 HP/ATK/DEF/공격속도/마나/쌍검패시브를 다시 계산한다 — ATK는 오직 무기(raw+무기 AttackDelta)로만 정해지고 방어구는 절대 관여하지 않으며, DEF는 오직 방어구(raw+방어구 DefDelta)로만 정해지고 무기는 절대 관여하지 않는다.

**획득 경로 3종**: ① 무기고(지역 2) 구매(`GameSession.PurchaseNewWeapon`/`PurchaseArmor`, 신규 무기 3종 각 20골드·가죽 갑옷 10골드·강화 판금 갑옷 20골드) ② 몬스터 드롭(고블린 처치 시 신규 무기 20%/가죽 갑옷 15%, 독립 시행 — 던전 수호자 처치 시 신규 무기+강화 판금 갑옷 확정 100%) ③ 탐색 상자(갈림길, 지역 1, 회차당 1회, 성공률 35% — 성공 시 신규 무기/가죽 갑옷/골드+재료 3갈래 균등). 가격·확률 산정 근거는 `docs/06_open_questions.md` DEC-129 참조.

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
- 저장 필드: 기존 `version, hp, max_hp, attack, defense, level, experience, gold, location, game_round, armory_looted, goblin_defeated, item_*, quest_*` (콘솔 버전 `version=2` 포맷, architecture-audit 2026-09-04 수정분 포함) + 신규 `class_id` + 신규(2026-09-08, DEC-123) `mana, max_mana`(마나는 HP처럼 전투 간·세션 간 이월되는 지속 자원이라 저장이 필요해짐) + 신규(2026-09-08, DEC-129) `attack_speed, has_double_attack, equipped_weapon, equipped_armor`(무기/방어구를 교체할 수 있게 되면서 공격속도·쌍검 패시브·현재 장비 상태도 저장이 필요해짐). `version` 번호는 올리지 않고(기존 2 유지) 필드만 추가했다 — 구버전 세이브(이 필드들이 없음)를 읽으면 전부 기본값(0/false/빈 문자열)으로 채워지는데, `Player.LoadState()`가 `savedMaxMana<=0`이면 클래스 기반 기본 마나값을, `savedEquippedWeaponName`이 비어 있으면 캐릭터 생성 시 채워둔 기본 무기를, `savedEquippedArmorName`이 비어 있으면 맨몸 상태를 그대로 유지하도록 방어해 하위호환을 지켰다(방어구는 구버전이든 실제 맨몸이든 "필드가 비어 있으면 맨몸"이 두 경우 모두 정답이라 마나처럼 별도 구버전 판별 로직이 필요 없었다). `LoadState()`는 또한 저장된 최종 스탯(레벨업 누적분 포함)에서 현재 장착 장비의 보너스를 역산해 raw 기준선을 재구성한다 — 그렇지 않으면 로드 후 장비를 한 번이라도 교체하는 순간 레벨업 보너스가 증발하는 회귀가 생긴다(DEC-129 참조)
- 오프라인 가용성: 완전 오프라인(로컬 파일 I/O만 사용, REQ-NF-102)
- 동기화 시점: SCR-003의 "저장하고 종료" 선택 시에만 기록 — 자동 저장 없음(콘솔 버전과 동일한 "안전 지점 저장" 원칙 유지)
