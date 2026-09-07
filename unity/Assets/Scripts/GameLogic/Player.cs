/*
 * Player.cs
 *
 * 📝 역할: include/Player.h + src/Player.cpp 그대로 포팅.
 * 신규: classId 필드(04_data_model.md ENT-102)와, CharacterClass로부터 시작 스탯을
 * 초기화하는 생성자를 추가한다(원본에는 없던 부분 — 직업 시스템 자체가 신규 기능).
 * 기존 기본 생성자(HP100/ATK10/DEF3)는 원본 Player() 그대로 남겨 회귀 비교에 쓴다.
 */

using System;

namespace TextRPG.GameLogic
{
    public class Player
    {
        private readonly string name;
        private int hp;
        private int maxHp;
        private int attack;
        private int defense;
        private int level;
        private int experience;
        private int gold;

        /// <summary>선택한 직업 ID. 신규 필드(ENT-102) — 콘솔 버전에는 없음.</summary>
        public string ClassId { get; private set; }

        /// <summary>원본 Player(const std::string&amp; playerName) 생성자 그대로 — HP100/ATK10/DEF3.</summary>
        public Player(string playerName)
        {
            name = playerName;
            hp = 100;
            maxHp = 100;
            attack = 10;
            defense = 3;
            level = 1;
            experience = 0;
            gold = 0;
            ClassId = null;
        }

        /// <summary>
        /// 신규: CharacterClass의 시작 스탯으로 초기화하는 생성자(DEC-102/111, ENT-101).
        /// 레벨/경험치/골드는 원본과 동일하게 1/0/0에서 시작한다.
        /// </summary>
        public Player(string playerName, CharacterClass characterClass) : this(playerName)
        {
            if (characterClass == null)
            {
                throw new ArgumentNullException(nameof(characterClass));
            }

            ClassId = characterClass.ClassId;
            maxHp = characterClass.BaseHp;
            hp = characterClass.BaseHp;
            attack = characterClass.BaseAttack;
            defense = characterClass.BaseDefense;
        }

        public string GetName() => name;
        public int GetHp() => hp;
        public int GetMaxHp() => maxHp;
        public int GetAttack() => attack;
        public int GetDefense() => defense;
        public int GetLevel() => level;
        public int GetExperience() => experience;
        public int GetGold() => gold;

        public void SetHp(int newHp)
        {
            hp = Utils.Clamp(newHp, 0, maxHp);
        }

        public void SetAttack(int newAttack)
        {
            attack = Math.Max(1, newAttack);
        }

        /// <summary>
        /// 세이브 복원용. src/Player.cpp의 loadState 그대로.
        /// </summary>
        public void LoadState(int savedHp, int savedMaxHp, int savedAttack, int savedDefense,
            int savedLevel, int savedExperience, int savedGold, string savedClassId = null)
        {
            maxHp = Math.Max(1, savedMaxHp);
            hp = Utils.Clamp(savedHp, 0, maxHp);
            attack = Math.Max(1, savedAttack);
            defense = Math.Max(0, savedDefense);
            level = Math.Max(1, savedLevel);
            experience = Math.Max(0, savedExperience);
            gold = Math.Max(0, savedGold);
            if (!string.IsNullOrEmpty(savedClassId))
            {
                ClassId = savedClassId;
            }
        }

        public void AddExperience(int exp)
        {
            if (exp <= 0)
            {
                return;
            }

            experience += exp;
            while (experience >= level * 100)
            {
                experience -= level * 100;
                LevelUp();
            }
        }

        public void AddGold(int amount)
        {
            gold = Math.Max(0, gold + amount);
        }

        /// <summary>
        /// 피해를 입는다. src/Player.cpp의 takeDamage 그대로.
        /// 반환값(실제로 적용된 피해량)은 원본에는 없던 것이지만, 콘솔 std::cout 로그를
        /// UI 로그 문자열로 옮기기 위해 호출부(BattleSystem)가 값을 참조할 수 있게 추가했다.
        /// </summary>
        public int TakeDamage(int damage)
        {
            int finalDamage = Math.Max(1, damage - defense);
            hp = Math.Max(0, hp - finalDamage);
            return finalDamage;
        }

        public bool IsAlive() => hp > 0;

        /// <summary>src/Player.cpp의 levelUp 그대로: 레벨+1, 최대HP+20, 공격+3, 방어+1, HP 전량 회복.</summary>
        public void LevelUp()
        {
            level++;
            maxHp += 20;
            attack += 3;
            defense += 1;
            hp = maxHp;
        }
    }
}
