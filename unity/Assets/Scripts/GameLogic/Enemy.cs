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
    }
}
