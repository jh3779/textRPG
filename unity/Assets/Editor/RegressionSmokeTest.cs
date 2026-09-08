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
            passed += Check("CharacterClassDatabase 3직업 최종 수치(DEC-123: 기본스탯+무기보너스 반영)", TestCharacterClassStats);
            passed += Check("GameSession 새 게임→직업 확정→전투→승리 전체 플로우", TestGameSessionFullPlaythroughToVictoryPossible);
            passed += Check("OQ-102/DEC-122: SaveSystem.SaveExists()가 새 게임 덮어쓰기 확인 모달 표시 조건과 일치", TestSaveExistsDetection);

            // 신규(DEC-123): 공격속도 선공 결정 · 도적 2타 · 마나 스킬 3종 · 마나 회복 경제(아이템/휴식/재료 스킬)
            passed += Check("DEC-123: 공격속도가 더 높은 적이 선공해야 함", TestAttackSpeedDeterminesTurnOrder);
            passed += Check("DEC-123: 공격속도가 같거나 더 높으면 플레이어가 선공해야 함(동률 시 플레이어 우선)", TestPlayerActsFirstWhenFasterOrTied);
            passed += Check("DEC-123: 도적 쌍검 패시브 — 기본 공격이 2회 독립 타격해야 함", TestRogueDoubleAttack);
            passed += Check("DEC-123: 마나가 스킬 비용보다 적으면 CanUseManaSkill()이 false여야 함", TestManaSkillAffordability);
            passed += Check("DEC-123: 전사 강타 — 피해량이 1.5배로 계산되어야 함", TestWarriorPowerStrikeDamage);
            passed += Check("DEC-123: 마법사 화염구 — 방어력을 절반만 적용해야 함", TestMageFireballHalvesDefense);
            passed += Check("DEC-123: 도적 맹독 일격 — 즉시타격 + 3턴 도트(틱당 고정 피해)", TestRoguePoisonStrikeAppliesDotOverThreeTurns);
            passed += Check("DEC-123 수정: 맹독 일격 3틱 합계가 정확히 totalDot(최대체력10%)과 같아야 함", TestPoisonStrikeThreeTickTotalMatchesTenPercent);
            passed += Check("DEC-123: 마나는 새 전투 시작 시 자동으로 풀회복되지 않아야 함(전투 간 이월)", TestManaDoesNotAutoRestoreBetweenBattles);
            passed += Check("DEC-123: GameSession.UseItem — 이름에 '마나' 포함 여부로 HP/마나 회복 분기", TestUseItemRestoresHpOrManaBasedOnName);
            passed += Check("DEC-123: GameSession.Rest — 지역당 1회만 마나 회복", TestRestRestoresManaOncePerLocation);
            passed += Check("DEC-123 수정: 같은 GameSession 인스턴스로 '새 게임'을 다시 시작하면 휴식 제한이 초기화되어야 함", TestRestLimitResetsWhenSameSessionStartsNewGame);
            passed += Check("DEC-123: 전투 중 마나 회복 스킬은 재료('마나 결정')를 소모해야 함", TestManaRecoverySkillConsumesMaterialInBattle);

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
            // DEC-123: 기본 스탯 변경(전사 ATK12→10, 도적 ATK14→16) + 무기 보너스(장검/단검2자루/지팡이)가
            // 반영된 "최종" 수치. CharacterClassDatabase.cs 주석의 계산식과 정확히 일치해야 한다.
            var warrior = CharacterClassDatabase.Get("warrior");
            Assert(warrior.BaseHp == 114 && warrior.BaseAttack == 13 && warrior.BaseDefense == 5,
                $"전사 HP114/ATK13/DEF5(DEC-123)여야 하는데 HP{warrior.BaseHp}/ATK{warrior.BaseAttack}/DEF{warrior.BaseDefense}");
            Assert(warrior.BaseMana == 10 && warrior.AttackSpeed == -1 && !warrior.HasDoubleAttack,
                $"전사 Mana10/공격속도-1/쌍검없음(DEC-123)이어야 하는데 Mana{warrior.BaseMana}/AtkSpd{warrior.AttackSpeed}/DoubleAttack{warrior.HasDoubleAttack}");
            Assert(warrior.ManaSkill == ManaSkillType.PowerStrike && warrior.ManaSkillCost == 5,
                "전사 마나 스킬은 강타(비용5)여야 함");

            var rogue = CharacterClassDatabase.Get("rogue");
            Assert(rogue.BaseHp == 85 && rogue.BaseAttack == 16 && rogue.BaseDefense == 2,
                $"도적 HP85/ATK16/DEF2(DEC-123)여야 하는데 HP{rogue.BaseHp}/ATK{rogue.BaseAttack}/DEF{rogue.BaseDefense}");
            Assert(rogue.BaseMana == 15 && rogue.AttackSpeed == 3 && rogue.HasDoubleAttack,
                $"도적 Mana15/공격속도+3/쌍검있음(DEC-123)이어야 하는데 Mana{rogue.BaseMana}/AtkSpd{rogue.AttackSpeed}/DoubleAttack{rogue.HasDoubleAttack}");
            Assert(rogue.ManaSkill == ManaSkillType.PoisonStrike && rogue.ManaSkillCost == 7,
                "도적 마나 스킬은 맹독 일격(비용7)이어야 함");
            Assert(rogue.CreateStartingItems().Count == 2, "도적은 단검 2자루로 시작해야 함");

            var mage = CharacterClassDatabase.Get("mage");
            Assert(mage.BaseHp == 75 && mage.BaseAttack == 19 && mage.BaseDefense == 1,
                $"마법사 HP75/ATK19/DEF1(DEC-123)이어야 하는데 HP{mage.BaseHp}/ATK{mage.BaseAttack}/DEF{mage.BaseDefense}");
            Assert(mage.BaseMana == 32 && mage.AttackSpeed == 1 && !mage.HasDoubleAttack,
                $"마법사 Mana32/공격속도+1/쌍검없음(DEC-123)이어야 하는데 Mana{mage.BaseMana}/AtkSpd{mage.AttackSpeed}/DoubleAttack{mage.HasDoubleAttack}");
            Assert(mage.ManaSkill == ManaSkillType.Fireball && mage.ManaSkillCost == 10,
                "마법사 마나 스킬은 화염구(비용10)여야 함");
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
            Assert(session.Player.GetMaxHp() == 114, $"전사로 시작했으니 HP114(DEC-123)여야 하는데 {session.Player.GetMaxHp()}");
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

        // ───────────────────────── 신규(DEC-123) 회귀 테스트 ─────────────────────────

        private static void TestAttackSpeedDeterminesTurnOrder()
        {
            // 적 공격속도(5)가 플레이어 기본 공격속도(0)보다 높으면 적이 선공해야 한다.
            var player = new Player("테스트");
            player.SetHp(1); // 한 대만 맞아도 죽도록 만들어 "누가 먼저 때렸는지"를 명확히 구분한다.
            var enemy = new Enemy("빠른 적", 9999, 100, 0, 0, 0) { AttackSpeed = 5 };
            var battle = new BattleSystem(player, enemy);

            var result = battle.TakeTurn(BattleSystem.ActionAttack);

            Assert(result == BattleResult.PLAYER_LOSE, "적 공격속도가 더 높으면 적이 선공해 플레이어가 즉시 패배해야 함");
            Assert(enemy.GetHp() == enemy.GetMaxHp(), "적이 선공해 플레이어가 죽었다면 플레이어는 공격 기회가 없어 적 HP는 그대로여야 함");
        }

        private static void TestPlayerActsFirstWhenFasterOrTied()
        {
            // 공격속도가 동률(둘 다 기본값 0)이면 플레이어가 먼저 행동해야 한다(기존 동작과의 하위호환).
            var enemy = new Enemy("느린 적", 9999, 100, 0, 0, 0) { AttackSpeed = 0 };
            var player = new Player("테스트");
            player.SetAttack(10);
            var battle = new BattleSystem(player, enemy);

            int enemyHpBefore = enemy.GetHp();
            battle.TakeTurn(BattleSystem.ActionAttack);

            Assert(enemy.GetHp() < enemyHpBefore, "동률이면 플레이어가 먼저 공격해 적 HP가 줄어야 함");
            Assert(player.GetHp() < player.GetMaxHp(), "적이 죽지 않았다면 같은 라운드에 적도 반격해야 함");
        }

        private static void TestRogueDoubleAttack()
        {
            var rogue = new Player("테스트", CharacterClassDatabase.Get("rogue")); // ATK16, 쌍검 패시브
            Assert(rogue.HasDoubleAttack, "도적은 쌍검 패시브로 기본 공격이 2회 타격해야 함");

            var enemy = new Enemy("더미", 9999, 0, 0, 0, 0); // 방어력 0, 반격은 무해
            var battle = new BattleSystem(rogue, enemy);

            int hpBefore = enemy.GetHp();
            battle.TakeTurn(BattleSystem.ActionAttack);
            int totalDamage = hpBefore - enemy.GetHp();

            // ATK16 + Random(0,3) 두 번 독립 적용 => 최소 16*2=32, 최대 (16+3)*2=38
            Assert(totalDamage >= 32 && totalDamage <= 38, $"쌍검 2회 타격 합산 데미지가 32~38 범위여야 하는데 {totalDamage}");
        }

        private static void TestManaSkillAffordability()
        {
            var mage = new Player("테스트", CharacterClassDatabase.Get("mage")); // Mana32, 화염구 비용10
            var enemy = new Enemy("더미", 9999, 0, 0, 0, 0);
            var battle = new BattleSystem(mage, enemy);

            Assert(battle.CanUseManaSkill(), "마나가 충분하면 스킬 사용 가능해야 함");

            mage.SpendMana(30); // 남은 마나 2 < 비용 10
            Assert(!battle.CanUseManaSkill(), "마나가 비용보다 적으면 스킬 사용 불가해야 함");
        }

        private static void TestWarriorPowerStrikeDamage()
        {
            var warrior = new Player("테스트", CharacterClassDatabase.Get("warrior")); // ATK13, Mana10, 강타 비용5
            var enemy = new Enemy("더미", 9999, 0, 0, 0, 0); // 방어력 0
            var battle = new BattleSystem(warrior, enemy);

            int hpBefore = enemy.GetHp();
            int manaBefore = warrior.GetMana();
            battle.TakeTurn(BattleSystem.ActionManaSkill);
            int damage = hpBefore - enemy.GetHp();

            Assert(warrior.GetMana() == manaBefore - 5, "강타는 마나 5를 소모해야 함");
            // rawDamage=13~16, boosted=Math.Round(raw*1.5) => 20/21/22/24 중 하나
            Assert(damage >= 19 && damage <= 24, $"강타 피해량이 예상 범위(19~24)를 벗어남: {damage}");
        }

        private static void TestMageFireballHalvesDefense()
        {
            var mage = new Player("테스트", CharacterClassDatabase.Get("mage")); // ATK19, Mana32, 화염구 비용10
            var enemyHalved = new Enemy("더미", 9999, 0, 20, 0, 0); // 방어력20(짝수 - 절반이 정확히 10)
            var battle = new BattleSystem(mage, enemyHalved);

            int hpBefore = enemyHalved.GetHp();
            battle.TakeTurn(BattleSystem.ActionManaSkill);
            int damage = hpBefore - enemyHalved.GetHp();

            // rawDamage=19~22, 방어력 절반(10) 적용 => 9~12
            Assert(damage >= 9 && damage <= 12, $"화염구 피해량이 예상 범위(9~12)를 벗어남: {damage}");

            // 비교군: 같은 방어력에 일반 공격이면 방어력 전체(20)가 적용되어 최소 피해(1~2)만 들어가야 함
            var enemyNormal = new Enemy("더미2", 9999, 0, 20, 0, 0);
            var battleNormal = new BattleSystem(mage, enemyNormal);
            int hpBefore2 = enemyNormal.GetHp();
            battleNormal.TakeTurn(BattleSystem.ActionAttack);
            int normalDamage = hpBefore2 - enemyNormal.GetHp();
            Assert(normalDamage >= 1 && normalDamage <= 2,
                $"일반 공격은 방어력을 그대로 적용해 최소 피해(1~2)만 들어가야 하는데 {normalDamage}");
        }

        private static void TestRoguePoisonStrikeAppliesDotOverThreeTurns()
        {
            var rogue = new Player("테스트", CharacterClassDatabase.Get("rogue")); // Mana15, 맹독일격 비용7
            var enemy = new Enemy("더미", 100, 0, 0, 0, 0); // maxHp100, 방어력0, ATK0(반격 무해)
            var battle = new BattleSystem(rogue, enemy);

            // totalDot = max(1, maxHp/10) = 10. 3틱 배분(나머지를 앞 틱부터 몰아줌) = [4,3,3], 합계 정확히 10
            // (2026-09-08 수정: 이전에는 totalDot/3을 세 번 적용해 9(9%)로 총량이 깎이는 문제가 있었음).
            battle.TakeTurn(BattleSystem.ActionManaSkill);
            int round1Damage = 100 - enemy.GetHp();
            // 1라운드: 즉시타격 1회(맹독 일격은 쌍검 패시브 미적용, ATK16~19) + 도트 1번째 틱(4)
            Assert(round1Damage >= 16 + 4 && round1Damage <= 19 + 4,
                $"1라운드 피해(즉시타격+도트1틱)가 예상 범위(20~23)를 벗어남: {round1Damage}");

            // 이후에는 공격 없는 행동(마나 회복)만 반복해 도트만 순수하게 측정한다.
            int hpBeforeRound2 = enemy.GetHp();
            battle.TakeTurn(BattleSystem.ActionRecoverMana);
            Assert(hpBeforeRound2 - enemy.GetHp() == 3, "2번째 도트 틱은 정확히 3이어야 함");

            int hpBeforeRound3 = enemy.GetHp();
            battle.TakeTurn(BattleSystem.ActionRecoverMana);
            Assert(hpBeforeRound3 - enemy.GetHp() == 3, "3번째(마지막) 도트 틱은 정확히 3이어야 함");

            int hpBeforeRound4 = enemy.GetHp();
            battle.TakeTurn(BattleSystem.ActionRecoverMana);
            Assert(hpBeforeRound4 - enemy.GetHp() == 0, "3턴이 지나면 도트가 더 이상 적용되지 않아야 함");
        }

        private static void TestPoisonStrikeThreeTickTotalMatchesTenPercent()
        {
            // maxHp=100(10/3 나누어떨어지지 않음, 배분 로직의 핵심 검증 대상)과 maxHp=55(보스 HP, 5/3도 나누어
            // 떨어지지 않음) 둘 다에서 3틱 각각의 정확한 값과 총합이 totalDot(최대체력의 10%)과 같은지 확인한다.
            foreach (int maxHp in new[] { 100, 55 })
            {
                var rogue = new Player("테스트", CharacterClassDatabase.Get("rogue"));
                var enemy = new Enemy("더미", maxHp, 0, 0, 0, 0);
                var battle = new BattleSystem(rogue, enemy);

                int totalDot = Math.Max(1, maxHp / 10);
                int basePerTick = totalDot / 3;
                int remainder = totalDot % 3;
                int expectedTick1 = basePerTick + (remainder >= 1 ? 1 : 0);
                int expectedTick2 = basePerTick + (remainder >= 2 ? 1 : 0);
                int expectedTick3 = basePerTick;
                Assert(expectedTick1 + expectedTick2 + expectedTick3 == totalDot,
                    $"maxHp={maxHp}: 배분 공식 자체가 totalDot({totalDot})과 일치해야 함(테스트 사전 검산)");

                // 1라운드: 즉시타격(랜덤 16~19) + 1번째 틱(고정값 expectedTick1) — 즉시타격 성분을 역산해 범위 확인.
                battle.TakeTurn(BattleSystem.ActionManaSkill);
                int round1Damage = maxHp - enemy.GetHp();
                int immediateHitComponent = round1Damage - expectedTick1;
                Assert(immediateHitComponent >= 16 && immediateHitComponent <= 19,
                    $"maxHp={maxHp}: 즉시타격 성분이 16~19 범위를 벗어남(1틱={expectedTick1} 가정 시 역산값 {immediateHitComponent})");

                int hpBeforeTick2 = enemy.GetHp();
                battle.TakeTurn(BattleSystem.ActionRecoverMana);
                Assert(hpBeforeTick2 - enemy.GetHp() == expectedTick2, $"maxHp={maxHp}: 2번째 틱은 {expectedTick2}여야 함");

                int hpBeforeTick3 = enemy.GetHp();
                battle.TakeTurn(BattleSystem.ActionRecoverMana);
                Assert(hpBeforeTick3 - enemy.GetHp() == expectedTick3, $"maxHp={maxHp}: 3번째 틱은 {expectedTick3}이어야 함");
            }
        }

        private static void TestManaDoesNotAutoRestoreBetweenBattles()
        {
            var session = new GameSession();
            session.BeginNewGameFlow();
            session.SelectPendingClass("mage");
            session.ConfirmClass();

            session.Player.SpendMana(20); // 32 -> 12
            int manaBeforeNewBattle = session.Player.GetMana();

            // 새 BattleSystem(=새 전투)을 만들어도 마나가 자동으로 풀회복되면 안 된다(DEC-123 수정 — 이월).
            var enemy = new Enemy("더미", 10, 0, 0, 0, 0);
            var _ = new BattleSystem(session.Player, enemy);

            Assert(session.Player.GetMana() == manaBeforeNewBattle,
                $"새 전투를 시작해도 마나가 자동 회복되면 안 되는데 {session.Player.GetMana()}(기대값 {manaBeforeNewBattle})");
        }

        private static int FindItemIndexByName(GameSession session, string name)
        {
            for (int i = 0; i < session.Inventory.GetItemCount(); i++)
            {
                if (session.Inventory.GetItem(i).GetName() == name)
                {
                    return i;
                }
            }
            return -1;
        }

        private static void TestUseItemRestoresHpOrManaBasedOnName()
        {
            var session = new GameSession();
            session.BeginNewGameFlow();
            session.SelectPendingClass("mage");
            session.ConfirmClass();

            int weaponIndex = FindItemIndexByName(session, "지팡이");
            Assert(weaponIndex >= 0, "시작 무기(지팡이)가 인벤토리에 있어야 함");
            var notUsable = session.UseItem(weaponIndex);
            Assert(notUsable == ItemUseResult.NotUsable, "POTION이 아닌 아이템은 사용할 수 없어야 함(NotUsable)");

            int hpPotionIndex = FindItemIndexByName(session, "회복 물약");
            Assert(hpPotionIndex >= 0, "기본 회복 물약이 시작 인벤토리에 있어야 함");
            session.Player.SetHp(10);
            int countBefore = session.Inventory.GetItemCount();
            var hpResult = session.UseItem(hpPotionIndex);
            Assert(hpResult == ItemUseResult.Success, "회복 물약 사용은 성공해야 함");
            Assert(session.Player.GetHp() == 40, $"체력 10+30=40이어야 하는데 {session.Player.GetHp()}");
            Assert(session.Inventory.GetItemCount() == countBefore - 1, "사용한 아이템은 인벤토리에서 제거되어야 함");

            session.Inventory.AddItem(new Item(CharacterClassDatabase.ManaPotionName, ItemType.POTION, 25, 40, "마나를 25 회복합니다."));
            int manaPotionIndex = FindItemIndexByName(session, CharacterClassDatabase.ManaPotionName);
            session.Player.SpendMana(30); // 32 -> 2
            int hpSnapshot = session.Player.GetHp();
            var manaResult = session.UseItem(manaPotionIndex);
            Assert(manaResult == ItemUseResult.Success, "마나 물약 사용은 성공해야 함");
            Assert(session.Player.GetMana() == 27, $"마나 2+25=27이어야 하는데 {session.Player.GetMana()}");
            Assert(session.Player.GetHp() == hpSnapshot, "마나 물약은 HP에 영향을 주면 안 됨");

            var notFound = session.UseItem(999);
            Assert(notFound == ItemUseResult.NotFound, "존재하지 않는 인덱스는 NotFound여야 함");
        }

        private static void TestRestRestoresManaOncePerLocation()
        {
            var session = new GameSession();
            session.BeginNewGameFlow();
            session.SelectPendingClass("mage");
            session.ConfirmClass();

            session.Player.SpendMana(32); // 마나 0
            Assert(session.CanRestHere(), "처음에는 현재 지역에서 휴식 가능해야 함");

            bool rested = session.Rest();
            Assert(rested, "휴식은 처음에는 성공해야 함");
            Assert(session.Player.GetMana() == 13, $"최대마나 32의 40%(반올림 13)여야 하는데 {session.Player.GetMana()}");

            Assert(!session.CanRestHere(), "같은 지역에서는 다시 휴식할 수 없어야 함(지역당 1회 제한)");
            bool restedAgain = session.Rest();
            Assert(!restedAgain, "같은 지역에서 두 번째 휴식 시도는 실패해야 함");
            Assert(session.Player.GetMana() == 13, "휴식 실패 시 마나가 추가로 회복되면 안 됨");

            session.ChooseLocationAction(1); // 던전 입구 -> 갈림길
            Assert(session.CanRestHere(), "다른 지역으로 이동하면 다시 휴식 가능해야 함");
        }

        /// <summary>
        /// 2026-09-08 review-verify-agent Major 확인 회귀 재현 테스트: GameBootstrap이 GameSession을
        /// 앱 실행 중 단 한 번만 생성하고 "새 게임"도 같은 인스턴스를 재사용하므로(TitlePanelController),
        /// ConfirmClass()가 restedLocationIndices를 Clear()하지 않으면 이전 회차에 이미 휴식한 지역에서
        /// 새 캐릭터가 "휴식하기" 버튼을 영영 볼 수 없는 회귀가 생긴다. 이 테스트는 반드시 "새 GameSession을
        /// 새로 만들지 않고" 같은 인스턴스에 대해 Rest() -> ConfirmClass()(새 게임 재시작 시뮬레이션) ->
        /// CanRestHere()가 다시 true인지를 확인해야 실제 버그를 재현한다(기존 테스트들처럼 매번
        /// new GameSession()을 새로 만들면 이 세션-재사용 시나리오를 커버하지 못함).
        /// </summary>
        private static void TestRestLimitResetsWhenSameSessionStartsNewGame()
        {
            var session = new GameSession(); // GameBootstrap과 동일하게 앱 생명주기 동안 단 하나만 생성한다고 가정
            session.BeginNewGameFlow();
            session.SelectPendingClass("mage");
            session.ConfirmClass();

            Assert(session.CanRestHere(), "1회차 시작 시점에는 현재 지역에서 휴식 가능해야 함");
            bool rested = session.Rest();
            Assert(rested, "1회차 휴식은 성공해야 함");
            Assert(!session.CanRestHere(), "1회차에서 휴식한 직후에는 같은 지역에서 다시 휴식할 수 없어야 함");

            // 같은 GameSession 인스턴스로 "새 게임"을 다시 시작한다(앱 재시작 없이 타이틀 -> 새 게임 -> 직업 확정).
            session.BeginNewGameFlow();
            session.SelectPendingClass("warrior");
            bool confirmed = session.ConfirmClass();
            Assert(confirmed, "2회차 직업 확정도 성공해야 함");

            Assert(session.CanRestHere(),
                "2회차(새 게임)는 1회차의 휴식 이력과 무관하게 던전 입구에서 휴식 가능해야 함(ConfirmClass가 restedLocationIndices를 초기화해야 함)");
        }

        private static void TestManaRecoverySkillConsumesMaterialInBattle()
        {
            var session = new GameSession();
            session.BeginNewGameFlow();
            session.SelectPendingClass("mage");
            session.ConfirmClass();

            Assert(!session.HasManaRecoveryMaterial(), "초기에는 마나 결정을 갖고 있지 않아야 함");

            bool triedWithoutMaterial = session.TryUseManaRecoverySkillInBattle(out _);
            Assert(!triedWithoutMaterial, "재료가 없으면 마나 회복 스킬 사용이 실패해야 함");

            session.Inventory.AddItem(new Item(CharacterClassDatabase.ManaRecoveryMaterialName, ItemType.CONSUMABLE, 1, 20, "설명"));
            Assert(session.HasManaRecoveryMaterial(), "재료를 얻으면 감지되어야 함");

            session.ChooseLocationAction(1); // 던전 입구 -> 갈림길
            session.ChooseLocationAction(2); // 오른쪽 통로 -> 어두운 통로, 고블린과 자동 전투
            Assert(session.CurrentState == GameState.BATTLE, "고블린과 전투가 시작되어야 함");

            session.Player.SpendMana(session.Player.GetMana()); // 마나 0으로
            int itemCountBefore = session.Inventory.GetItemCount();

            bool used = session.TryUseManaRecoverySkillInBattle(out BattleResult? battleResult);
            Assert(used, "재료가 있으면 마나 회복 스킬 사용이 성공해야 함");
            Assert(session.Inventory.GetItemCount() == itemCountBefore - 1, "재료 아이템이 소모되어야 함");
            Assert(session.Player.GetMana() > 0, "마나 회복 스킬 사용 후 마나가 0보다 커야 함");
            Assert(!session.HasManaRecoveryMaterial(), "재료를 소모한 뒤에는 더 이상 감지되면 안 됨");
        }
    }
}
