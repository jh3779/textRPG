/*
 * WeaponDatabase.cs
 *
 * 📝 역할: 신규(DEC-129, Unity 한정 — 콘솔 정본과는 별개). 직업별 무기 2종(기존 1종 + 신규 1종)의
 * 장착 보너스·구매 가격·설명을 한 곳에 모은 정본 데이터. DEC-123까지는 "직업 확정 시 무기 보너스가
 * 이미 합산된 최종 스탯"을 CharacterClassDatabase가 직접 하드코딩했지만, 이제 무기를 여러 개 보유하고
 * 교체할 수 있어야 하므로 무기 보너스 자체를 별도 테이블로 분리했다. CharacterClass는 "기본(무기 제외)
 * 스탯 + 기본 장착 무기"만 알고, Player.EquipWeapon()이 이 테이블을 조회해 실제 스탯을 재계산한다.
 *
 * 무기 목록(설계 확정, DEC-129 — docs/06_open_questions.md 참조):
 *   전사: 장검(기존, 보너스 없이 그대로 유지) / 대검(신규)
 *   도적: 단검(기존, "2자루 세트"라는 서사는 유지하되 장착 로직상으로는 단일 이름 "단검"이 곧
 *         그 보너스를 대표한다 — 인벤토리에 2자루가 있어도 "단검"이라는 이름 하나면 충분) / 독아 단검(신규)
 *   마법사: 지팡이(기존) / 수정 지팡이(신규)
 *
 * ShopPrice는 기존 무기(장검/단검/지팡이)에는 사실상 쓰이지 않는다(시작 시 무료 지급이라 구매 대상이
 * 아님 — GameSession.PurchaseNewWeapon()은 항상 GetNewWeaponForClass()만 조회한다). 신규 무기 3종은
 * 전부 20골드로 통일했다 — 근거는 GameSession.cs의 PurchaseNewWeapon 주석 참조(경제 제약 때문에
 * 오케스트레이터 제안 150~250골드에서 의도적으로 낮췄다).
 */

using System.Collections.Generic;

namespace TextRPG.GameLogic
{
    /// <summary>
    /// 무기 1종의 장착 보너스·구매 정보. HpDelta/AttackDelta/SpeedDelta/ManaDelta는 전부
    /// "기본(무기 제외) 스탯에 더하는 보정치"다(음수 가능 — 예: 단검류의 HpDelta는 음수).
    /// 방어력(DEF)에 영향을 주는 무기는 설계상 없다(DEC-129 확정 스탯표에 DEF 보너스가 없음).
    /// </summary>
    public class WeaponDefinition
    {
        public string Name { get; }
        public string RequiredClassId { get; }
        public int HpDelta { get; }
        public int AttackDelta { get; }
        public int SpeedDelta { get; }
        public int ManaDelta { get; }
        public bool HasDoubleAttack { get; }
        public int ShopPrice { get; }
        public string Description { get; }

        public WeaponDefinition(string name, string requiredClassId,
            int hpDelta, int attackDelta, int speedDelta, int manaDelta, bool hasDoubleAttack,
            int shopPrice, string description)
        {
            Name = name;
            RequiredClassId = requiredClassId;
            HpDelta = hpDelta;
            AttackDelta = attackDelta;
            SpeedDelta = speedDelta;
            ManaDelta = manaDelta;
            HasDoubleAttack = hasDoubleAttack;
            ShopPrice = shopPrice;
            Description = description;
        }

        /// <summary>인벤토리에 넣을 새 Item 인스턴스를 만든다(구매/드롭/상자 획득 전부 이 메서드를 재사용).</summary>
        public Item CreateItem()
        {
            return new Item(Name, ItemType.WEAPON, System.Math.Max(1, AttackDelta), ShopPrice, Description);
        }
    }

    public static class WeaponDatabase
    {
        public const string Longsword = "장검";
        public const string Greatsword = "대검";
        public const string Dagger = "단검";
        public const string VenomFangDagger = "독아 단검";
        public const string Staff = "지팡이";
        public const string CrystalStaff = "수정 지팡이";

        /// <summary>신규 무기 3종의 구매/드롭/상자 공용 가격(골드). 근거는 GameSession.PurchaseNewWeapon 참조.</summary>
        public const int NewWeaponShopPrice = 20;

        private static readonly Dictionary<string, WeaponDefinition> Definitions = new Dictionary<string, WeaponDefinition>
        {
            [Longsword] = new WeaponDefinition(Longsword, CharacterClassDatabase.WarriorId,
                hpDelta: 4, attackDelta: 3, speedDelta: -1, manaDelta: 0, hasDoubleAttack: false,
                shopPrice: 60,
                description: "전사가 사용하는 묵직한 장검입니다. (장착 보너스: HP+4/ATK+3/공격속도-1)"),

            [Greatsword] = new WeaponDefinition(Greatsword, CharacterClassDatabase.WarriorId,
                hpDelta: 8, attackDelta: 6, speedDelta: -3, manaDelta: 0, hasDoubleAttack: false,
                shopPrice: NewWeaponShopPrice,
                description: "장검보다 훨씬 무겁고 강력한 대검입니다. 느리지만 육중한 한 방을 꽂아 넣습니다. " +
                    "(장착 보너스: HP+8/ATK+6/공격속도-3)"),

            [Dagger] = new WeaponDefinition(Dagger, CharacterClassDatabase.RogueId,
                hpDelta: -5, attackDelta: 0, speedDelta: 3, manaDelta: 0, hasDoubleAttack: true,
                shopPrice: 40,
                description: "도적이 사용하는 가벼운 단검입니다. (2자루 동시 장착 보너스: 공격속도+3/HP-5, 기본 공격 2회 타격)"),

            [VenomFangDagger] = new WeaponDefinition(VenomFangDagger, CharacterClassDatabase.RogueId,
                hpDelta: -3, attackDelta: 3, speedDelta: 2, manaDelta: 0, hasDoubleAttack: true,
                shopPrice: NewWeaponShopPrice,
                description: "칼날에 독을 발라둔 도적용 단검입니다. 단검보다 무겁지만 훨씬 매섭습니다. " +
                    "(장착 보너스: 공격속도+2/HP-3/ATK+3, 기본 공격 2회 타격 유지)"),

            [Staff] = new WeaponDefinition(Staff, CharacterClassDatabase.MageId,
                hpDelta: 0, attackDelta: 2, speedDelta: 1, manaDelta: 2, hasDoubleAttack: false,
                shopPrice: 70,
                description: "마법사가 사용하는 지팡이입니다. (장착 보너스: 공격속도+1/ATK+2/마나+2)"),

            [CrystalStaff] = new WeaponDefinition(CrystalStaff, CharacterClassDatabase.MageId,
                hpDelta: 0, attackDelta: 4, speedDelta: -1, manaDelta: 5, hasDoubleAttack: false,
                shopPrice: NewWeaponShopPrice,
                description: "마력이 응집된 수정을 박아 넣은 지팡이입니다. 다루기는 조금 무겁지만 훨씬 강한 " +
                    "마력을 다룰 수 있습니다. (장착 보너스: ATK+4/마나+5/공격속도-1)"),
        };

        /// <summary>직업별로 이번에 새로 추가된 무기(구매/드롭/상자 획득의 대상이 되는 단 하나의 무기).</summary>
        private static readonly Dictionary<string, string> NewWeaponNameByClass = new Dictionary<string, string>
        {
            [CharacterClassDatabase.WarriorId] = Greatsword,
            [CharacterClassDatabase.RogueId] = VenomFangDagger,
            [CharacterClassDatabase.MageId] = CrystalStaff,
        };

        /// <summary>직업별 기존(시작) 무기 이름. CharacterClassDatabase의 DefaultWeaponName과 대응한다.</summary>
        private static readonly Dictionary<string, string> DefaultWeaponNameByClass = new Dictionary<string, string>
        {
            [CharacterClassDatabase.WarriorId] = Longsword,
            [CharacterClassDatabase.RogueId] = Dagger,
            [CharacterClassDatabase.MageId] = Staff,
        };

        public static WeaponDefinition Get(string weaponName)
        {
            return weaponName != null && Definitions.TryGetValue(weaponName, out var def) ? def : null;
        }

        /// <summary>이 직업이 구매/드롭/상자로 새로 얻을 수 있는 무기(직업당 정확히 1개). 모르는 직업이면 null.</summary>
        public static WeaponDefinition GetNewWeaponForClass(string classId)
        {
            return classId != null && NewWeaponNameByClass.TryGetValue(classId, out var name) ? Definitions[name] : null;
        }

        /// <summary>이 직업의 시작(기존) 무기. 모르는 직업이면 null.</summary>
        public static WeaponDefinition GetDefaultWeaponForClass(string classId)
        {
            return classId != null && DefaultWeaponNameByClass.TryGetValue(classId, out var name) ? Definitions[name] : null;
        }
    }
}
