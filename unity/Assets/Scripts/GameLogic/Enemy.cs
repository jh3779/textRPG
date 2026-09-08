/*
 * Enemy.cs
 *
 * 📝 역할: include/Enemy.h + src/Enemy.cpp 그대로 포팅.
 * 신규(콘솔에는 없던 것): portraitVariants — 04_data_model.md ENT-103 아트 변형(DEC-114).
 * 2026-09-08 갱신(DEC-124, OQ-107 해결): 이 필드 자체는 스탯과 무관한 "표시용 초상화 파일명"일 뿐이지만,
 * 몬스터별로 스탯과의 관계가 다르다 — 던전 수호자는 여전히 시각만 다르고 스탯은 무관하게 고정(DEC-114 유지,
 * OQ-108 범위 밖). 반면 고블린은 DEC-124로 변종별 스탯이 서로 다르며, 이 필드가 그 스탯 선택과 함께
 * 확정된 결과(단일 파일명 1개)를 담을 뿐 — 즉 "이 필드 자체가 스탯을 바꾸지는 않는다"는 여전히 사실이지만,
 * 옛 ASM-104 가정("고블린도 그림만 다름")은 더 이상 유효하지 않다(GameSession.StartBattleWithGoblin 참조).
 */

using System;

namespace TextRPG.GameLogic
{
    public class Enemy
    {
        private readonly string name;
        private int hp;
        private readonly int maxHp;
        private readonly int attack;
        private readonly int defense;
        private readonly int experienceReward;
        private readonly int goldReward;

        /// <summary>
        /// 신규(DEC-114): 전투 시작 시 표시할 초상화 파일명. 던전 수호자는 시각만 다르고 스탯은 균형형으로
        /// 고정된 채 이 배열은 항상 그 1장(`enemy_던전수호자.png`)만 담는다(DEC-114 유지, 스탯과 무관).
        /// 2026-09-08 갱신(DEC-124, OQ-107 해결): 고블린은 더 이상 그림만 다른 게 아니라 변종별로 스탯도
        /// 다르다 — 이 배열은 4개 후보 "풀"이 아니라 GameSession.StartBattleWithGoblin()이 스탯 선택을
        /// 마친 뒤 그 변종에 대응하는 단일 확정 포트레이트 1개만 담는다(필드 자체가 스탯을 바꾸지는 않음).
        /// </summary>
        public string[] PortraitVariants { get; set; } = Array.Empty<string>();

        /// <summary>
        /// 신규(DEC-123): 공격속도. 콘솔 원본/기존 Enemy 생성자에는 없던 개념이라 기본값 0을 준다
        /// (BattleSystem이 매 턴 player.GetAttackSpeed()와 이 값을 비교해 선공을 정함 — 기존 적은
        /// 전부 0이므로, 공격속도가 음수인 전사만 적보다 늦게 행동하고 나머지는 그대로 선공 유지).
        /// </summary>
        public int AttackSpeed { get; set; } = 0;

        public Enemy(string enemyName, int hp, int atk, int def, int exp, int gold)
        {
            name = enemyName;
            this.hp = hp;
            maxHp = hp;
            attack = atk;
            defense = def;
            experienceReward = exp;
            goldReward = gold;
        }

        public string GetName() => name;
        public int GetHp() => hp;
        public int GetMaxHp() => maxHp;
        public int GetAttack() => attack;
        public int GetDefense() => defense;
        public int GetExperienceReward() => experienceReward;
        public int GetGoldReward() => goldReward;

        public void SetHp(int newHp)
        {
            hp = Utils.Clamp(newHp, 0, maxHp);
        }

        /// <summary>src/Enemy.cpp의 takeDamage 그대로. 반환값은 UI 로그 출력을 위해 추가.</summary>
        public int TakeDamage(int damage)
        {
            int finalDamage = Math.Max(1, damage - defense);
            hp = Math.Max(0, hp - finalDamage);
            return finalDamage;
        }

        public bool IsAlive() => hp > 0;

        /// <summary>
        /// 신규(DEC-123): 마법사 "화염구" 전용 피해 계산 — 방어력을 절반만(내림) 적용한다.
        /// 콘솔 원본에는 없는 메서드. 기존 TakeDamage 공식(max(1, dmg-def))에서 def만 절반으로 바꾼 변형이다.
        /// </summary>
        public int TakeDamageWithHalvedDefense(int damage)
        {
            int halvedDefense = defense / 2;
            int finalDamage = Math.Max(1, damage - halvedDefense);
            hp = Math.Max(0, hp - finalDamage);
            return finalDamage;
        }
    }
}
