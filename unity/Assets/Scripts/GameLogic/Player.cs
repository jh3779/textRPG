/*
 * Player.cs
 *
 * 📝 역할: include/Player.h + src/Player.cpp 그대로 포팅.
 * 신규: classId 필드(04_data_model.md ENT-102)와, CharacterClass로부터 시작 스탯을
 * 초기화하는 생성자를 추가한다(원본에는 없던 부분 — 직업 시스템 자체가 신규 기능).
 * 기존 기본 생성자(HP100/ATK10/DEF3)는 원본 Player() 그대로 남겨 회귀 비교에 쓴다.
 *
 * 2026-09-08 확장(DEC-129, Unity 한정): 무기/방어구 장착·교체 시스템을 위해 "장비 보너스를 뺀
 * 기본(raw) 스탯"을 baseHpNoWeapon/baseAttackNoWeapon/baseManaNoWeapon/baseDefenseNoArmor에
 * 별도로 기억해두고, RecomputeStats()가 이 값 + 현재 장착한 무기/방어구 보너스로 최종 스탯을
 * 매번 다시 계산한다 — "무기는 HP/ATK/공격속도/마나/쌍검 패시브에만 관여, 방어구는 DEF/공격속도/
 * HP/마나에만 관여하고 ATK에는 절대 관여하지 않는다"는 역할 분담이 이 한 메서드에 정확히
 * 반영돼 있다. 캐릭터 생성 시에는 CharacterClass.DefaultWeaponName으로 EquipWeapon()을 1회
 * 호출해 DEC-123 최종 스탯과 정확히 같은 값을 만든다(회귀 테스트로 검증) — 즉 이 리팩터링
 * 자체는 시작 스탯을 전혀 바꾸지 않고, 이후 장비를 갈아입었을 때만 스탯이 바뀐다. 방어구는
 * 콘솔 원본은 물론 DEC-123에도 없던 완전히 새로운 슬롯이라, 게임 시작 시엔 아무 것도
 * 장착하지 않은 "맨몸"(equippedArmor == null) 상태로 시작한다.
 *
 * LevelUp()도 이번에 raw 기준선(baseHpNoWeapon/baseAttackNoWeapon/baseDefenseNoArmor)을
 * 올리고 RecomputeStats()를 다시 태우도록 바꿨다 — 레벨업 이후에 무기·방어구를 갈아입어도
 * 레벨업으로 얻은 영구 보너스가 사라지지 않도록 하기 위함이다(직접 maxHp/attack/defense
 * 필드를 건드리면 다음 장비 교체 때 raw 기준선만으로 재계산되어 레벨업 보너스가 증발한다).
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

        // 💡 신규(DEC-129): 장비(무기/방어구) 보너스를 전부 뺀 기본 스탯 — RecomputeStats()가 이 값
        // 기준으로 최종 스탯을 매번 다시 계산하는 기준선이다. LevelUp()이 여기에 영구 보너스를
        // 더한다. 직업이 없는 콘솔 호환 생성자(Player(playerName))도 자기 자신의 기본값(100/10/3)을
        // 그대로 기준선으로 채워, 그 경로에서도 LevelUp()이 올바르게 동작하게 한다.
        private int baseHpNoWeapon;
        private int baseAttackNoWeapon;
        private int baseManaNoWeapon;
        private int baseDefenseNoArmor;

        private WeaponDefinition equippedWeapon;
        private ArmorDefinition equippedArmor; // 신규(DEC-129): null이면 "맨몸"(방어구 미착용) 상태.

        /// <summary>신규(DEC-129): 현재 장착 중인 무기 이름(WeaponDatabase 키와 동일). 미장착 상태는 없음(항상 하나는 장착).</summary>
        public string EquippedWeaponName => equippedWeapon?.Name;

        /// <summary>신규(DEC-129): 현재 장착 중인 방어구 이름(ArmorDatabase 키와 동일). 미착용이면 null.</summary>
        public string EquippedArmorName => equippedArmor?.Name;

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

            // 직업이 없는 원본 콘솔 플레이어에는 마나/공격속도/장비 개념 자체가 없으므로 안전한 기본값.
            mana = 0;
            maxMana = 0;
            attackSpeed = 0;
            hasDoubleAttack = false;
            equippedWeapon = null;
            equippedArmor = null;

            // 신규(DEC-129): 이 경로도 LevelUp()이 raw 기준선을 올리는 방식으로 통일했으므로,
            // 자기 자신의 초기 가시 스탯을 그대로 기준선으로 채워둔다(장비가 전혀 없으므로
            // RecomputeStats()를 호출해도 결과가 그대로 100/10/3이 나와야 한다 — 회귀 테스트 참조).
            baseHpNoWeapon = 100;
            baseAttackNoWeapon = 10;
            baseManaNoWeapon = 0;
            baseDefenseNoArmor = 3;
        }

        /// <summary>
        /// 신규: CharacterClass의 시작 스탯으로 초기화하는 생성자(DEC-102/111, ENT-101).
        /// 레벨/경험치/골드는 원본과 동일하게 1/0/0에서 시작한다.
        /// DEC-123: 마나/공격속도/쌍검 패시브도 이 시점에 CharacterClass(무기 보너스 반영 최종값)에서 복사한다.
        /// 마나는 캐릭터 생성 시 1회 가득 채워지고, 이후에는 전투 결과와 무관하게 이월된다(자동 풀회복 없음).
        /// DEC-129: 방어구는 시작 시 장착하지 않는다(맨몸) — 방어구는 이번에 새로 생긴 슬롯이라
        /// 직업별 기본 지급 개념이 없다(무기와 다르게 콘솔 원본에 대응 데이터가 전혀 없음).
        /// </summary>
        public Player(string playerName, CharacterClass characterClass) : this(playerName)
        {
            if (characterClass == null)
            {
                throw new ArgumentNullException(nameof(characterClass));
            }

            ClassId = characterClass.ClassId;

            // 신규(DEC-129): "장비 제외 기본 스탯"을 기준선으로 기억해두고, 시작 시 장착하는
            // 기본 무기(DefaultWeaponName)의 보너스를 EquipWeapon()으로 적용한다 — 결과값은
            // characterClass.BaseHp/BaseAttack/BaseMana/AttackSpeed/HasDoubleAttack(DEC-123 최종값)와
            // 정확히 같아야 한다(회귀 테스트로 검증). 방어구는 미착용 상태(equippedArmor==null)이므로
            // DEF는 baseDefenseNoArmor(=characterClass.BaseDefense) 그대로 최종값이 된다.
            baseHpNoWeapon = characterClass.BaseHpRaw;
            baseAttackNoWeapon = characterClass.BaseAttackRaw;
            baseManaNoWeapon = characterClass.BaseManaRaw;
            baseDefenseNoArmor = characterClass.BaseDefense;

            EquipWeapon(WeaponDatabase.Get(characterClass.DefaultWeaponName));

            // 캐릭터 생성 시점엔 항상 HP/마나 전량으로 시작한다(기존 동작 그대로) — RecomputeStats()의
            // 델타 보정 계산도 이 시점엔 결과적으로 풀피/풀마나를 만들지만, 의도를 명확히 하기 위해
            // 명시적으로 한 번 더 채워둔다.
            hp = maxHp;
            mana = maxMana;
        }

        /// <summary>
        /// 신규(DEC-129): 무기를 장착(교체)한다. weaponDef가 null이면 방어적으로 아무 것도 하지
        /// 않는다(잘못된 이름으로 호출되는 경우에 대비). 실제 재계산은 RecomputeStats()가 담당한다.
        /// </summary>
        public void EquipWeapon(WeaponDefinition weaponDef)
        {
            if (weaponDef == null)
            {
                return;
            }

            equippedWeapon = weaponDef;
            RecomputeStats();
        }

        /// <summary>신규(DEC-129): 방어구를 장착(교체)한다. armorDef가 null이면 아무 것도 하지 않는다(해제는 UnequipArmor() 사용).</summary>
        public void EquipArmor(ArmorDefinition armorDef)
        {
            if (armorDef == null)
            {
                return;
            }

            equippedArmor = armorDef;
            RecomputeStats();
        }

        /// <summary>신규(DEC-129): 방어구를 벗는다(맨몸으로). 이미 맨몸이어도 안전하게 아무 효과 없이 끝난다.</summary>
        public void UnequipArmor()
        {
            equippedArmor = null;
            RecomputeStats();
        }

        /// <summary>
        /// 신규(DEC-129): 현재 장착한 무기+방어구 보너스를 raw 기준선에 합산해 최종 스탯을 다시
        /// 계산한다. maxHp/maxMana가 바뀐 만큼 현재 hp/mana도 같은 폭으로 이동시킨다(예: 장비를
        /// 바꿔 최대HP가 10 줄면 현재 HP도 10 줄어듦) — 장비 교체가 "공짜 전량 회복" 수단이 되지
        /// 않도록 하기 위함이다. ATK는 오직 무기(baseAttackNoWeapon + weapon.AttackDelta)로만
        /// 정해지고 방어구는 절대 관여하지 않는다 — 방어구는 DEF/공격속도/HP/마나에만 영향을 준다.
        /// </summary>
        private void RecomputeStats()
        {
            int weaponHpDelta = equippedWeapon?.HpDelta ?? 0;
            int weaponAttackDelta = equippedWeapon?.AttackDelta ?? 0;
            int weaponSpeedDelta = equippedWeapon?.SpeedDelta ?? 0;
            int weaponManaDelta = equippedWeapon?.ManaDelta ?? 0;
            bool weaponHasDoubleAttack = equippedWeapon?.HasDoubleAttack ?? false;

            int armorDefDelta = equippedArmor?.DefDelta ?? 0;
            int armorSpeedDelta = equippedArmor?.SpeedDelta ?? 0;
            int armorHpDelta = equippedArmor?.HpDelta ?? 0;
            int armorManaDelta = equippedArmor?.ManaDelta ?? 0;

            int newMaxHp = Math.Max(1, baseHpNoWeapon + weaponHpDelta + armorHpDelta);
            int newMaxMana = Math.Max(0, baseManaNoWeapon + weaponManaDelta + armorManaDelta);

            hp = Utils.Clamp(hp + (newMaxHp - maxHp), 1, newMaxHp);
            mana = Utils.Clamp(mana + (newMaxMana - maxMana), 0, newMaxMana);

            maxHp = newMaxHp;
            maxMana = newMaxMana;
            attack = Math.Max(1, baseAttackNoWeapon + weaponAttackDelta); // 방어구는 ATK에 절대 관여하지 않음
            defense = Math.Max(0, baseDefenseNoArmor + armorDefDelta); // 무기는 DEF에 절대 관여하지 않음
            attackSpeed = weaponSpeedDelta + armorSpeedDelta; // 둘 다 공격속도에는 관여(가중 갑옷 페널티 포함)
            hasDoubleAttack = weaponHasDoubleAttack; // 쌍검 패시브는 무기 전용 특성 — 방어구는 영향 없음
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

        /// <summary>
        /// ⚠️ 이 setter는 raw 기준선(baseAttackNoWeapon)을 거치지 않고 `attack` 필드를 직접 덮어쓴다 —
        /// 이후 EquipWeapon()/EquipArmor()가 RecomputeStats()를 호출하면 raw+장비 보너스로 값을
        /// 재계산해버려 이 setter로 준 보너스가 조용히 증발한다(review-verify-agent Major로 실제
        /// 발견됨 — 낡은 무기고 "장비를 챙긴다"의 ATK+4가 이후 무기 교체 시 사라지는 버그).
        /// 영구적인 ATK 보너스(장비와 무관하게 계속 유지돼야 하는 값)를 주려면 반드시
        /// AddPermanentAttackBonus()를 쓰고, 이 메서드는 순수 회귀 테스트(장비 시스템과 무관한
        /// bare Player로 값을 직접 세팅하는 용도)에서만 사용한다.
        /// </summary>
        public void SetAttack(int newAttack)
        {
            attack = Math.Max(1, newAttack);
        }

        /// <summary>
        /// 신규(DEC-129 Major 수정): 영구적인 ATK 보너스를 raw 기준선(baseAttackNoWeapon)에 더하고
        /// RecomputeStats()를 다시 태운다 — LevelUp()이 HP/ATK/DEF 보너스를 raw 기준선에 반영하는
        /// 것과 동일한 패턴이다. 낡은 무기고 "장비를 챙긴다"(ATK+4)처럼 "장비와 무관하게 영구히
        /// 유지돼야 하는" 보너스는 반드시 이 메서드로 줘야, 이후 무기/방어구를 교체해도 사라지지 않는다.
        /// </summary>
        public void AddPermanentAttackBonus(int amount)
        {
            baseAttackNoWeapon += amount;
            RecomputeStats();
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
        /// DEC-129: savedAttackSpeed/savedHasDoubleAttack/savedEquippedWeaponName/savedEquippedArmorName을
        /// 추가(맨 끝, 기본값 0/false/null/null — 하위호환용). 무기/방어구를 교체할 수 있게 되면서
        /// 공격속도·쌍검 패시브·장비 상태도 저장/복원이 필요해졌다. equippedArmor는 savedEquippedArmorName이
        /// 비어 있으면(구버전 세이브이거나, 실제로 맨몸인 경우) null로 둔다 — 이 신규 필드는 두 경우
        /// 모두 "맨몸"이 정답이라 마나 필드처럼 별도의 구버전 판별 로직이 필요 없다.
        ///
        /// ⚠️ raw 기준선 역산(DEC-129 핵심 버그 방지): 저장된 최종 스탯(레벨업 누적분이 포함돼 있을 수
        /// 있음)을 그대로 신뢰하는 대신, "저장된 최종값 - 현재 장착 중인 장비의 보너스"로 raw 기준선
        /// (baseHpNoWeapon/baseAttackNoWeapon/baseDefenseNoArmor/baseManaNoWeapon)을 역산해 다시 채운다.
        /// 이렇게 하지 않으면(즉 캐릭터 생성 시점의 class raw 값을 그대로 두면) 로드 후 무기/방어구를
        /// 한 번이라도 교체하는 순간 RecomputeStats()가 레벨업으로 쌓인 영구 보너스를 전부 날려버리는
        /// 회귀가 생긴다 — 레벨1 raw 기준으로 최종 스탯을 다시 계산해버리기 때문이다.
        /// </summary>
        public void LoadState(int savedHp, int savedMaxHp, int savedAttack, int savedDefense,
            int savedLevel, int savedExperience, int savedGold, string savedClassId = null,
            int savedMana = 0, int savedMaxMana = 0,
            int savedAttackSpeed = 0, bool savedHasDoubleAttack = false,
            string savedEquippedWeaponName = null, string savedEquippedArmorName = null)
        {
            level = Math.Max(1, savedLevel);
            experience = Math.Max(0, savedExperience);
            gold = Math.Max(0, savedGold);
            if (!string.IsNullOrEmpty(savedClassId))
            {
                ClassId = savedClassId;
            }

            if (!string.IsNullOrEmpty(savedEquippedWeaponName))
            {
                equippedWeapon = WeaponDatabase.Get(savedEquippedWeaponName) ?? equippedWeapon;
                attackSpeed = savedAttackSpeed;
                hasDoubleAttack = savedHasDoubleAttack;
            }
            equippedArmor = string.IsNullOrEmpty(savedEquippedArmorName) ? null : ArmorDatabase.Get(savedEquippedArmorName);

            maxHp = Math.Max(1, savedMaxHp);
            hp = Utils.Clamp(savedHp, 0, maxHp);
            attack = Math.Max(1, savedAttack);
            defense = Math.Max(0, savedDefense);

            int weaponHpDelta = equippedWeapon?.HpDelta ?? 0;
            int weaponAttackDelta = equippedWeapon?.AttackDelta ?? 0;
            int weaponManaDelta = equippedWeapon?.ManaDelta ?? 0;
            int armorHpDelta = equippedArmor?.HpDelta ?? 0;
            int armorDefDelta = equippedArmor?.DefDelta ?? 0;
            int armorManaDelta = equippedArmor?.ManaDelta ?? 0;

            baseHpNoWeapon = maxHp - weaponHpDelta - armorHpDelta;
            baseAttackNoWeapon = attack - weaponAttackDelta;
            baseDefenseNoArmor = defense - armorDefDelta;

            if (savedMaxMana > 0)
            {
                maxMana = savedMaxMana;
                mana = Utils.Clamp(savedMana, 0, maxMana);
                baseManaNoWeapon = maxMana - weaponManaDelta - armorManaDelta;
            }
            // savedMaxMana<=0(구버전 세이브)이면 baseManaNoWeapon은 캐릭터 생성 시 이미 설정된
            // class 기반 raw 값을 그대로 둔다(기존 마나 하위호환 원칙과 동일).
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

        /// <summary>
        /// src/Player.cpp의 levelUp 그대로: 레벨+1, 최대HP+20, 공격+3, 방어+1, HP 전량 회복.
        /// DEC-129: 이 영구 보너스를 maxHp/attack/defense에 직접 더하는 대신 raw 기준선
        /// (baseHpNoWeapon/baseAttackNoWeapon/baseDefenseNoArmor)에 더하고 RecomputeStats()를
        /// 다시 태운다 — 그래야 레벨업 이후 무기·방어구를 갈아입어도 이 보너스가 사라지지 않는다.
        /// </summary>
        public void LevelUp()
        {
            level++;
            baseHpNoWeapon += 20;
            baseAttackNoWeapon += 3;
            baseDefenseNoArmor += 1;
            RecomputeStats();
            hp = maxHp; // 레벨업은 항상 HP 전량 회복(기존 동작 그대로) — RecomputeStats의 델타 보정 대신 명시적으로 덮어씀.
        }
    }
}
