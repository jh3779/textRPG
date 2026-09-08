/*
 * RegressionSmokeTest.cs
 *
 * 📝 역할: 06_open_questions.md 리스크 항목("C++ → C# 포팅 시 수치·확률·분기가 미세하게
 * 달라질 위험... 포팅 후 콘솔 버전과 같은 시드/입력으로 결과를 비교하는 회귀 테스트를
 * 구현 단계에서 별도로 마련")에 대응하는 1차 스모크 테스트.
 *
 * 이번 작업 범위에서는 정식 Unity Test Framework(PlayMode/EditMode 테스트 어셈블리) 대신
 * 배치 모드에서 -executeMethod로 바로 실행 가능한 간단한 스크립트로 작성했다 — 정식
 * 테스트 스위트로 승격하는 것은 다음 작업으로 남겨둔다(최종 보고 참조).
 *
 * 실행: Unity -batchmode -projectPath unity -executeMethod TextRPG.EditorTools.RegressionSmokeTest.RunAll -quit
 * 모든 항목을 통과하면 "[RegressionSmokeTest] ALL PASSED"를 로그에 남기고,
 * 하나라도 실패하면 예외를 던져 배치 실행이 0이 아닌 종료 코드로 끝나게 한다.
 */

using System;
using System.IO;
using TextRPG.GameLogic;
using TextRPG.Persistence;
using UnityEngine;

namespace TextRPG.EditorTools
{
    public static class RegressionSmokeTest
    {
        public static void RunAll()
        {
            int passed = 0;
            passed += Check("Player 기본 생성자 스탯", TestPlayerDefaults);
            passed += Check("Player.TakeDamage 공식(max(1, dmg-def))", TestPlayerTakeDamage);
            passed += Check("Player.LevelUp 증가량", TestPlayerLevelUp);
            passed += Check("Player.AddExperience 레벨업 임계값(level*100)", TestAddExperienceThreshold);
            passed += Check("Enemy.TakeDamage 공식", TestEnemyTakeDamage);
            passed += Check("Quest 상태 전이 NOT_STARTED→IN_PROGRESS→COMPLETED", TestQuestTransitions);
            passed += Check("Map 5개 고정 지역(DEC-101)", TestMapLocations);
            passed += Check("Inventory 용량 5, 초과 시 실패", TestInventoryCapacity);
            passed += Check("BattleSystem 공격 데미지 범위(ATK+[0,3])", TestBattlePlayerAttackRange);
            passed += Check("BattleSystem 반격 데미지 범위(ATK+[0,2])", TestBattleEnemyAttackRange);
            passed += Check("BattleSystem 도망 성공률 55%(통계적 근사)", TestFleeRateApprox55Percent);
            passed += Check("BattleSystem 전체 전투 시뮬레이션(고블린전)", TestFullGoblinBattleSimulation);
            passed += Check("CharacterClassDatabase 3직업 수치(와이어프레임 S-002 기준)", TestCharacterClassStats);
            passed += Check("GameSession 새 게임→직업 확정→전투→승리 전체 플로우", TestGameSessionFullPlaythroughToVictoryPossible);
            passed += Check("OQ-102/DEC-122: SaveSystem.SaveExists()가 새 게임 덮어쓰기 확인 모달 표시 조건과 일치", TestSaveExistsDetection);

            Debug.Log($"[RegressionSmokeTest] ALL PASSED ({passed} checks)");
        }

        private static int Check(string name, Action test)
        {
            test();
            Debug.Log($"[RegressionSmokeTest] PASS: {name}");
            return 1;
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception($"FAIL: {message}");
            }
        }

        private static void TestPlayerDefaults()
        {
            var p = new Player("모험가");
            Assert(p.GetHp() == 100 && p.GetMaxHp() == 100, "기본 HP는 100/100이어야 함(src/Player.cpp)");
            Assert(p.GetAttack() == 10, "기본 공격력은 10이어야 함");
            Assert(p.GetDefense() == 3, "기본 방어력은 3이어야 함");
            Assert(p.GetLevel() == 1 && p.GetExperience() == 0 && p.GetGold() == 0, "레벨1/경험치0/골드0으로 시작해야 함");
        }

        private static void TestPlayerTakeDamage()
        {
            var p = new Player("테스트");
            int dealt = p.TakeDamage(10); // defense=3 -> max(1,10-3)=7
            Assert(dealt == 7, $"10 피해 - 방어3 = 7이어야 하는데 {dealt}");
            Assert(p.GetHp() == 93, $"HP가 100->93이어야 하는데 {p.GetHp()}");

            var p2 = new Player("테스트2");
            int dealt2 = p2.TakeDamage(1); // max(1, 1-3) = max(1,-2) = 1 (최소 1 피해 보장)
            Assert(dealt2 == 1, $"방어력보다 낮은 피해도 최소 1은 들어가야 하는데 {dealt2}");
        }

        private static void TestPlayerLevelUp()
        {
            var p = new Player("테스트");
            p.SetHp(1);
            p.LevelUp();
            Assert(p.GetLevel() == 2, "레벨이 2가 되어야 함");
            Assert(p.GetMaxHp() == 120, $"최대HP가 100+20=120이어야 하는데 {p.GetMaxHp()}");
            Assert(p.GetAttack() == 13, $"공격력이 10+3=13이어야 하는데 {p.GetAttack()}");
            Assert(p.GetDefense() == 4, $"방어력이 3+1=4여야 하는데 {p.GetDefense()}");
            Assert(p.GetHp() == p.GetMaxHp(), "레벨업 시 HP가 전부 회복되어야 함");
        }

        private static void TestAddExperienceThreshold()
        {
            var p = new Player("테스트");
            p.AddExperience(99); // level 1 * 100 = 100 미만 -> 레벨업 안함
            Assert(p.GetLevel() == 1 && p.GetExperience() == 99, "99 경험치로는 레벨업하면 안 됨");

            p.AddExperience(1); // 100 도달 -> 레벨업, 남은 경험치 0
            Assert(p.GetLevel() == 2 && p.GetExperience() == 0, $"100 경험치에서 레벨업, 잔여 0이어야 하는데 lvl={p.GetLevel()} exp={p.GetExperience()}");

            // 레벨2 임계값은 200. 250 추가 시: 250-200=50 남고 레벨3
            p.AddExperience(250);
            Assert(p.GetLevel() == 3 && p.GetExperience() == 50, $"레벨3, 잔여50이어야 하는데 lvl={p.GetLevel()} exp={p.GetExperience()}");
        }

        private static void TestEnemyTakeDamage()
        {
            var e = new Enemy("고블린", 30, 7, 1, 60, 25);
            int dealt = e.TakeDamage(5); // max(1, 5-1) = 4
            Assert(dealt == 4, $"5 피해 - 방어1 = 4여야 하는데 {dealt}");
            Assert(e.GetHp() == 26, $"HP 30->26이어야 하는데 {e.GetHp()}");
        }

        private static void TestQuestTransitions()
        {
            var q = new Quest("quest001", "던전 탈출", "설명", QuestType.REACH_GOAL, 1, 100, 80);
            Assert(q.GetStatus() == QuestStatus.NOT_STARTED, "초기 상태는 NOT_STARTED");
            q.StartQuest();
            Assert(q.GetStatus() == QuestStatus.IN_PROGRESS, "StartQuest 후 IN_PROGRESS");
            q.UpdateProgress();
            Assert(q.GetStatus() == QuestStatus.COMPLETED, "목표(1) 도달 시 COMPLETED");
            Assert(q.IsCompleted(), "IsCompleted()는 COMPLETED에서도 true");
        }

        private static void TestMapLocations()
        {
            var map = new Map();
            Assert(map.GetTotalLocations() == 5, "DEC-101: 지역은 정확히 5개");
            string[] expectedNames = { "던전 입구", "갈림길", "낡은 무기고", "어두운 통로", "보스의 방" };
            for (int i = 0; i < 5; i++)
            {
                Assert(map.GetLocationAt(i).Name == expectedNames[i], $"지역 {i} 이름이 {expectedNames[i]}여야 함");
            }
            Assert(map.GetLocationAt(3).HasEnemy && !map.GetLocationAt(3).IsDeadEnd, "어두운 통로는 적 있음/막다른길 아님");
            Assert(map.GetLocationAt(4).HasEnemy && map.GetLocationAt(4).IsDeadEnd, "보스의 방은 적 있음/막다른길");
        }

        private static void TestInventoryCapacity()
        {
            var inv = new Inventory(5);
            for (int i = 0; i < 5; i++)
            {
                Assert(inv.AddItem(new Item($"아이템{i}", ItemType.POTION, 10, 10, "설명")), "5개까지는 추가 성공해야 함");
            }
            Assert(!inv.AddItem(new Item("여섯번째", ItemType.POTION, 10, 10, "설명")), "6번째부터는 실패해야 함(용량 5)");
            Assert(inv.IsFull(), "가득 찼어야 함");
        }

        private static void TestBattlePlayerAttackRange()
        {
            var p = new Player("테스트");
            p.SetAttack(10);
            for (int i = 0; i < 500; i++)
            {
                var enemy = new Enemy("더미", 9999, 0, 0, 0, 0);
                var battle = new BattleSystem(p, enemy);
                int dealt = battle.PlayerAttack();
                Assert(dealt >= 10 && dealt <= 13, $"플레이어 공격 데미지는 10~13 범위여야 하는데 {dealt}");
            }
        }

        private static void TestBattleEnemyAttackRange()
        {
            // 고블린 ATK7 + Random(0,2) = 원시 피해 7~9. Player 기본 방어력은 3이므로
            // takeDamage() 공식 max(1, dmg-def) 적용 후 최종 피해는 4~6이어야 한다.
            for (int i = 0; i < 500; i++)
            {
                var p = new Player("테스트"); // defense = 3 (기본 생성자)
                var enemy = new Enemy("고블린", 30, 7, 1, 60, 25);
                var battle = new BattleSystem(p, enemy);
                int hpBefore = p.GetHp();
                int dealt = battle.EnemyAttack();
                Assert(dealt >= 4 && dealt <= 6, $"방어력 3 적용 후 최종 피해는 4~6 범위여야 하는데 {dealt}");
                Assert(p.GetHp() == hpBefore - dealt, "플레이어 HP가 정확히 dealt만큼 줄어야 함");
            }
        }

        private static void TestFleeRateApprox55Percent()
        {
            var p = new Player("테스트");
            var enemy = new Enemy("더미", 9999, 0, 0, 0, 0);
            var battle = new BattleSystem(p, enemy);
            int successCount = 0;
            const int trials = 20000;
            for (int i = 0; i < trials; i++)
            {
                if (battle.AttemptFlee()) successCount++;
            }
            double rate = successCount / (double)trials;
            Assert(rate > 0.52 && rate < 0.58, $"도망 성공률은 55% 근방이어야 하는데 {rate:P1} (n={trials})");
        }

        private static void TestFullGoblinBattleSimulation()
        {
            var p = new Player("모험가");
            var enemy = new Enemy("고블린", 30, 7, 1, 60, 25);
            var battle = new BattleSystem(p, enemy);

            BattleResult? result = null;
            int guardRounds = 0;
            while (result == null && guardRounds < 1000)
            {
                result = battle.TakeTurn(1); // 항상 공격
                guardRounds++;
            }

            Assert(result != null, "1000라운드 안에 전투가 끝나야 함(무한루프 아님)");
            Assert(result == BattleResult.PLAYER_WIN || result == BattleResult.PLAYER_LOSE,
                $"항상 공격만 하면 WIN 또는 LOSE로 끝나야 하는데 {result}");

            if (result == BattleResult.PLAYER_WIN)
            {
                Assert(p.GetExperience() > 0 || p.GetLevel() > 1, "승리 시 경험치가 지급되어야 함");
                Assert(p.GetGold() == 25, $"승리 시 골드 25를 획득해야 하는데 {p.GetGold()}");
            }
        }

        private static void TestCharacterClassStats()
        {
            var warrior = CharacterClassDatabase.Get("warrior");
            Assert(warrior.BaseHp == 110 && warrior.BaseAttack == 12 && warrior.BaseDefense == 5,
                "전사 HP110/ATK12/DEF5 (wireframes.html S-002 기준)");

            var rogue = CharacterClassDatabase.Get("rogue");
            Assert(rogue.BaseHp == 90 && rogue.BaseAttack == 14 && rogue.BaseDefense == 2,
                "도적 HP90/ATK14/DEF2");
            Assert(rogue.CreateStartingItems().Count == 2, "도적은 단검 2자루로 시작해야 함");

            var mage = CharacterClassDatabase.Get("mage");
            Assert(mage.BaseHp == 75 && mage.BaseAttack == 17 && mage.BaseDefense == 1,
                "마법사 HP75/ATK17/DEF1");
        }

        /// <summary>
        /// OQ-102(DEC-122): TitlePanelController.OnNewGameClicked()가 덮어쓰기 확인 모달을
        /// 띄울지 판단하는 근거가 정확히 SaveSystem.SaveExists()이므로, 그 감지 자체가
        /// 파일 존재/부재에 정확히 반응하는지 회귀 확인한다(MonoBehaviour 버튼 클릭 배선
        /// 자체는 Play Mode 없이는 검증 불가 — 최종 보고서 참조).
        /// 실제 로컬 세이브가 있어도 안전하도록 백업/복원한다.
        /// </summary>
        private static void TestSaveExistsDetection()
        {
            string path = SaveSystem.GetSavePath();
            bool hadExistingFile = File.Exists(path);
            string backup = hadExistingFile ? File.ReadAllText(path) : null;
            try
            {
                if (hadExistingFile)
                {
                    File.Delete(path);
                }
                Assert(!SaveSystem.SaveExists(), "세이브 파일이 없으면 SaveExists()==false여야 함(모달을 띄우지 않는 조건)");

                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, "{}");
                Assert(SaveSystem.SaveExists(), "세이브 파일이 있으면 SaveExists()==true여야 함(모달을 띄우는 조건)");
            }
            finally
            {
                if (hadExistingFile)
                {
                    File.WriteAllText(path, backup);
                }
                else if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        private static void TestGameSessionFullPlaythroughToVictoryPossible()
        {
            var session = new GameSession();
            session.BeginNewGameFlow();
            Assert(session.CurrentState == GameState.CLASS_SELECT, "새 게임 시작 시 CLASS_SELECT여야 함(STATE-101)");

            session.SelectPendingClass("warrior");
            Assert(session.ConfirmClass(), "직업 확정이 성공해야 함");
            Assert(session.CurrentState == GameState.PLAYING, "확정 후 PLAYING이어야 함");
            Assert(session.Player.GetMaxHp() == 110, "전사로 시작했으니 HP110이어야 함");
            Assert(session.Inventory.GetItemCount() == 2, "전사 시작 아이템(장검) + 기본 회복물약 = 2개");

            // 던전 입구 -> 갈림길 -> 어두운 통로(자동 전투)
            session.ChooseLocationAction(1); // 던전에 들어간다 -> 갈림길
            Assert(session.Map.GetCurrentLocationIndex() == 1, "갈림길로 이동해야 함");

            session.ChooseLocationAction(2); // 오른쪽 통로 -> 어두운 통로, 즉시 전투 진입
            Assert(session.CurrentState == GameState.BATTLE, "고블린과 자동으로 전투가 시작되어야 함");

            BattleResult? result = null;
            int guard = 0;
            while (result == null && guard < 1000)
            {
                result = session.ProcessBattleTurn(1); // 항상 공격
                guard++;
            }
            Assert(result != null, "전투가 끝나야 함");

            if (result == BattleResult.PLAYER_WIN)
            {
                Assert(session.GoblinDefeated, "고블린을 처치했으면 GoblinDefeated=true");
                Assert(session.Map.GetCurrentLocationIndex() == 4, "승리 후 보스의 방으로 자동 이동해야 함");
                Assert(session.CurrentState == GameState.PLAYING, "전투 승리 후 다시 PLAYING이어야 함");
            }
            else
            {
                Assert(session.CurrentState == GameState.GAME_OVER, "패배 시 GAME_OVER여야 함");
            }
        }
    }
}
