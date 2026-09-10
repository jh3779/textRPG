/*
 * BattleSystem.cs
 *
 * 📝 역할: include/BattleSystem.h + src/BattleSystem.cpp 포팅.
 * 수치·확률(공격 데미지 = ATK + Random(0,3), 반격 데미지 = ATK + Random(0,2),
 * 도망 성공률 55%)은 원본과 100% 동일하게 유지한다.
 *
 * ⚠️ 구조적 차이(수치 변경 아님, 로직-뷰 분리 때문에 불가피한 변경):
 * 콘솔 원본의 startBattle()은 while 루프 안에서 std::cin으로 매 라운드 입력을
 * 그 자리에서 블로킹으로 받는다. Unity UI는 버튼 클릭으로 입력을 받으므로
 * 블로킹 루프를 쓸 수 없다 — 대신 View(예: ExplorePanelController)가 라운드마다
 * TakeTurn(action)을 한 번씩 호출하는 구조로 바꿨다. 각 라운드 안에서 실행되는
 * 계산 순서(round 증가 → 공격/도망 처리 → 승패 판정)는 원본 while 루프의 한 iteration과
 * 정확히 동일하다.
 *
 * 2026-09-08 확장(DEC-123, Unity 한정 — 콘솔 정본과는 별개):
 * - playerAction에 3(마나 스킬 사용)·4(마나 회복, 재료 소모형) 추가.
 * - 공격속도 기반 선공 결정: 매 턴 player.GetAttackSpeed() >= enemy.AttackSpeed 이면 플레이어 선공,
 *   아니면 적 선공. 동률이면 플레이어 우선(기존 동작과 호환 — 기존 테스트는 전부 기본값 0 vs 0이라
 *   플레이어가 항상 먼저 공격했으므로 이 규칙으로 하위호환 유지).
 * - 도적 쌍검 패시브(player.HasDoubleAttack): 기본 공격이 1회가 아니라 2회 독립 타격.
 * - 마나 스킬 3종(전사 강타/도적 맹독 일격/마법사 화염구)은 기존 데미지 공식(ATK+Random) 위에
 *   배수/보정을 얹는 방식으로 구현 — 공식 자체는 바꾸지 않는다.
 * - 맹독 일격의 도트(지속) 피해는 "총량을 미리 3틱으로 나눠 정해둔 뒤 한 턴에 하나씩 소진"만 지원하는
 *   최소 구현이다(범용 상태이상 시스템 아님 — 동시에 걸 수 있는 도트는 1개뿐이고, 재시전 시 남은 턴/
 *   피해량을 새로 덮어쓴다). 3틱 배분은 정수 나눗셈 손실 없이 합계가 정확히 총량과 같도록 나머지를
 *   앞 틱부터 몰아준다(예: 총량10 -> [4,3,3], 2026-09-08 review-verify-agent Minor 수정).
 * - 마나 회복(action=4)은 재료 아이템을 소모하는 행동이라, 재료 인벤토리 관리 권한이 있는
 *   GameSession이 재료를 먼저 확인·소모한 뒤에만 이 액션을 호출해야 한다(BattleSystem은 Inventory를
 *   모르므로 여기서는 마나 회복량 계산만 담당). 공격이 아닌 턴이므로 선공 판정 없이 항상 적이 반격한다
 *   (도망 실패 분기와 동일한 취급).
 *
 * 2026-09-08 확장(DEC-132, Unity 한정): playerAction에 5(전투 중 "가방"에서 포션 사용) 추가.
 *   마나 회복(action=4)과 마찬가지로 BattleSystem은 Inventory/Item을 모르므로, 실제 HP/마나 회복과
 *   아이템 소모는 호출부(GameSession.TryUseItemInBattle, 기존 GameSession.UseItem 재사용)가 이 메서드를
 *   호출하기 전에 이미 끝내둔다 — 여기서는 "턴을 소모시키는 것"(공격 행동이 아니므로 선공 판정 없이
 *   항상 적이 반격)만 담당한다. 밸런스 요구사항(아이템 사용도 턴을 소모해야 함)을 지키기 위한 최소 구현.
 */

using System;

namespace TextRPG.GameLogic
{
    public class BattleSystem
    {
        public const int ActionAttack = 1;
        public const int ActionFlee = 2;
        public const int ActionManaSkill = 3;
        public const int ActionRecoverMana = 4;

        /// <summary>신규(DEC-132): 전투 중 "가방"에서 포션을 사용하는 행동. 효과 적용은 GameSession이 담당.</summary>
        public const int ActionUseItem = 5;

        /// <summary>신규(DEC-123): "마나 회복" 행동 1회로 회복되는 비율(최대 마나 대비). 40~50% 범위 내에서 45%로 결정.</summary>
        private const double ManaRecoveryRestoreRatio = 0.45;

        private readonly Player player;
        private readonly Enemy enemy;
        private int round;

        // 신규(DEC-123): 도적 "맹독 일격"이 건 도트(지속) 피해 상태. 동시에 1개만 유지되는 최소 구현.
        // enemyPoisonTickAmounts[3]에 3틱 각각의 정확한 피해량을 미리 나눠 담아두고(나머지는 앞 틱에 몰아줌 —
        // 예: 총량10 -> [4,3,3]), enemyPoisonTickCursor로 다음에 적용할 인덱스를 가리킨다. 이렇게 하면
        // "총 최대체력 10%"가 정수 나눗셈을 두 번 거치며 깎이는 일 없이 3틱 합계가 정확히 총량과 같아진다.
        private int enemyPoisonTicksRemaining;
        private int[] enemyPoisonTickAmounts = new int[3];
        private int enemyPoisonTickCursor;

        public int Round => round;
        public Player Player => player;
        public Enemy Enemy => enemy;

        public BattleSystem(Player p, Enemy e)
        {
            player = p;
            enemy = e;
            round = 0;
            enemyPoisonTicksRemaining = 0;
            enemyPoisonTickCursor = 0;
        }

        /// <summary>src/BattleSystem.cpp startBattle()의 전투 시작 안내 문구(기본 fallback 텍스트) 그대로.</summary>
        public string GetIntroText() => $"전투 시작! {enemy.GetName()} 등장!";

        /// <summary>
        /// 한 라운드를 처리한다. playerAction: 1=일반 공격, 2=도망, 3=마나 스킬 사용, 4=마나 회복(재료 소모형).
        /// 전투가 계속되면 null, 끝나면 최종 BattleResult를 반환한다.
        /// 반환 즉시 endBattle에 해당하는 보상 지급까지 완료된 상태다.
        /// </summary>
        public BattleResult? TakeTurn(int playerAction)
        {
            round++;

            if (playerAction == ActionFlee)
            {
                if (AttemptFlee())
                {
                    var fleeResult = BattleResult.PLAYER_FLEE;
                    EndBattle(fleeResult);
                    return fleeResult;
                }

                // 도망 실패 — 원본처럼 플레이어는 공격하지 못하고 적만 반격한다.
                EnemyAttack();
            }
            else if (playerAction == ActionRecoverMana)
            {
                // 공격 행동이 아니므로 선공 판정 없이 항상 적이 반격한다(도망 실패와 동일한 취급).
                int recovered = (int)Math.Round(player.GetMaxMana() * ManaRecoveryRestoreRatio);
                player.RecoverMana(Math.Max(1, recovered));
                EnemyAttack();
            }
            else if (playerAction == ActionUseItem)
            {
                // 신규(DEC-132): 아이템(포션) 효과는 호출부(GameSession)가 이미 적용했다 — 여기서는
                // 공격 행동이 아니므로 선공 판정 없이 턴만 소모시킨다(마나 회복과 동일한 취급).
                EnemyAttack();
            }
            else
            {
                bool playerActsFirst = player.GetAttackSpeed() >= enemy.AttackSpeed;

                if (playerActsFirst)
                {
                    ExecutePlayerAction(playerAction);
                    if (enemy.IsAlive())
                    {
                        EnemyAttack();
                    }
                }
                else
                {
                    EnemyAttack();
                    if (player.IsAlive())
                    {
                        ExecutePlayerAction(playerAction);
                    }
                }
            }

            TickEnemyPoison();

            if (!player.IsAlive() || !enemy.IsAlive())
            {
                var result = player.IsAlive() ? BattleResult.PLAYER_WIN : BattleResult.PLAYER_LOSE;
                EndBattle(result);
                return result;
            }

            return null;
        }

        /// <summary>일반 공격(1) 또는 마나 스킬(3)을 실행한다. 그 외 값은 안전하게 일반 공격으로 취급한다.</summary>
        private void ExecutePlayerAction(int playerAction)
        {
            if (playerAction == ActionManaSkill && CanUseManaSkill())
            {
                UseManaSkill();
                return;
            }

            PlayerAttack();
            if (player.HasDoubleAttack && enemy.IsAlive())
            {
                // 도적 쌍검 패시브(DEC-123): 두 번째 타격도 정상적인 단일 공격과 동일한 공식을 독립 적용.
                PlayerAttack();
            }
        }

        /// <summary>src/BattleSystem.cpp playerAttack() 그대로: ATK + Random(0,3).</summary>
        public int PlayerAttack()
        {
            int damage = player.GetAttack() + Utils.GenerateRandomNumber(0, 3);
            return enemy.TakeDamage(damage);
        }

        /// <summary>src/BattleSystem.cpp enemyAttack() 그대로: ATK + Random(0,2). 적이 죽어있으면 아무 일도 안 함.</summary>
        public int EnemyAttack()
        {
            if (!enemy.IsAlive())
            {
                return 0;
            }

            int damage = enemy.GetAttack() + Utils.GenerateRandomNumber(0, 2);
            return player.TakeDamage(damage);
        }

        /// <summary>src/BattleSystem.cpp attemptFlee() 그대로: 1~100 중 55 이하면 성공(55%).</summary>
        public bool AttemptFlee()
        {
            return Utils.GenerateRandomNumber(1, 100) <= 55;
        }

        /// <summary>신규(DEC-123): 현재 플레이어가 자신의 마나 스킬을 쓸 만큼 마나가 충분한지.</summary>
        public bool CanUseManaSkill()
        {
            var cls = CharacterClassDatabase.Get(player.ClassId);
            return cls != null && player.GetMana() >= cls.ManaSkillCost;
        }

        /// <summary>신규(DEC-123): UI 버튼 라벨용 — "강타 (마나 5)" 형태.</summary>
        public string GetManaSkillLabel()
        {
            var cls = CharacterClassDatabase.Get(player.ClassId);
            return cls == null ? "마나 스킬" : $"{cls.ManaSkillName} (마나 {cls.ManaSkillCost})";
        }

        /// <summary>신규(DEC-123): 직업별 마나 스킬 실행. 기존 공식(ATK+Random) 위에 배수/보정만 얹는다.</summary>
        private void UseManaSkill()
        {
            var cls = CharacterClassDatabase.Get(player.ClassId);
            if (cls == null)
            {
                // 방어적 fallback(클래스 정보가 없으면 스킬을 특정할 수 없음) — 일반 공격으로 대체.
                PlayerAttack();
                return;
            }

            player.SpendMana(cls.ManaSkillCost);

            switch (cls.ManaSkill)
            {
                case ManaSkillType.PowerStrike:
                {
                    // 전사 "강타": 이번 공격의 피해량을 1.5배로 계산(반올림) 후 기존 TakeDamage 공식 적용.
                    int rawDamage = player.GetAttack() + Utils.GenerateRandomNumber(0, 3);
                    int boosted = (int)Math.Round(rawDamage * 1.5);
                    enemy.TakeDamage(boosted);
                    break;
                }
                case ManaSkillType.PoisonStrike:
                {
                    // 도적 "맹독 일격": 즉시 일반 공격 1회(정상 공식) + 적 최대체력 10%를 3턴에 걸쳐 도트로.
                    PlayerAttack();
                    if (enemy.IsAlive())
                    {
                        // 3틱 합계가 정확히 totalDot(최대체력의 10%)이 되도록 나머지를 앞 틱부터 몰아준다
                        // (예: totalDot=10 -> [4,3,3]). 단순히 totalDot/3을 세 번 적용하면 정수 나눗셈이
                        // 두 번 겹쳐 총량이 10%보다 적어지는 문제(예: 100->9%)가 있었다(review-verify-agent Minor).
                        int totalDot = Math.Max(1, enemy.GetMaxHp() / 10);
                        int basePerTick = totalDot / 3;
                        int remainder = totalDot % 3;
                        for (int i = 0; i < 3; i++)
                        {
                            enemyPoisonTickAmounts[i] = basePerTick + (i < remainder ? 1 : 0);
                        }
                        enemyPoisonTickCursor = 0;
                        enemyPoisonTicksRemaining = 3;
                    }
                    break;
                }
                case ManaSkillType.Fireball:
                {
                    // 마법사 "화염구": 이번 공격에 한해 적 방어력을 절반만 적용.
                    int rawDamage = player.GetAttack() + Utils.GenerateRandomNumber(0, 3);
                    enemy.TakeDamageWithHalvedDefense(rawDamage);
                    break;
                }
            }
        }

        /// <summary>신규(DEC-123): 맹독 일격 도트 틱 처리 — 라운드 마지막에 한 번, 남은 턴이 있으면 정해진 피해를 준다.</summary>
        private void TickEnemyPoison()
        {
            if (enemyPoisonTicksRemaining > 0 && enemy.IsAlive())
            {
                enemy.SetHp(enemy.GetHp() - enemyPoisonTickAmounts[enemyPoisonTickCursor]);
                enemyPoisonTickCursor++;
                enemyPoisonTicksRemaining--;
            }
        }

        /// <summary>src/BattleSystem.cpp endBattle() 그대로: 승리 시에만 경험치/골드 지급.</summary>
        public void EndBattle(BattleResult result)
        {
            if (result == BattleResult.PLAYER_WIN)
            {
                player.AddExperience(enemy.GetExperienceReward());
                player.AddGold(enemy.GetGoldReward());
            }
        }
    }
}
