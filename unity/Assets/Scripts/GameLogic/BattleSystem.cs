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
 */

namespace TextRPG.GameLogic
{
    public class BattleSystem
    {
        private readonly Player player;
        private readonly Enemy enemy;
        private int round;

        public int Round => round;
        public Player Player => player;
        public Enemy Enemy => enemy;

        public BattleSystem(Player p, Enemy e)
        {
            player = p;
            enemy = e;
            round = 0;
        }

        /// <summary>src/BattleSystem.cpp startBattle()의 전투 시작 안내 문구(기본 fallback 텍스트) 그대로.</summary>
        public string GetIntroText() => $"전투 시작! {enemy.GetName()} 등장!";

        /// <summary>
        /// 한 라운드를 처리한다. playerAction: 1=공격, 2=도망(원본 getPlayerAction() 값 그대로).
        /// 전투가 계속되면 null, 끝나면 최종 BattleResult를 반환한다.
        /// 반환 즉시 endBattle에 해당하는 보상 지급까지 완료된 상태다.
        /// </summary>
        public BattleResult? TakeTurn(int playerAction)
        {
            round++;

            if (playerAction == 2)
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
            else
            {
                PlayerAttack();
                if (enemy.IsAlive())
                {
                    EnemyAttack();
                }
            }

            if (!player.IsAlive() || !enemy.IsAlive())
            {
                var result = player.IsAlive() ? BattleResult.PLAYER_WIN : BattleResult.PLAYER_LOSE;
                EndBattle(result);
                return result;
            }

            return null;
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
