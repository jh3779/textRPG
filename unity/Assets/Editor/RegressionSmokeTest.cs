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

            // 신규(OQ-107 해결, DEC-124): 고블린 4개 변종이 스탯도 다른 별개의 적으로 무작위 선택되는지
            passed += Check("DEC-124: 고블린 조우 시 4개 변종이 전부 무작위로 나오고, 각 변종의 스탯·포트레이트·보상이 정확해야 함",
                TestGoblinVariantSelectionAndStatsMatchVariantTable);

            // 신규(OQ-108 해결, DEC-125): 던전 수호자는 4개 포트레이트 변종이 무작위로 나오되 스탯은 항상 동일해야 함
            passed += Check("DEC-125: 던전 수호자 보스전 조우 시 4개 포트레이트 변종이 전부 무작위로 나오고, 스탯은 항상 균형형 그대로 동일해야 함",
                TestGuardianPortraitVariantSelectionKeepsStatsFixed);

            // 신규(DEC-129): 무기/방어구 장착·교체 시스템 + 3가지 획득 경로(무기고 구매/몬스터 드롭/탐색 상자)
            passed += Check("DEC-129: 무기 교체(장검→대검) 시 HP/ATK/공격속도가 정확히 재계산되어야 함",
                TestWeaponSwapChangesStatsExactly);
            passed += Check("DEC-129: 크로스 직업 무기 장착은 거부되고 기존 장비가 유지되어야 함",
                TestCrossClassWeaponEquipIsRejected);
            passed += Check("DEC-129: 방어구 장착/해제/교체는 DEF·공격속도·HP·마나만 바꾸고 ATK는 절대 바꾸지 않아야 함",
                TestArmorEquipUnequipNeverChangesAttackButChangesOthers);
            passed += Check("DEC-129: 무기+방어구를 동시에 장착하면 두 보너스가 각 스탯에 올바르게 합산되어야 함",
                TestWeaponAndArmorBonusesStackIndependently);
            passed += Check("DEC-129: 고블린 처치 시 신규 무기·가죽 갑옷이 설계된 확률 근방으로 드롭되어야 함(통계적 근사)",
                TestGoblinDefeatSometimesDropsNewWeaponAndLeatherArmor);
            passed += Check("DEC-129: 던전 수호자 처치 시 직업 전용 신규 무기 + 강화 판금 갑옷이 확정 드롭되어야 함",
                TestGuardianDefeatGuaranteesClassWeaponAndPlateArmorDrop);
            passed += Check("DEC-129: 상자 조사는 회차당 1회만 가능하고, 새 게임 시작 시 반드시 초기화되어야 함",
                TestChestInvestigateOncePerRunAndResetsOnNewGame);
            passed += Check("DEC-129: 무기고 구매는 지역/골드 조건을 지켜야 하고 성공 시 골드 차감·아이템 지급이 정확해야 함",
                TestArmoryPurchaseRequiresLocationAndGold);
            passed += Check("DEC-129: 레벨업 이후 무기/방어구를 교체해도 레벨업으로 얻은 영구 보너스가 사라지면 안 됨",
                TestLevelUpBonusSurvivesEquipmentSwap);
            passed += Check("DEC-129 Major 수정: 낡은 무기고 '장비를 챙긴다'(ATK+4)가 이후 무기 교체 후에도 사라지면 안 됨",
                TestArmoryAttackBonusSurvivesWeaponSwap);

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

        /// <summary>
        /// OQ-107 해결(DEC-124): 고블린 4개 시각 변종은 이제 스탯도 다른 별개의 적이다. StartBattleWithGoblin()은
        /// private이라 GameSession의 실제 진행 흐름(던전 입구→갈림길→어두운 통로 자동 전투)을 통해 간접
        /// 트리거한다. 충분한 횟수를 반복해 ① 4종이 전부 무작위로 관측되는지, ② 관측된 각 조합의 스탯/공격속도/
        /// 포트레이트/보상이 06_open_questions.md DEC-124 표와 정확히 일치하는지 확인한다.
        /// </summary>
        private static void TestGoblinVariantSelectionAndStatsMatchVariantTable()
        {
            var expected = new System.Collections.Generic.Dictionary<string, (int Hp, int Atk, int Def, int AttackSpeed)>
            {
                ["enemy_고블린_약소형.png"] = (20, 5, 0, 0),
                ["enemy_고블린_날렵형.png"] = (25, 7, 1, 2),
                ["enemy_고블린_거대형.png"] = (45, 9, 3, -1),
                ["enemy_고블린_주술사형.png"] = (25, 10, 0, 0),
            };

            var observedPortraits = new System.Collections.Generic.HashSet<string>();
            const int trials = 200; // 4종 중 하나라도 관측되지 않을 확률은 (3/4)^200 ≈ 0에 수렴

            for (int i = 0; i < trials; i++)
            {
                var session = new GameSession();
                session.BeginNewGameFlow();
                session.SelectPendingClass("warrior");
                session.ConfirmClass();

                session.ChooseLocationAction(1); // 던전 입구 -> 갈림길
                session.ChooseLocationAction(2); // 오른쪽 통로 -> 어두운 통로, 고블린과 자동 전투
                Assert(session.CurrentState == GameState.BATTLE, "고블린과 전투가 시작되어야 함");

                var enemy = session.CurrentEnemy;
                Assert(enemy.GetName() == "고블린", "고블린 변종이어도 이름은 '고블린'으로 유지되어야 함");
                Assert(enemy.PortraitVariants != null && enemy.PortraitVariants.Length == 1,
                    "DEC-124: 더 이상 4개 후보 풀이 아니라 확정된 포트레이트 1개만 가져야 함");

                string portrait = enemy.PortraitVariants[0];
                Assert(expected.ContainsKey(portrait), $"알 수 없는 포트레이트 파일명: {portrait}");

                var (expHp, expAtk, expDef, expAtkSpd) = expected[portrait];
                Assert(enemy.GetMaxHp() == expHp && enemy.GetAttack() == expAtk && enemy.GetDefense() == expDef,
                    $"{portrait} 스탯 불일치: HP{enemy.GetMaxHp()}/ATK{enemy.GetAttack()}/DEF{enemy.GetDefense()} " +
                    $"(기대 HP{expHp}/ATK{expAtk}/DEF{expDef})");
                Assert(enemy.AttackSpeed == expAtkSpd, $"{portrait} 공격속도 불일치: {enemy.AttackSpeed} (기대 {expAtkSpd})");
                Assert(enemy.GetExperienceReward() == 60 && enemy.GetGoldReward() == 25,
                    "DEC-124: 4종 모두 EXP60/골드25 보상은 원본과 동일하게 유지되어야 함");

                observedPortraits.Add(portrait);
            }

            Assert(observedPortraits.Count == 4,
                $"{trials}회 시행에서 4종 변종이 전부 관측되어야 하는데 {observedPortraits.Count}종만 관측됨");
        }

        /// <summary>
        /// OQ-108 해결(DEC-125): 던전 수호자 예비 변종 3개(중장형·기동형·마도형)는 "회차마다 랜덤 보스"로
        /// 쓰기로 확정됐지만, 고블린(DEC-124)과 달리 스탯을 다르게 하라는 요구는 없었다 — 4개 변종
        /// (균형형 포함) 모두 스탯(HP55/ATK10/DEF3, src/Game.cpp 원본 그대로)은 항상 동일하고 포트레이트만
        /// 무작위로 달라져야 한다. GameSession의 실제 진행 흐름으로 보스의 방까지 도달해야 하므로, 먼저
        /// 고블린전을 이겨야 한다(고블린 스탯은 DEC-124로 변종마다 다르지만 승패와 무관하게 이 테스트의
        /// 관심사가 아니다) — 패배/도주 시에는 보스의 방에 도달하지 못하므로 해당 시행은 버리고 재시도한다.
        /// </summary>
        private static void TestGuardianPortraitVariantSelectionKeepsStatsFixed()
        {
            var expectedPortraits = new System.Collections.Generic.HashSet<string>
            {
                "enemy_던전수호자_균형형.png",
                "enemy_던전수호자_중장형.png",
                "enemy_던전수호자_기동형.png",
                "enemy_던전수호자_마도형.png",
            };

            var observedPortraits = new System.Collections.Generic.HashSet<string>();
            const int trials = 200; // 4종 중 하나라도 관측되지 않을 확률은 (3/4)^200 ≈ 0에 수렴

            for (int i = 0; i < trials; i++)
            {
                GameSession session = null;
                bool reachedBossRoom = false;

                // 고블린전은 변종별 스탯이 랜덤이라 패배/도주할 수 있다 — 보스의 방에 도달할 때까지 재시도한다.
                for (int attempt = 0; attempt < 30 && !reachedBossRoom; attempt++)
                {
                    session = new GameSession();
                    session.BeginNewGameFlow();
                    session.SelectPendingClass("warrior"); // HP114(DEC-123)로 고블린전 생존 여유가 가장 큼
                    session.ConfirmClass();

                    session.ChooseLocationAction(1); // 던전 입구 -> 갈림길
                    session.ChooseLocationAction(2); // 오른쪽 통로 -> 어두운 통로, 고블린과 자동 전투
                    Assert(session.CurrentState == GameState.BATTLE, "고블린과 전투가 시작되어야 함");

                    BattleResult? goblinResult = null;
                    int guard = 0;
                    while (goblinResult == null && guard < 1000)
                    {
                        goblinResult = session.ProcessBattleTurn(1); // 항상 공격
                        guard++;
                    }
                    Assert(goblinResult != null, "고블린전이 끝나야 함");

                    if (goblinResult == BattleResult.PLAYER_WIN)
                    {
                        Assert(session.Map.GetCurrentLocationIndex() == 4, "고블린 처치 후 보스의 방으로 자동 이동해야 함");
                        reachedBossRoom = true;
                    }
                }
                Assert(reachedBossRoom, "여러 번 재시도해도 보스의 방에 도달하지 못함(고블린전 승리 실패)");

                session.ChooseLocationAction(1); // 보스에게 도전 -> StartBattleWithGuardian()
                Assert(session.CurrentState == GameState.BATTLE, "던전 수호자와 전투가 시작되어야 함");

                var guardian = session.CurrentEnemy;
                Assert(guardian.GetName() == "던전 수호자", "적 이름은 '던전 수호자'로 고정되어야 함");
                Assert(guardian.PortraitVariants != null && guardian.PortraitVariants.Length == 1,
                    "DEC-125: 4개 후보 풀이 아니라 확정된 포트레이트 1개만 가져야 함");

                string portrait = guardian.PortraitVariants[0];
                Assert(expectedPortraits.Contains(portrait), $"알 수 없는 던전 수호자 포트레이트 파일명: {portrait}");

                // DEC-125 핵심: 포트레이트가 무엇이든 스탯은 항상 균형형(원본) 그대로 고정이어야 한다.
                Assert(guardian.GetMaxHp() == 55 && guardian.GetAttack() == 10 && guardian.GetDefense() == 3,
                    $"던전 수호자 스탯은 변종({portrait})과 무관하게 항상 HP55/ATK10/DEF3이어야 하는데 " +
                    $"HP{guardian.GetMaxHp()}/ATK{guardian.GetAttack()}/DEF{guardian.GetDefense()}");
                Assert(guardian.AttackSpeed == 0, $"던전 수호자 공격속도는 변종과 무관하게 항상 0(기본값)이어야 하는데 {guardian.AttackSpeed}");
                Assert(guardian.GetExperienceReward() == 120 && guardian.GetGoldReward() == 70,
                    "던전 수호자 보상(EXP120/골드70)은 변종과 무관하게 항상 동일해야 함");

                observedPortraits.Add(portrait);
            }

            Assert(observedPortraits.Count == 4,
                $"{trials}회 시행에서 던전 수호자 4종 포트레이트 변종이 전부 관측되어야 하는데 {observedPortraits.Count}종만 관측됨");
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

        // ───────────────────────── 신규(DEC-129) 회귀 테스트: 무기/방어구 장착·구매·드롭·상자 ─────────────────────────

        private static GameSession NewWarriorSession()
        {
            var session = new GameSession();
            session.BeginNewGameFlow();
            session.SelectPendingClass("warrior");
            session.ConfirmClass();
            return session;
        }

        private static void TestWeaponSwapChangesStatsExactly()
        {
            var session = NewWarriorSession();
            var p = session.Player;

            Assert(p.EquippedWeaponName == "장검", "전사는 장검을 장착한 채로 시작해야 함");
            Assert(p.GetMaxHp() == 114 && p.GetAttack() == 13 && p.GetAttackSpeed() == -1,
                $"전사 시작 스탯은 여전히 HP114/ATK13/AtkSpd-1이어야 하는데 HP{p.GetMaxHp()}/ATK{p.GetAttack()}/AtkSpd{p.GetAttackSpeed()}");

            var greatsword = WeaponDatabase.GetNewWeaponForClass("warrior");
            Assert(greatsword != null && greatsword.Name == "대검", "전사의 신규 무기는 대검이어야 함");

            session.Inventory.AddItem(greatsword.CreateItem());
            var result = session.EquipWeaponByName("대검");
            Assert(result == EquipWeaponResult.Success, "인벤토리에 있는 자기 직업 무기 장착은 성공해야 함");

            Assert(p.EquippedWeaponName == "대검", "장착 후 EquippedWeaponName이 대검으로 바뀌어야 함");
            Assert(p.GetMaxHp() == 110 + 8, $"대검 장착 후 최대HP는 110+8=118이어야 하는데 {p.GetMaxHp()}");
            Assert(p.GetHp() == p.GetMaxHp(), "장착 전 풀피였다면 최대HP가 늘어난 만큼 현재 HP도 같이 늘어 여전히 풀피여야 함");
            Assert(p.GetAttack() == 10 + 6, $"대검 장착 후 ATK는 10+6=16이어야 하는데 {p.GetAttack()}");
            Assert(p.GetAttackSpeed() == -3, $"대검 장착 후 공격속도는 -3이어야 하는데 {p.GetAttackSpeed()}");
            Assert(p.GetMaxMana() == 10, "대검은 마나 보너스가 없으므로 최대 마나는 여전히 10이어야 함");
            Assert(!p.HasDoubleAttack, "전사 무기는 어느 쪽도 쌍검 패시브가 없어야 함");
            Assert(p.GetDefense() == 5, "무기 교체는 방어력에 절대 영향을 주면 안 됨");
        }

        private static void TestCrossClassWeaponEquipIsRejected()
        {
            var session = NewWarriorSession();
            // 정상 경로로는 절대 발생하지 않지만(무기는 항상 자기 직업 무기만 지급/드롭/구매됨),
            // 방어 로직 자체를 검증하기 위해 도적 전용 무기를 억지로 인벤토리에 넣어본다.
            session.Inventory.AddItem(WeaponDatabase.Get(WeaponDatabase.Dagger).CreateItem());

            var result = session.EquipWeaponByName(WeaponDatabase.Dagger);
            Assert(result == EquipWeaponResult.WrongClass, "다른 직업 전용 무기 장착은 WrongClass로 실패해야 함");
            Assert(session.Player.EquippedWeaponName == "장검", "실패한 장착 시도는 기존 장착 무기를 바꾸면 안 됨");
            Assert(session.Player.GetAttack() == 13, "실패한 장착 시도는 스탯도 전혀 바꾸면 안 됨");
        }

        private static void TestArmorEquipUnequipNeverChangesAttackButChangesOthers()
        {
            var session = NewWarriorSession();
            var p = session.Player;

            int atkBefore = p.GetAttack();       // 13
            int hpBefore = p.GetMaxHp();          // 114
            int defBefore = p.GetDefense();       // 5
            int speedBefore = p.GetAttackSpeed(); // -1
            int manaBefore = p.GetMaxMana();      // 10

            Assert(p.EquippedArmorName == null, "게임 시작 시에는 어떤 직업이든 방어구를 장착하지 않은 맨몸 상태여야 함");

            session.Inventory.AddItem(ArmorDatabase.Get(ArmorDatabase.LeatherArmor).CreateItem());
            var equipLeather = session.EquipArmorByName(ArmorDatabase.LeatherArmor);
            Assert(equipLeather == EquipArmorResult.Success, "가죽 갑옷 장착은 성공해야 함");
            Assert(p.EquippedArmorName == ArmorDatabase.LeatherArmor, "EquippedArmorName이 가죽 갑옷이어야 함");
            Assert(p.GetAttack() == atkBefore, $"가죽 갑옷 장착 후에도 ATK는 그대로({atkBefore})여야 하는데 {p.GetAttack()}");
            Assert(p.GetDefense() == defBefore + 3, $"DEF는 {defBefore}+3={defBefore + 3}이어야 하는데 {p.GetDefense()}");
            Assert(p.GetMaxHp() == hpBefore + 8, $"최대HP는 {hpBefore}+8={hpBefore + 8}이어야 하는데 {p.GetMaxHp()}");
            Assert(p.GetAttackSpeed() == speedBefore, "가죽 갑옷은 공격속도 보너스가 0이므로 그대로여야 함");
            Assert(p.GetMaxMana() == manaBefore, "가죽 갑옷은 마나 보너스가 0이므로 그대로여야 함");

            // 교체: 강화 판금 갑옷
            session.Inventory.AddItem(ArmorDatabase.Get(ArmorDatabase.ReinforcedPlateArmor).CreateItem());
            var equipPlate = session.EquipArmorByName(ArmorDatabase.ReinforcedPlateArmor);
            Assert(equipPlate == EquipArmorResult.Success, "강화 판금 갑옷으로 교체는 성공해야 함");
            Assert(p.GetAttack() == atkBefore, $"판금 갑옷 교체 후에도 ATK는 여전히 그대로({atkBefore})여야 하는데 {p.GetAttack()}");
            Assert(p.GetDefense() == defBefore + 7, $"DEF는 {defBefore}+7={defBefore + 7}이어야 하는데 {p.GetDefense()}");
            Assert(p.GetMaxHp() == hpBefore + 15, $"최대HP는 {hpBefore}+15={hpBefore + 15}이어야 하는데 {p.GetMaxHp()}");
            Assert(p.GetAttackSpeed() == speedBefore - 2, $"공격속도는 {speedBefore}-2={speedBefore - 2}여야 하는데 {p.GetAttackSpeed()}");
            Assert(p.GetMaxMana() == manaBefore - 3, $"최대 마나는 {manaBefore}-3={manaBefore - 3}이어야 하는데 {p.GetMaxMana()}");

            // 해제: 맨몸으로
            session.UnequipArmor();
            Assert(p.EquippedArmorName == null, "해제 후에는 다시 맨몸(null)이어야 함");
            Assert(p.GetAttack() == atkBefore, "해제 후에도 ATK는 계속 그대로여야 함");
            Assert(p.GetDefense() == defBefore, "해제 후 DEF는 원래 값으로 돌아와야 함");
            Assert(p.GetMaxHp() == hpBefore, "해제 후 최대HP는 원래 값으로 돌아와야 함");
            Assert(p.GetAttackSpeed() == speedBefore, "해제 후 공격속도는 원래 값으로 돌아와야 함");
            Assert(p.GetMaxMana() == manaBefore, "해제 후 최대 마나는 원래 값으로 돌아와야 함");
        }

        private static void TestWeaponAndArmorBonusesStackIndependently()
        {
            var session = NewWarriorSession();
            var p = session.Player;

            session.Inventory.AddItem(WeaponDatabase.GetNewWeaponForClass("warrior").CreateItem());
            session.Inventory.AddItem(ArmorDatabase.Get(ArmorDatabase.ReinforcedPlateArmor).CreateItem());

            Assert(session.EquipWeaponByName("대검") == EquipWeaponResult.Success, "대검 장착은 성공해야 함");
            Assert(session.EquipArmorByName(ArmorDatabase.ReinforcedPlateArmor) == EquipArmorResult.Success, "판금 갑옷 장착은 성공해야 함");

            // raw(무기·방어구 제외): HP110/ATK10/DEF5/Mana10. 대검(HP+8/ATK+6/AtkSpd-3) + 판금(DEF+7/HP+15/AtkSpd-2/Mana-3).
            Assert(p.GetMaxHp() == 110 + 8 + 15, $"HP는 raw+무기+방어구=133이어야 하는데 {p.GetMaxHp()}");
            Assert(p.GetAttack() == 10 + 6, $"ATK는 raw+무기(방어구 제외)=16이어야 하는데 {p.GetAttack()}");
            Assert(p.GetDefense() == 5 + 7, $"DEF는 raw+방어구(무기 제외)=12여야 하는데 {p.GetDefense()}");
            Assert(p.GetAttackSpeed() == -3 + -2, $"공격속도는 무기+방어구 합산=-5여야 하는데 {p.GetAttackSpeed()}");
            Assert(p.GetMaxMana() == 10 + 0 - 3, $"최대 마나는 raw+무기(0)+방어구(-3)=7이어야 하는데 {p.GetMaxMana()}");
        }

        private static void TestGoblinDefeatSometimesDropsNewWeaponAndLeatherArmor()
        {
            int wins = 0;
            int weaponDrops = 0;
            int armorDrops = 0;
            const int trials = 300;

            for (int i = 0; i < trials; i++)
            {
                var session = NewWarriorSession();
                session.ChooseLocationAction(1); // 던전 입구 -> 갈림길
                session.ChooseLocationAction(2); // 오른쪽 통로 -> 어두운 통로, 고블린과 자동 전투

                BattleResult? result = null;
                int guard = 0;
                while (result == null && guard < 1000)
                {
                    result = session.ProcessBattleTurn(BattleSystem.ActionAttack);
                    guard++;
                }

                if (result == BattleResult.PLAYER_WIN)
                {
                    wins++;
                    if (FindItemIndexByName(session, "대검") >= 0) weaponDrops++;
                    if (FindItemIndexByName(session, ArmorDatabase.LeatherArmor) >= 0) armorDrops++;
                }
            }

            Assert(wins > trials / 2, $"전사 기준 고블린전은 대부분 승리해야 통계 검증이 유효한데 {wins}/{trials}승");

            double weaponRate = weaponDrops / (double)wins;
            double armorRate = armorDrops / (double)wins;
            // 설계값 20%/15% ± 넉넉한 여유(표본 변동성 감안, 완전히 어긋난 배선만 잡아내면 충분).
            Assert(weaponRate > 0.08 && weaponRate < 0.35,
                $"신규 무기 드롭률이 20% 근방이어야 하는데 {weaponRate:P1}({weaponDrops}/{wins})");
            Assert(armorRate > 0.05 && armorRate < 0.30,
                $"가죽 갑옷 드롭률이 15% 근방이어야 하는데 {armorRate:P1}({armorDrops}/{wins})");
        }

        private static void TestGuardianDefeatGuaranteesClassWeaponAndPlateArmorDrop()
        {
            bool reached = false;

            for (int attempt = 0; attempt < 30 && !reached; attempt++)
            {
                var session = NewWarriorSession();
                session.ChooseLocationAction(1);
                session.ChooseLocationAction(2);

                BattleResult? goblinResult = null;
                int guard = 0;
                while (goblinResult == null && guard < 1000)
                {
                    goblinResult = session.ProcessBattleTurn(BattleSystem.ActionAttack);
                    guard++;
                }
                if (goblinResult != BattleResult.PLAYER_WIN)
                {
                    continue;
                }

                // 고블린 처치 보상/드롭(RNG)으로 인벤토리가 가득 차 있으면 보스의 확정 드롭이 자리가 없어
                // 실패할 수 있다 — 이 테스트의 관심사는 "확정 드롭 로직 자체"이므로, 드롭 검증 전에
                // 인벤토리를 비워 자리를 확보한다(용량 5는 그대로 유지 — 내용물만 정리).
                while (session.Inventory.GetItemCount() > 0)
                {
                    session.Inventory.RemoveItem(0);
                }

                session.ChooseLocationAction(1); // 보스에게 도전
                Assert(session.CurrentState == GameState.BATTLE, "던전 수호자와 전투가 시작되어야 함");

                BattleResult? bossResult = null;
                guard = 0;
                while (bossResult == null && guard < 1000)
                {
                    bossResult = session.ProcessBattleTurn(BattleSystem.ActionAttack);
                    guard++;
                }
                if (bossResult != BattleResult.PLAYER_WIN)
                {
                    continue;
                }

                reached = true;
                Assert(FindItemIndexByName(session, "대검") >= 0,
                    "던전 수호자 처치 시 직업 전용 신규 무기(대검)가 확정 드롭되어야 함");
                Assert(FindItemIndexByName(session, ArmorDatabase.ReinforcedPlateArmor) >= 0,
                    "던전 수호자 처치 시 강화 판금 갑옷이 확정 드롭되어야 함");
            }

            Assert(reached, "여러 번 재시도해도 던전 수호자를 처치하지 못함(테스트 환경 문제 가능성)");
        }

        private static void TestChestInvestigateOncePerRunAndResetsOnNewGame()
        {
            var session = NewWarriorSession();

            session.ChooseLocationAction(1); // 던전 입구 -> 갈림길(상자 위치)
            Assert(session.Map.GetCurrentLocationIndex() == 1, "갈림길로 이동해야 함");
            Assert(session.CanInvestigateChestHere(), "갈림길에 처음 도착하면 상자를 조사할 수 있어야 함");

            var firstAttempt = session.InvestigateChest();
            Assert(firstAttempt != ChestResult.NotAvailable, "처음 시도는 NotAvailable이면 안 됨");
            Assert(!session.CanInvestigateChestHere(), "한 번 조사한 뒤에는 같은 회차에서 다시 조사할 수 없어야 함");

            var secondAttempt = session.InvestigateChest();
            Assert(secondAttempt == ChestResult.NotAvailable, "같은 회차의 두 번째 시도는 NotAvailable이어야 함");

            // 같은 GameSession 인스턴스로 "새 게임"을 다시 시작한다(DEC-123 review에서 발견된 것과
            // 동일한 종류의 "세션 재사용 시 초기화 안 되는 버그"를 반복하지 않는지 확인).
            session.BeginNewGameFlow();
            session.SelectPendingClass("rogue");
            bool confirmed = session.ConfirmClass();
            Assert(confirmed, "2회차 직업 확정도 성공해야 함");

            session.ChooseLocationAction(1); // 던전 입구 -> 갈림길
            Assert(session.CanInvestigateChestHere(),
                "2회차(새 게임)는 1회차의 상자 시도 이력과 무관하게 갈림길에서 다시 상자를 조사할 수 있어야 함(ConfirmClass가 초기화해야 함)");
        }

        private static void TestArmoryPurchaseRequiresLocationAndGold()
        {
            var session = NewWarriorSession();

            Assert(session.PurchaseNewWeapon() == PurchaseWeaponResult.NotAtArmory,
                "무기고(지역2)가 아니면 무기 구매가 실패해야 함(현재 지역: 던전 입구)");
            Assert(session.PurchaseArmor(ArmorDatabase.LeatherArmor) == PurchaseArmorResult.NotAtArmory,
                "무기고(지역2)가 아니면 방어구 구매가 실패해야 함");

            session.ChooseLocationAction(1); // 갈림길
            session.ChooseLocationAction(1); // 무기고(지역2)
            Assert(session.Map.GetCurrentLocationIndex() == 2, "무기고로 이동해야 함");

            Assert(session.Player.GetGold() == 0, "전투 전이라 골드는 0이어야 함");
            Assert(session.PurchaseNewWeapon() == PurchaseWeaponResult.NotEnoughGold, "골드가 없으면 무기 구매가 실패해야 함");

            session.Player.AddGold(100);
            int beforeCount = session.Inventory.GetItemCount();
            var weaponPurchase = session.PurchaseNewWeapon();
            Assert(weaponPurchase == PurchaseWeaponResult.Success, "골드가 충분하면 무기 구매가 성공해야 함");
            Assert(session.Player.GetGold() == 100 - WeaponDatabase.NewWeaponShopPrice,
                $"구매 가격({WeaponDatabase.NewWeaponShopPrice})만큼 골드가 차감되어야 하는데 {session.Player.GetGold()}");
            Assert(session.Inventory.GetItemCount() == beforeCount + 1, "구매한 무기가 인벤토리에 추가되어야 함");
            Assert(FindItemIndexByName(session, "대검") >= 0, "구매한 무기(대검)가 실제로 인벤토리에 있어야 함");

            var armorPurchase = session.PurchaseArmor(ArmorDatabase.LeatherArmor);
            Assert(armorPurchase == PurchaseArmorResult.Success, "가죽 갑옷 구매가 성공해야 함");
            Assert(FindItemIndexByName(session, ArmorDatabase.LeatherArmor) >= 0, "구매한 가죽 갑옷이 실제로 인벤토리에 있어야 함");
        }

        private static void TestLevelUpBonusSurvivesEquipmentSwap()
        {
            var session = NewWarriorSession();
            var p = session.Player;

            int hpBeforeLevelUp = p.GetMaxHp();   // 114
            int atkBeforeLevelUp = p.GetAttack(); // 13
            int defBeforeLevelUp = p.GetDefense();// 5

            p.LevelUp(); // 레벨업: raw 기준선에 HP+20/ATK+3/DEF+1이 영구히 더해져야 함

            Assert(p.GetMaxHp() == hpBeforeLevelUp + 20, $"레벨업 직후 최대HP는 {hpBeforeLevelUp + 20}이어야 하는데 {p.GetMaxHp()}");
            Assert(p.GetAttack() == atkBeforeLevelUp + 3, $"레벨업 직후 ATK는 {atkBeforeLevelUp + 3}이어야 하는데 {p.GetAttack()}");
            Assert(p.GetDefense() == defBeforeLevelUp + 1, $"레벨업 직후 DEF는 {defBeforeLevelUp + 1}이어야 하는데 {p.GetDefense()}");

            // 레벨업 이후 무기를 대검으로 교체해도 레벨업 보너스가 raw 기준선에 남아 있어야 하므로,
            // "레벨업 후 스탯 - 장검 보너스 + 대검 보너스"가 새 최종값이어야 한다(레벨업 보너스가
            // 증발해 raw 그대로(레벨1 기준)로 되돌아가면 안 됨).
            int hpAfterLevelUp = p.GetMaxHp();
            int atkAfterLevelUp = p.GetAttack();

            session.Inventory.AddItem(WeaponDatabase.GetNewWeaponForClass("warrior").CreateItem());
            Assert(session.EquipWeaponByName("대검") == EquipWeaponResult.Success, "레벨업 이후에도 무기 교체는 성공해야 함");

            int longswordHpDelta = WeaponDatabase.Get(WeaponDatabase.Longsword).HpDelta;   // +4
            int greatswordHpDelta = WeaponDatabase.Get(WeaponDatabase.Greatsword).HpDelta; // +8
            int longswordAtkDelta = WeaponDatabase.Get(WeaponDatabase.Longsword).AttackDelta;   // +3
            int greatswordAtkDelta = WeaponDatabase.Get(WeaponDatabase.Greatsword).AttackDelta; // +6

            int expectedHp = hpAfterLevelUp - longswordHpDelta + greatswordHpDelta;
            int expectedAtk = atkAfterLevelUp - longswordAtkDelta + greatswordAtkDelta;

            Assert(p.GetMaxHp() == expectedHp,
                $"레벨업 보너스가 유지된 채 무기만 교체됐다면 최대HP는 {expectedHp}여야 하는데 {p.GetMaxHp()}(레벨업 보너스가 증발했다면 버그)");
            Assert(p.GetAttack() == expectedAtk,
                $"레벨업 보너스가 유지된 채 무기만 교체됐다면 ATK는 {expectedAtk}여야 하는데 {p.GetAttack()}(레벨업 보너스가 증발했다면 버그)");
            Assert(p.GetDefense() == defBeforeLevelUp + 1, "무기 교체는 방어력에 영향을 주지 않으므로 레벨업 DEF 보너스가 그대로 유지되어야 함");
        }

        /// <summary>
        /// review-verify-agent Major 확인 회귀 재현 테스트: 낡은 무기고 "장비를 챙긴다"(GameSession.cs
        /// location 2, choice 1)가 주는 ATK+4는 Player.SetAttack()으로 attack 필드를 직접 덮어썼는데,
        /// 이는 raw 기준선(baseAttackNoWeapon)을 거치지 않아 이후 무기를 교체하면 RecomputeStats()가
        /// raw+새 무기 보너스로 값을 재계산해버려 +4가 조용히 증발했다(전사 ATK 13→17→[무기 교체]→16으로
        /// 오히려 감소). Player.AddPermanentAttackBonus()로 교체해 raw 기준선에 반영하도록 수정했으므로,
        /// 이 테스트는 "장비를 챙긴다 → 무기 교체" 순서로 실제 진행해 ATK+4 보너스가 계속 유지되는지 확인한다.
        /// </summary>
        private static void TestArmoryAttackBonusSurvivesWeaponSwap()
        {
            var session = NewWarriorSession();
            session.ChooseLocationAction(1); // 던전 입구 -> 갈림길
            session.ChooseLocationAction(1); // 왼쪽 빛 -> 무기고(지역 2)
            Assert(session.Map.GetCurrentLocationIndex() == 2, "무기고로 이동해야 함");

            int atkBeforeLoot = session.Player.GetAttack(); // 13
            session.ChooseLocationAction(1); // "장비를 챙긴다" -> ATK+4 (곧바로 어두운 통로로 이동해 고블린전 자동 진입)
            Assert(session.Player.GetAttack() == atkBeforeLoot + 4,
                $"장비를 챙기면 ATK가 {atkBeforeLoot + 4}여야 하는데 {session.Player.GetAttack()}");
            Assert(session.ArmoryLooted, "ArmoryLooted가 true로 설정되어야 함");

            int atkAfterLoot = session.Player.GetAttack(); // 17

            // 무기 교체(장검 -> 대검): raw 기준선에 반영된 +4 보너스는 절대 사라지면 안 된다.
            session.Inventory.AddItem(WeaponDatabase.GetNewWeaponForClass("warrior").CreateItem());
            var equipResult = session.EquipWeaponByName("대검");
            Assert(equipResult == EquipWeaponResult.Success, "무기 교체는 성공해야 함");

            int longswordAtkDelta = WeaponDatabase.Get(WeaponDatabase.Longsword).AttackDelta;   // +3
            int greatswordAtkDelta = WeaponDatabase.Get(WeaponDatabase.Greatsword).AttackDelta; // +6
            int expectedAtk = atkAfterLoot - longswordAtkDelta + greatswordAtkDelta; // 17-3+6=20

            Assert(session.Player.GetAttack() == expectedAtk,
                $"무기 교체 후에도 무기고에서 얻은 ATK+4 보너스가 유지된 채 {expectedAtk}여야 하는데 {session.Player.GetAttack()}" +
                "(만약 17이 아니라 16이 나온다면 +4 보너스가 증발한 것 — 바로 그 버그)");
        }
    }
}
