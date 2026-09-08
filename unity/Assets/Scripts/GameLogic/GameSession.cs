/*
 * GameSession.cs
 *
 * 📝 역할: src/Game.cpp의 핵심 흐름(생성자 초기값, handleLocationEvent 분기)을
 * 콘솔 입출력과 분리해 포팅한 것. unity-mapping.html M-04 원칙대로 이 클래스는
 * UnityEngine을 전혀 참조하지 않는 순수 C# 로직이며, View(예: ExplorePanelController)가
 * 이 클래스를 호출해 화면을 갱신한다.
 *
 * 원본과의 구조적 차이(수치 변경 아님):
 * - Game::update()의 while 루프 + std::cin 블로킹 입력 대신, View가 사용자 클릭이 있을 때만
 *   ChooseLocationAction(choice)을 호출하는 구조로 바꿨다.
 * - 콘솔 std::cout 로그 대신 각 메서드가 결과를 리턴값/프로퍼티로 노출한다.
 * - 저장 파일 입출력(File I/O)은 UnityEngine.Application.persistentDataPath에 의존하므로
 *   이 클래스에 넣지 않고 Persistence/SaveSystem.cs로 분리했다(순수성 유지).
 */

using System;
using System.Collections.Generic;

namespace TextRPG.GameLogic
{
    public enum LocationActionResult
    {
        Moved,          // 다른 지역으로 이동함 (전투 진입 여부는 CurrentState로 확인)
        ShowStatus,     // "상태를 확인한다" — View가 상태창/인벤토리 오버레이를 띄워야 함
        SaveAndQuit,    // "저장하고 종료" — View가 SaveSystem으로 파일을 써야 함
        None            // 아무 효과 없음(잘못된 choice 등)
    }

    /// <summary>신규(DEC-123): 탐색 화면 "아이템 사용"의 결과.</summary>
    public enum ItemUseResult
    {
        Success,
        NotFound,
        NotUsable
    }

    /// <summary>신규(DEC-129): 무기 장착 시도 결과.</summary>
    public enum EquipWeaponResult
    {
        Success,
        NotFound,
        NotAWeapon,
        WrongClass
    }

    /// <summary>신규(DEC-129): 방어구 장착 시도 결과. 방어구는 직업 제한이 없어 WrongClass가 없다.</summary>
    public enum EquipArmorResult
    {
        Success,
        NotFound,
        NotAnArmor
    }

    /// <summary>신규(DEC-129): 무기고에서 직업 전용 신규 무기를 구매한 결과.</summary>
    public enum PurchaseWeaponResult
    {
        Success,
        NotAtArmory,
        NoWeaponForClass,
        NotEnoughGold,
        InventoryFull
    }

    /// <summary>신규(DEC-129): 무기고에서 방어구를 구매한 결과.</summary>
    public enum PurchaseArmorResult
    {
        Success,
        NotAtArmory,
        UnknownArmor,
        NotEnoughGold,
        InventoryFull
    }

    /// <summary>신규(DEC-129): 탐색 중 "상자를 조사한다"의 결과.</summary>
    public enum ChestResult
    {
        NotAvailable,
        FoundNothing,
        FoundWeapon,
        FoundArmor,
        FoundGoldAndMaterial
    }

    public class GameSession
    {
        public const string PlayerDisplayName = "모험가"; // 원본 Game() 생성자와 동일: new Player("모험가")

        public Player Player { get; private set; }
        public Map Map { get; private set; }
        public Inventory Inventory { get; private set; }
        public List<Quest> Quests { get; private set; } = new List<Quest>();

        public GameState CurrentState { get; private set; } = GameState.TITLE;
        public int GameRound { get; private set; }
        public bool ArmoryLooted { get; private set; }
        public bool GoblinDefeated { get; private set; }

        public CharacterClass PendingClass { get; private set; }
        public BattleSystem CurrentBattle { get; private set; }
        public Enemy CurrentEnemy { get; private set; }

        // 신규(DEC-123): "휴식하기"를 이미 사용한 지역 인덱스 집합(지역당 1회 제한). 세이브 파일에는 포함하지
        // 않고(단순함 우선), 대신 ConfirmClass()/ResumeFromLoadedState() 양쪽에서 매번 Clear()한다 —
        // GameBootstrap이 GameSession을 앱 실행 중 단 한 번만 생성해 "새 게임"도 같은 인스턴스를 재사용하므로,
        // 여기서 Clear()하지 않으면 이전 회차에 이미 휴식한 지역에서 새 캐릭터가 휴식하기 버튼을 영영 못 보는
        // 회귀가 생긴다(2026-09-08 review-verify-agent Major로 확인되어 수정). 알려진 한계는 06_open_questions.md
        // DEC-123 참조(세이브를 재로드할 때마다 초기화되는 아주 경미한 파밍 경로).
        private readonly HashSet<int> restedLocationIndices = new HashSet<int>();

        /// <summary>신규(DEC-123): "휴식하기" 1회당 회복되는 비율(최대 마나 대비). 30~50% 범위 내 40%로 결정.</summary>
        private const double RestManaRecoverRatio = 0.4;

        // ───────────────────────── 신규(DEC-129): 장비(무기/방어구) 획득 경로 3종 ─────────────────────────
        // restedLocationIndices와 동일한 이유(DEC-123 review-verify-agent Major)로, "상자 조사" 시도 여부도
        // 세이브 파일에는 포함하지 않고(단순함 우선) ConfirmClass()/ResumeFromLoadedState() 양쪽에서 매번
        // 초기화한다 — 그렇지 않으면 GameBootstrap이 재사용하는 같은 GameSession 인스턴스로 "새 게임"을
        // 다시 시작했을 때 이전 회차에 이미 상자를 열어본 적이 있으면 새 캐릭터가 상자를 영영 못 여는
        // 회귀가 생긴다(DEC-123에서 실제로 발견됐던 것과 동일한 종류의 버그 — TestChestAttemptResetsOnNewGame로 검증).

        /// <summary>상자가 있는 지역(갈림길, 아직 다른 특별 이벤트가 없던 지역). DEC-101: 지역 5개 고정, 인덱스 불변.</summary>
        private const int ChestLocationIndex = 1;

        /// <summary>신규(DEC-129): 상자 조사 성공 확률. 30~40% 범위 내 35%로 결정(근거는 InvestigateChest 참조).</summary>
        private const int ChestSuccessChancePercent = 35;

        /// <summary>신규(DEC-129): 상자 조사 성공 시 "골드+재료" 결과로 지급되는 골드량.</summary>
        private const int ChestGoldReward = 15;

        /// <summary>신규(DEC-129): 고블린 처치 시 직업 전용 신규 무기가 드롭될 확률. 15~20% 범위 내 20%로 결정.</summary>
        private const int GoblinWeaponDropChancePercent = 20;

        /// <summary>신규(DEC-129): 고블린 처치 시 가죽 갑옷이 드롭될 확률(무기 드롭과 별개의 독립 시행).</summary>
        private const int GoblinArmorDropChancePercent = 15;

        private bool chestAttempted;

        /// <summary>신규(DEC-129): 이번 회차에 아직 "상자를 조사한다"를 시도하지 않았는지(전체 1회 제한, 지역당이 아니라 게임당 1회 — 상자가 있는 지역이 하나뿐이므로 동일하다).</summary>
        public bool CanInvestigateChestHere()
        {
            return CurrentState == GameState.PLAYING && Map.GetCurrentLocationIndex() == ChestLocationIndex && !chestAttempted;
        }

        /// <summary>SCR-001 "새 게임" → SCR-002(CLASS_SELECT)로 전이(STATE-101).</summary>
        public void BeginNewGameFlow()
        {
            CurrentState = GameState.CLASS_SELECT;
            PendingClass = null;
        }

        public void SelectPendingClass(string classId)
        {
            PendingClass = CharacterClassDatabase.Get(classId);
        }

        /// <summary>INV-01: 직업을 확정해야만 PLAYING으로 진행 가능. src/Game.cpp Game() 생성자 로직 포함.</summary>
        public bool ConfirmClass()
        {
            if (PendingClass == null)
            {
                return false;
            }

            Player = new Player(PlayerDisplayName, PendingClass);
            Map = new Map();
            Inventory = new Inventory(5);
            foreach (var item in PendingClass.CreateStartingItems())
            {
                Inventory.AddItem(item);
            }

            // 원본 Game() 생성자: 회복 물약 1개 기본 지급 (직업과 무관, 콘솔 버전 그대로 유지)
            Inventory.AddItem(new Item("회복 물약", ItemType.POTION, 30, 50, "체력을 30 회복합니다."));

            Quests = new List<Quest>
            {
                new Quest("quest001", "던전 탈출", "보스의 방까지 도달해 던전의 주인을 쓰러뜨리세요.",
                    QuestType.REACH_GOAL, 1, 100, 80)
            };
            foreach (var quest in Quests)
            {
                quest.StartQuest();
            }

            GameRound = 0;
            ArmoryLooted = false;
            GoblinDefeated = false;
            restedLocationIndices.Clear(); // DEC-123 수정: 새 게임 확정마다 휴식 제한을 반드시 초기화한다.
            chestAttempted = false; // DEC-129: 상자 조사 시도 여부도 동일한 이유로 새 게임마다 반드시 초기화한다.
            CurrentState = GameState.PLAYING;
            return true;
        }

        /// <summary>INV-02: 이어하기는 세이브된 classId를 그대로 쓰고 CLASS_SELECT를 건너뛴다.</summary>
        public void ResumeFromLoadedState(Player loadedPlayer, Map loadedMap, Inventory loadedInventory,
            List<Quest> loadedQuests, int gameRound, bool armoryLooted, bool goblinDefeated)
        {
            Player = loadedPlayer;
            Map = loadedMap;
            Inventory = loadedInventory;
            Quests = loadedQuests;
            GameRound = gameRound;
            ArmoryLooted = armoryLooted;
            GoblinDefeated = goblinDefeated;
            restedLocationIndices.Clear(); // DEC-123 수정: 이어하기(로드)마다도 휴식 제한을 초기화한다.
            chestAttempted = false; // DEC-129: 상자 조사 시도 여부도 이어하기마다 초기화한다(동일한 이유).
            CurrentState = GameState.PLAYING;
        }

        /// <summary>현재 지역에서 사용자에게 보여줄 선택지 라벨. src/Game.cpp handleLocationEvent의 각 분기 출력 문구 그대로.</summary>
        public List<string> GetLocationChoices()
        {
            int idx = Map.GetCurrentLocationIndex();
            switch (idx)
            {
                case 0:
                    return new List<string> { "던전에 들어간다", "상태를 확인한다", "저장하고 종료" };
                case 1:
                    return new List<string> { "왼쪽 빛을 따라간다", "오른쪽 통로로 간다", "저장하고 종료" };
                case 2:
                    return new List<string> { "장비를 챙긴다", "지나간다", "저장하고 종료" };
                case 3:
                    // 고블린을 아직 처치하지 않았다면 선택지 없이 즉시 전투 진입(원본과 동일 — 메뉴 자체가 없었음)
                    return GoblinDefeated
                        ? new List<string> { "보스의 방으로 간다", "갈림길로 돌아간다", "저장하고 종료" }
                        : new List<string>();
                case 4:
                    return new List<string> { "보스에게 도전한다", "갈림길로 물러난다", "저장하고 종료" };
                default:
                    return new List<string>();
            }
        }

        /// <summary>
        /// 지역 진입 시 즉시 처리해야 할 자동 이벤트(고블린 조우 등)를 실행한다.
        /// View는 지역을 그릴 때마다 이 메서드를 먼저 호출해야 한다.
        /// </summary>
        public void ProcessLocationEntry()
        {
            if (Map.GetCurrentLocationIndex() == 3 && !GoblinDefeated && CurrentState == GameState.PLAYING)
            {
                StartBattleWithGoblin();
            }
        }

        /// <summary>src/Game.cpp handleLocationEvent의 각 지역 분기를 그대로 옮긴 것. choice는 1-base.</summary>
        public LocationActionResult ChooseLocationAction(int choice)
        {
            int idx = Map.GetCurrentLocationIndex();

            switch (idx)
            {
                case 0:
                    if (choice == 1) { Map.MoveToLocation(1); return LocationActionResult.Moved; }
                    if (choice == 2) { return LocationActionResult.ShowStatus; }
                    if (choice == 3) { return RequestSaveAndQuit(); }
                    return LocationActionResult.None;

                case 1:
                    if (choice == 1) { Map.MoveToLocation(2); return LocationActionResult.Moved; }
                    if (choice == 2) { Map.MoveToLocation(3); ProcessLocationEntry(); return LocationActionResult.Moved; }
                    if (choice == 3) { return RequestSaveAndQuit(); }
                    return LocationActionResult.None;

                case 2:
                    if (choice == 1)
                    {
                        if (!ArmoryLooted)
                        {
                            // DEC-129 Major 수정: Player.SetAttack(Player.GetAttack() + 4)는 attack 필드를
                            // 직접 덮어써 raw 기준선(baseAttackNoWeapon)을 거치지 않는다 — 이후 무기/방어구를
                            // 교체하면 RecomputeStats()가 raw+장비 보너스로 값을 재계산해버려 이 +4가 조용히
                            // 증발하는 버그가 있었다(review-verify-agent Major로 실제 재현 확인). raw 기준선에
                            // 영구 반영하는 AddPermanentAttackBonus()로 교체해 장비를 몇 번을 바꿔도 유지되게 했다.
                            Player.AddPermanentAttackBonus(4);
                            Inventory.AddItem(new Item("작은 회복 물약", ItemType.POTION, 20, 30, "체력을 20 회복합니다."));
                            // 신규(DEC-123): 마나 물약 — "아이템 사용" 시 이름에 "마나"가 포함되어 있으면
                            // HP 대신 마나를 value만큼 회복한다(UseItem 참조).
                            Inventory.AddItem(new Item(CharacterClassDatabase.ManaPotionName, ItemType.POTION, 25, 40,
                                "마나를 25 회복합니다."));
                            ArmoryLooted = true;
                        }
                        Map.MoveToLocation(3);
                        ProcessLocationEntry();
                        return LocationActionResult.Moved;
                    }
                    if (choice == 2) { Map.MoveToLocation(3); ProcessLocationEntry(); return LocationActionResult.Moved; }
                    if (choice == 3) { return RequestSaveAndQuit(); }
                    return LocationActionResult.None;

                case 3:
                    // GoblinDefeated == true일 때만 메뉴가 존재(위 GetLocationChoices 참조)
                    if (!GoblinDefeated) { return LocationActionResult.None; }
                    if (choice == 1) { Map.MoveToLocation(4); return LocationActionResult.Moved; }
                    if (choice == 2) { Map.MoveToLocation(1); return LocationActionResult.Moved; }
                    if (choice == 3) { return RequestSaveAndQuit(); }
                    return LocationActionResult.None;

                case 4:
                    if (choice == 2) { Map.MoveToLocation(1); return LocationActionResult.Moved; }
                    if (choice == 3) { return RequestSaveAndQuit(); }
                    // choice == 1(또는 그 외): 보스에게 도전
                    StartBattleWithGuardian();
                    return LocationActionResult.Moved;

                default:
                    return LocationActionResult.None;
            }
        }

        private LocationActionResult RequestSaveAndQuit()
        {
            // INV-03: 전투 중에는 저장을 제공하지 않는다 — 호출부(UI)가 전투 중엔 이 버튼 자체를 노출하지 않아야 한다.
            CurrentState = GameState.TITLE;
            return LocationActionResult.SaveAndQuit;
        }

        /// <summary>
        /// OQ-107 해결(DEC-124): 고블린 4개 시각 변종(DEC-114)은 그림만 다른 게 아니라 스탯도 다른
        /// 별개의 적이다(ASM-104 가정 폐기). EXP/골드 보상(60/25)은 이번 범위에서 4종 모두 원본
        /// (src/Game.cpp: Enemy goblin("고블린", 30, 7, 1, 60, 25))과 동일하게 유지한다 — 보상 밸런스까지
        /// 새로 설계하는 것은 과설계이므로 스탯(HP/ATK/DEF/공격속도)만 변종별로 분리했다.
        /// 정확한 수치·근거는 06_open_questions.md DEC-124 참조.
        /// </summary>
        private static readonly (string Variant, int Hp, int Atk, int Def, int AttackSpeed)[] GoblinVariants =
        {
            ("약소형", 20, 5, 0, 0),   // 가장 약한 잡몹
            ("날렵형", 25, 7, 1, 2),   // 빠르지만 얇음(공격속도 우위로 선공 확률 ↑)
            ("거대형", 45, 9, 3, -1),  // 느리지만 단단하고 세게 침
            ("주술사형", 25, 10, 0, 0), // 방어 포기하고 화력에 올인
        };

        private void StartBattleWithGoblin()
        {
            // 신규(DEC-124): 조우할 때마다(이 게임은 지역당 고블린 조우가 1회뿐이라 사실상 이번 회차
            // 유일한 고블린 전투 시) 4개 변종 중 하나를 무작위로 고른다. BattleSystem의 강타 스킬 등에서
            // 이미 쓰는 Utils.GenerateRandomNumber를 그대로 재사용한다(inclusive 범위).
            int variantIndex = Utils.GenerateRandomNumber(0, GoblinVariants.Length - 1);
            var variant = GoblinVariants[variantIndex];

            CurrentEnemy = new Enemy("고블린", variant.Hp, variant.Atk, variant.Def, 60, 25)
            {
                AttackSpeed = variant.AttackSpeed,
                // DEC-124: 더 이상 4개 후보를 담은 "풀"이 아니라, 이미 선택이 끝난 변종에 대응하는
                // 단일 확정 포트레이트 1개만 담는다(던전 수호자가 이미 쓰던 단일 확정 관례와 동일 패턴).
                PortraitVariants = new[] { $"enemy_고블린_{variant.Variant}.png" }
            };
            CurrentBattle = new BattleSystem(Player, CurrentEnemy);
            CurrentState = GameState.BATTLE;
        }

        /// <summary>
        /// OQ-108 해결(DEC-125): 던전 수호자 예비 변종 3개(중장형·기동형·마도형, DEC-114)는
        /// "회차마다 랜덤 보스"로 사용하기로 확정됐다. 단 고블린(DEC-124)과 달리 스탯을 변종마다
        /// 다르게 하라는 요구는 없었으므로, 4개 변종(균형형 포함) 전부 스탯은 동일하게 유지하고
        /// 포트레이트(시각)만 랜덤으로 고른다 — DEC-114의 "던전 수호자는 시각만 다름" 원칙 그대로.
        /// </summary>
        private static readonly string[] GuardianPortraitVariants =
        {
            "enemy_던전수호자_균형형.png",
            "enemy_던전수호자_중장형.png",
            "enemy_던전수호자_기동형.png",
            "enemy_던전수호자_마도형.png",
        };

        private void StartBattleWithGuardian()
        {
            // src/Game.cpp: Enemy guardian("던전 수호자", 55, 10, 3, 120, 70); — 스탯은 절대 변경하지 않는다.
            // 신규(DEC-125): 보스 조우는 회차당 1회(지역 5, 마지막)뿐이므로, 이 조우 시점에 포트레이트를
            // 랜덤 선택하는 것으로 "회차마다 랜덤 보스"를 충분히 만족한다(별도의 사전 확정 메커니즘 불필요).
            // GoblinVariants와 동일하게 Utils.GenerateRandomNumber를 재사용한다.
            int portraitIndex = Utils.GenerateRandomNumber(0, GuardianPortraitVariants.Length - 1);

            CurrentEnemy = new Enemy("던전 수호자", 55, 10, 3, 120, 70)
            {
                PortraitVariants = new[] { GuardianPortraitVariants[portraitIndex] }
            };
            CurrentBattle = new BattleSystem(Player, CurrentEnemy);
            CurrentState = GameState.BATTLE;
        }

        /// <summary>
        /// 전투 한 라운드 처리. 전투가 끝나면 이후 흐름(다음 지역 이동/게임오버/승리 처리)까지 이어서 처리한다.
        /// src/Game.cpp handleLocationEvent의 전투 결과 분기(location 3·4) 그대로.
        /// </summary>
        public BattleResult? ProcessBattleTurn(int playerAction)
        {
            var result = CurrentBattle.TakeTurn(playerAction);
            if (result.HasValue)
            {
                ResolveBattleResult(result.Value);
            }
            return result;
        }

        private void ResolveBattleResult(BattleResult result)
        {
            int idx = Map.GetCurrentLocationIndex();

            if (idx == 3)
            {
                if (result == BattleResult.PLAYER_LOSE)
                {
                    CurrentState = GameState.GAME_OVER;
                }
                else if (result == BattleResult.PLAYER_FLEE)
                {
                    Map.MoveToLocation(1);
                    CurrentState = GameState.PLAYING;
                }
                else
                {
                    GoblinDefeated = true;
                    // 신규(DEC-123): 고블린 처치 보상으로 "마나 결정"(재료) 1개 지급 — 보스전 전에
                    // 전투 중 "마나 회복" 행동을 최소 1번은 쓸 수 있게 하는 자연스러운 지급 지점.
                    Inventory.AddItem(new Item(CharacterClassDatabase.ManaRecoveryMaterialName, ItemType.CONSUMABLE, 1, 20,
                        "전투 중 '마나 회복' 행동에 사용하는 소모 재료입니다. 사용 시 최대 마나의 45%를 회복합니다."));

                    // 신규(DEC-129): 몬스터 드롭 경로 — 고블린은 확정 드롭이 아니라 낮은/중간 확률로만
                    // 직업 전용 신규 무기·가죽 갑옷을 떨어뜨린다(두 롤은 서로 독립적 — 둘 다, 하나만,
                    // 혹은 둘 다 안 뜨는 것도 가능). "자기 직업 무기만 드롭 풀에 넣는 게 가장 단순하다"는
                    // 지시에 따라 항상 Player.ClassId에 대응하는 무기만 후보로 삼는다(크로스 직업 드롭 없음).
                    // 인벤토리가 가득 차 있으면 Inventory.AddItem이 false를 반환하고 조용히 실패한다 —
                    // 기존 "마나 결정" 지급도 동일하게 실패를 특별히 알리지 않는 패턴이라 일관성을 유지했다.
                    if (Utils.GenerateRandomNumber(1, 100) <= GoblinWeaponDropChancePercent)
                    {
                        var droppedWeapon = WeaponDatabase.GetNewWeaponForClass(Player.ClassId);
                        if (droppedWeapon != null)
                        {
                            Inventory.AddItem(droppedWeapon.CreateItem());
                        }
                    }
                    if (Utils.GenerateRandomNumber(1, 100) <= GoblinArmorDropChancePercent)
                    {
                        Inventory.AddItem(ArmorDatabase.Get(ArmorDatabase.LeatherArmor).CreateItem());
                    }

                    Map.MoveToLocation(4);
                    CurrentState = GameState.PLAYING;
                }
            }
            else if (idx == 4)
            {
                if (result == BattleResult.PLAYER_WIN)
                {
                    // 신규(DEC-129): 던전 수호자(최종 보스) 처치 시 직업 전용 신규 무기 + 강화 판금 갑옷을
                    // 확정 드롭한다("확정 또는 높은 확률" 중 확정을 택함 — 별도 RNG 없이 항상 지급되므로
                    // 코드가 더 단순하고, 이 시점은 회차의 마지막 전투라 밸런스에 영향을 줄 여지도 없다).
                    var bossWeapon = WeaponDatabase.GetNewWeaponForClass(Player.ClassId);
                    if (bossWeapon != null)
                    {
                        Inventory.AddItem(bossWeapon.CreateItem());
                    }
                    Inventory.AddItem(ArmorDatabase.Get(ArmorDatabase.ReinforcedPlateArmor).CreateItem());

                    foreach (var quest in Quests)
                    {
                        quest.UpdateProgress();
                    }

                    if (Quests.Count > 0 && Quests[0].IsCompleted())
                    {
                        Player.AddExperience(Quests[0].GetRewardExp());
                        Player.AddGold(Quests[0].GetRewardGold());
                    }

                    CurrentState = GameState.VICTORY;
                }
                else if (result == BattleResult.PLAYER_FLEE)
                {
                    Map.MoveToLocation(3);
                    CurrentState = GameState.PLAYING;
                }
                else
                {
                    CurrentState = GameState.GAME_OVER;
                }
            }

            CurrentBattle = null;
            CurrentEnemy = null;
        }

        /// <summary>게임오버/승리 화면에서 "타이틀로 돌아가기"를 눌렀을 때. SCR-007/008 "즉시 재시작 금지" 규칙 유지.</summary>
        public void ReturnToTitle()
        {
            CurrentState = GameState.TITLE;
        }

        public void IncrementRound()
        {
            GameRound++;
        }

        // ───────────────────────── 신규(DEC-123): 마나 회복 경제 ─────────────────────────
        // 마나는 HP처럼 지속 자원으로 취급한다 — 전투가 끝나도 자동으로 풀회복되지 않고,
        // 아래 3가지 경로로만 회복된다: ① 마나 물약(아이템 사용), ② 휴식하기(탐색 화면, 지역당 1회),
        // ③ 마나 회복(전투 중, 마나가 아니라 "마나 결정" 재료 소모).

        /// <summary>
        /// 신규(DEC-123): 인벤토리의 POTION 아이템을 사용한다. 이름에 "마나"가 포함되어 있으면 마나를,
        /// 아니면 HP를 value만큼 회복하고 아이템을 소모한다(범용 아이템 효과 시스템이 아니라 이 최소
        /// 분기만 지원 — 과설계 금지 원칙에 따름).
        /// </summary>
        public ItemUseResult UseItem(int inventoryIndex)
        {
            var item = Inventory?.GetItem(inventoryIndex);
            if (item == null)
            {
                return ItemUseResult.NotFound;
            }

            if (item.GetItemType() != ItemType.POTION)
            {
                return ItemUseResult.NotUsable;
            }

            if (item.GetName().Contains("마나"))
            {
                Player.RecoverMana(item.GetValue());
            }
            else
            {
                Player.SetHp(Player.GetHp() + item.GetValue());
            }

            Inventory.RemoveItem(inventoryIndex);
            return ItemUseResult.Success;
        }

        /// <summary>신규(DEC-123): 현재 지역에서 아직 "휴식하기"를 쓰지 않았는지(지역당 1회 제한).</summary>
        public bool CanRestHere()
        {
            return CurrentState == GameState.PLAYING && !restedLocationIndices.Contains(Map.GetCurrentLocationIndex());
        }

        /// <summary>신규(DEC-123): 휴식하기 — 최대 마나의 40%를 즉시 회복한다. 지역당 1회만 가능.</summary>
        public bool Rest()
        {
            if (!CanRestHere())
            {
                return false;
            }

            int amount = Math.Max(1, (int)Math.Round(Player.GetMaxMana() * RestManaRecoverRatio));
            Player.RecoverMana(amount);
            restedLocationIndices.Add(Map.GetCurrentLocationIndex());
            return true;
        }

        /// <summary>신규(DEC-123): 전투 중 "마나 회복"에 쓸 재료("마나 결정")를 인벤토리가 갖고 있는지.</summary>
        public bool HasManaRecoveryMaterial()
        {
            return FindManaRecoveryMaterialIndex() >= 0;
        }

        private int FindManaRecoveryMaterialIndex()
        {
            if (Inventory == null)
            {
                return -1;
            }

            for (int i = 0; i < Inventory.GetItemCount(); i++)
            {
                var item = Inventory.GetItem(i);
                if (item.GetItemType() == ItemType.CONSUMABLE && item.GetName() == CharacterClassDatabase.ManaRecoveryMaterialName)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// 신규(DEC-123): 전투 중 "마나 회복" 행동 — 재료("마나 결정") 1개를 소모하고 BattleSystem에
        /// ActionRecoverMana 턴을 진행시킨다. 재료가 없으면 아무 것도 소모하지 않고 false를 반환한다
        /// (UI가 "재료가 없습니다" 안내 후 다시 선택하게 해야 함).
        /// </summary>
        public bool TryUseManaRecoverySkillInBattle(out BattleResult? battleResult)
        {
            battleResult = null;

            if (CurrentBattle == null)
            {
                return false;
            }

            int index = FindManaRecoveryMaterialIndex();
            if (index < 0)
            {
                return false;
            }

            Inventory.RemoveItem(index);
            battleResult = ProcessBattleTurn(BattleSystem.ActionRecoverMana);
            return true;
        }

        // ───────────────────────── 신규(DEC-129): 장비(무기/방어구) 장착·구매·상자 획득 ─────────────────────────

        /// <summary>
        /// 신규(DEC-129): 인벤토리 인덱스로 무기를 장착한다. 아이템이 없거나 WEAPON 타입이 아니거나,
        /// 현재 플레이어 직업과 맞지 않는 무기(크로스 직업 장착)면 실패한다 — 무기는 직업 전용이다
        /// (전사는 검류만, 도적은 단검류만, 마법사는 지팡이류만).
        /// </summary>
        public EquipWeaponResult EquipWeapon(int inventoryIndex)
        {
            var item = Inventory?.GetItem(inventoryIndex);
            if (item == null)
            {
                return EquipWeaponResult.NotFound;
            }
            if (item.GetItemType() != ItemType.WEAPON)
            {
                return EquipWeaponResult.NotAWeapon;
            }

            var weaponDef = WeaponDatabase.Get(item.GetName());
            if (weaponDef == null || weaponDef.RequiredClassId != Player.ClassId)
            {
                return EquipWeaponResult.WrongClass;
            }

            Player.EquipWeapon(weaponDef);
            return EquipWeaponResult.Success;
        }

        /// <summary>
        /// 신규(DEC-129): 이름으로 무기를 찾아 장착한다(UI가 인벤토리를 무기 이름 기준으로 중복 제거해
        /// 보여줄 때 편의용 — 예: 도적의 "단검" 2자루 중 아무 인덱스나 찾아 장착). 같은 이름의 첫 번째
        /// WEAPON 아이템을 찾아 EquipWeapon(index)에 위임한다.
        /// </summary>
        public EquipWeaponResult EquipWeaponByName(string weaponName)
        {
            for (int i = 0; i < (Inventory?.GetItemCount() ?? 0); i++)
            {
                var item = Inventory.GetItem(i);
                if (item.GetItemType() == ItemType.WEAPON && item.GetName() == weaponName)
                {
                    return EquipWeapon(i);
                }
            }
            return EquipWeaponResult.NotFound;
        }

        /// <summary>
        /// 신규(DEC-129): 인벤토리 인덱스로 방어구를 장착한다. 방어구는 직업 제한이 없어(범용) 무기보다
        /// 검증이 단순하다 — 아이템이 없거나 ARMOR 타입이 아니면 실패.
        /// </summary>
        public EquipArmorResult EquipArmor(int inventoryIndex)
        {
            var item = Inventory?.GetItem(inventoryIndex);
            if (item == null)
            {
                return EquipArmorResult.NotFound;
            }
            if (item.GetItemType() != ItemType.ARMOR)
            {
                return EquipArmorResult.NotAnArmor;
            }

            var armorDef = ArmorDatabase.Get(item.GetName());
            if (armorDef == null)
            {
                return EquipArmorResult.NotAnArmor;
            }

            Player.EquipArmor(armorDef);
            return EquipArmorResult.Success;
        }

        /// <summary>신규(DEC-129): 이름으로 방어구를 찾아 장착한다(EquipWeaponByName과 동일한 편의 패턴).</summary>
        public EquipArmorResult EquipArmorByName(string armorName)
        {
            for (int i = 0; i < (Inventory?.GetItemCount() ?? 0); i++)
            {
                var item = Inventory.GetItem(i);
                if (item.GetItemType() == ItemType.ARMOR && item.GetName() == armorName)
                {
                    return EquipArmor(i);
                }
            }
            return EquipArmorResult.NotFound;
        }

        /// <summary>신규(DEC-129): 방어구를 벗는다(맨몸으로). 인벤토리에서 아이템이 사라지지는 않는다 — "장착 해제"일 뿐, 버리기가 아니다.</summary>
        public void UnequipArmor()
        {
            Player?.UnequipArmor();
        }

        /// <summary>
        /// 신규(DEC-129): 무기고(지역 2)에서 플레이어 직업 전용 신규 무기(대검/독아 단검/수정 지팡이)를
        /// 구매한다. 가격은 WeaponDatabase.NewWeaponShopPrice(20골드)로 통일했다.
        ///
        /// ⚠️ 가격 근거(오케스트레이터 제안 150~250골드에서 의도적으로 낮춘 이유): 이 게임은 선형
        /// 구조(5개 지역, 되돌아갈 수 없음)이고 골드 수입원이 몬스터 처치 보상뿐이다. 무기고(지역 2)는
        /// 갈림길(지역 1) 이후 첫 방문 시 항상 골드 0인 상태로 도달하므로, 그 시점엔 구매가 애초에
        /// 불가능하다 — 유일한 실제 구매 기회는 고블린을 처치(골드+25)한 뒤 갈림길로 되돌아가
        /// 무기고를 다시 방문하는 경로뿐이다(맵 자체가 이 왕복을 허용함 — GetLocationChoices 참조).
        /// 즉 최종 보스전 전까지 현실적으로 모을 수 있는 최대 골드는 25이므로, 150~250골드는
        /// 이론상 절대 도달 불가능한 가격이 되어 "구매"라는 획득 경로 자체가 사실상 죽은 기능이
        /// 된다. 20골드로 낮춰 고블린 처치 보상만으로 실제로 살 수 있게 했다 — 그럼에도 회복
        /// 물약(30~50골드로 표시돼 있지만 실제로 소비되는 값은 아님)류보다 낮아 보일 수 있으나,
        /// 이 게임에서 유일하게 "실제로 골드를 소비하는" 아이템이므로 상대적으로는 가장 비싼
        /// 구매 대상이다.
        /// </summary>
        public PurchaseWeaponResult PurchaseNewWeapon()
        {
            if (CurrentState != GameState.PLAYING || Map.GetCurrentLocationIndex() != 2)
            {
                return PurchaseWeaponResult.NotAtArmory;
            }

            var weaponDef = WeaponDatabase.GetNewWeaponForClass(Player.ClassId);
            if (weaponDef == null)
            {
                return PurchaseWeaponResult.NoWeaponForClass;
            }
            if (Player.GetGold() < weaponDef.ShopPrice)
            {
                return PurchaseWeaponResult.NotEnoughGold;
            }
            if (Inventory.IsFull())
            {
                return PurchaseWeaponResult.InventoryFull;
            }

            Player.AddGold(-weaponDef.ShopPrice);
            Inventory.AddItem(weaponDef.CreateItem());
            return PurchaseWeaponResult.Success;
        }

        /// <summary>
        /// 신규(DEC-129): 무기고(지역 2)에서 방어구를 구매한다(가죽 갑옷·강화 판금 갑옷 둘 다 구매 대상 —
        /// 무기와 달리 직업 제한이 없다). 가격 근거는 PurchaseNewWeapon과 동일한 경제 제약을 따른다 —
        /// 가죽 갑옷(10골드)은 첫 고블린 처치 보상(25골드)만으로도 여유 있게 살 수 있고, 강화 판금
        /// 갑옷(20골드, 신규 무기와 동일 가격대)은 "고가"로 자리매김하되 여전히 실제로 도달 가능하다.
        /// </summary>
        public PurchaseArmorResult PurchaseArmor(string armorName)
        {
            if (CurrentState != GameState.PLAYING || Map.GetCurrentLocationIndex() != 2)
            {
                return PurchaseArmorResult.NotAtArmory;
            }

            var armorDef = ArmorDatabase.Get(armorName);
            if (armorDef == null)
            {
                return PurchaseArmorResult.UnknownArmor;
            }
            if (Player.GetGold() < armorDef.ShopPrice)
            {
                return PurchaseArmorResult.NotEnoughGold;
            }
            if (Inventory.IsFull())
            {
                return PurchaseArmorResult.InventoryFull;
            }

            Player.AddGold(-armorDef.ShopPrice);
            Inventory.AddItem(armorDef.CreateItem());
            return PurchaseArmorResult.Success;
        }

        /// <summary>
        /// 신규(DEC-129): 탐색 중 "상자를 조사한다"(갈림길, 지역 1 한정, 이번 회차 전체 1회). 30~40%
        /// 범위 내 35% 확률로 성공하고, 성공하면 다시 세 갈래로 나뉜다(각각 약 1/3 확률) —
        /// ① 직업 전용 신규 무기, ② 가죽 갑옷, ③ 소량의 골드(15)+마나 결정 1개. 실패(65%)하면
        /// 아무것도 얻지 못한다. 인벤토리가 가득 차 무기/방어구를 담지 못하면 조용히 실패하고
        /// (기존 드롭 아이템들과 동일한 패턴), 어느 쪽이든 시도 자체는 이번 회차에서 한 번만 가능하다
        /// (CanInvestigateChestHere 참조 — DEC-123 review에서 발견된 "세션 재사용 시 초기화 안 되는
        /// 버그"와 동일한 실수를 반복하지 않도록 ConfirmClass/ResumeFromLoadedState에서 매번 리셋한다).
        /// </summary>
        public ChestResult InvestigateChest()
        {
            if (!CanInvestigateChestHere())
            {
                return ChestResult.NotAvailable;
            }

            chestAttempted = true;

            if (Utils.GenerateRandomNumber(1, 100) > ChestSuccessChancePercent)
            {
                return ChestResult.FoundNothing;
            }

            int outcomeRoll = Utils.GenerateRandomNumber(1, 3);
            if (outcomeRoll == 1)
            {
                var weaponDef = WeaponDatabase.GetNewWeaponForClass(Player.ClassId);
                if (weaponDef != null && Inventory.AddItem(weaponDef.CreateItem()))
                {
                    return ChestResult.FoundWeapon;
                }
            }
            else if (outcomeRoll == 2)
            {
                if (Inventory.AddItem(ArmorDatabase.Get(ArmorDatabase.LeatherArmor).CreateItem()))
                {
                    return ChestResult.FoundArmor;
                }
            }

            // outcomeRoll == 3이거나, 위 두 분기가 인벤토리 만재로 실패했으면 골드+재료로 대체한다.
            Player.AddGold(ChestGoldReward);
            Inventory.AddItem(new Item(CharacterClassDatabase.ManaRecoveryMaterialName, ItemType.CONSUMABLE, 1, 20,
                "전투 중 '마나 회복' 행동에 사용하는 소모 재료입니다. 사용 시 최대 마나의 45%를 회복합니다."));
            return ChestResult.FoundGoldAndMaterial;
        }
    }
}
