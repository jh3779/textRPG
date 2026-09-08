/*
 * Enemy.cs
 *
 * 📝 역할: include/Enemy.h + src/Enemy.cpp 그대로 포팅.
 * 신규(콘솔에는 없던 것): portraitVariants — 04_data_model.md ENT-103 아트 변형(DEC-114).
 * 스탯에는 영향 없음(OQ-107 미결정 — ASM-104 가정대로 시각만 다름).
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

        /// <summary>신규(DEC-114): 전투 시작 시 무작위로 고를 수 있는 초상화 후보 목록. 스탯과 무관.</summary>
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
