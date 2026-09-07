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

        private void StartBattleWithGoblin()
        {
            // src/Game.cpp: Enemy goblin("고블린", 30, 7, 1, 60, 25);
            CurrentEnemy = new Enemy("고블린", 30, 7, 1, 60, 25)
            {
                PortraitVariants = new[]
                {
                    "enemy_고블린_약소형.png", "enemy_고블린_날렵형.png",
                    "enemy_고블린_거대형.png", "enemy_고블린_주술사형.png"
                }
            };
            CurrentBattle = new BattleSystem(Player, CurrentEnemy);
            CurrentState = GameState.BATTLE;
        }

        private void StartBattleWithGuardian()
        {
            // src/Game.cpp: Enemy guardian("던전 수호자", 55, 10, 3, 120, 70);
            CurrentEnemy = new Enemy("던전 수호자", 55, 10, 3, 120, 70)
            {
                PortraitVariants = new[] { "enemy_던전수호자.png" } // DEC-114: 균형형 고정
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
    }
}
