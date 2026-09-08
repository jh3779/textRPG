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
                            Player.SetAttack(Player.GetAttack() + 4);
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
                    Map.MoveToLocation(4);
                    CurrentState = GameState.PLAYING;
                }
            }
            else if (idx == 4)
            {
                if (result == BattleResult.PLAYER_WIN)
                {
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
    }
}
