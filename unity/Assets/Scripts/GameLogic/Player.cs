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

        // 💡 아래 4개 필드는 전부 신규(DEC-123, Unity 한정, 콘솔 원본에는 없음).
        private int mana;
        private int maxMana;
        private int attackSpeed;
        private bool hasDoubleAttack;

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

            // 직업이 없는 원본 콘솔 플레이어에는 마나/공격속도 개념 자체가 없으므로 안전한 기본값(0/false).
            mana = 0;
            maxMana = 0;
            attackSpeed = 0;
            hasDoubleAttack = false;
        }

        /// <summary>
        /// 신규: CharacterClass의 시작 스탯으로 초기화하는 생성자(DEC-102/111, ENT-101).
        /// 레벨/경험치/골드는 원본과 동일하게 1/0/0에서 시작한다.
        /// DEC-123: 마나/공격속도/쌍검 패시브도 이 시점에 CharacterClass(무기 보너스 반영 최종값)에서 복사한다.
        /// 마나는 캐릭터 생성 시 1회 가득 채워지고, 이후에는 전투 결과와 무관하게 이월된다(자동 풀회복 없음).
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

            maxMana = characterClass.BaseMana;
            mana = maxMana;
            attackSpeed = characterClass.AttackSpeed;
            hasDoubleAttack = characterClass.HasDoubleAttack;
        }

        public string GetName() => name;
        public int GetHp() => hp;
        public int GetMaxHp() => maxHp;
        public int GetAttack() => attack;
        public int GetDefense() => defense;
        public int GetLevel() => level;
        public int GetExperience() => experience;
        public int GetGold() => gold;

        /// <summary>신규(DEC-123). 전투 간 이월되는 지속 자원 — 자동 회복 없음(마나 물약/휴식/재료 소모 스킬로만 회복).</summary>
        public int GetMana() => mana;
        public int GetMaxMana() => maxMana;

        /// <summary>신규(DEC-123). 값이 클수록 먼저 행동한다(BattleSystem 선공 판정에 사용).</summary>
        public int GetAttackSpeed() => attackSpeed;

        /// <summary>신규(DEC-123). true면 기본 공격이 2회 독립 타격한다(도적 쌍검 패시브).</summary>
        public bool HasDoubleAttack => hasDoubleAttack;

        public void SetHp(int newHp)
        {
            hp = Utils.Clamp(newHp, 0, maxHp);
        }

        public void SetAttack(int newAttack)
        {
            attack = Math.Max(1, newAttack);
        }

        /// <summary>신규(DEC-123): 마나를 amount만큼 회복(최대 마나 초과 불가). 마나 물약/휴식/DOT 등에서 사용.</summary>
        public void RecoverMana(int amount)
        {
            if (amount <= 0)
            {
                return;
            }
            mana = Utils.Clamp(mana + amount, 0, maxMana);
        }

        /// <summary>신규(DEC-123): 마나를 amount만큼 소모(0 미만으로 내려가지 않음). 마나 스킬 사용 시 호출.</summary>
        public void SpendMana(int amount)
        {
            mana = Utils.Clamp(mana - amount, 0, maxMana);
        }

        /// <summary>
        /// 세이브 복원용. src/Player.cpp의 loadState 그대로.
        /// DEC-123: savedMaxMana/savedMana를 추가(맨 끝, 기본값 0 — 하위호환용).
        /// 마나는 HP처럼 전투 간 이월되는 지속 자원이라 저장/복원이 필요해졌다.
        /// savedMaxMana가 0 이하이면 "구버전 세이브(마나 필드 없음)"로 간주해 건드리지 않고
        /// Player(playerName, characterClass) 생성자가 이미 채워둔 class 기반 마나값을 그대로 둔다
        /// (0으로 덮어써 마나를 잃어버리는 회귀를 방지).
        /// </summary>
        public void LoadState(int savedHp, int savedMaxHp, int savedAttack, int savedDefense,
            int savedLevel, int savedExperience, int savedGold, string savedClassId = null,
            int savedMana = 0, int savedMaxMana = 0)
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

            if (savedMaxMana > 0)
            {
                maxMana = savedMaxMana;
                mana = Utils.Clamp(savedMana, 0, maxMana);
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
