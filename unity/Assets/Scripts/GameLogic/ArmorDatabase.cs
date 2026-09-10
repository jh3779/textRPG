/*
 * ArmorDatabase.cs
 *
 * 📝 역할: 신규(DEC-129, Unity 한정 — 콘솔 정본에는 대응 개념 자체가 없음). 무기와는 완전히
 * 독립된 방어구 슬롯의 정본 데이터. WeaponDatabase.cs와 자매 클래스이지만 중요한 차이가 있다:
 *
 * - 방어구는 직업 전용이 아니라 범용이다(RequiredClassId 같은 필드가 아예 없음) — 무기는 검류/
 *   단검류/지팡이류로 직업이 갈리지만, 갑옷은 그냥 갑옷이라 어떤 직업이든 장착 가능하다.
 * - 방어구는 ATK에 절대 관여하지 않는다 — WeaponDefinition에는 AttackDelta가 있지만
 *   ArmorDefinition에는 그 필드 자체가 없다(실수로라도 ATK를 바꿀 수 없게 타입 레벨에서 차단).
 *   방어구가 관여하는 스탯은 DEF·공격속도·HP·마나뿐이다(Player.RecomputeStats 참조).
 * - 무기 슬롯과 방어구 슬롯은 서로 완전히 독립적으로 장착/교체된다(Player가 equippedWeapon과
 *   equippedArmor를 별도 필드로 갖는다).
 *
 * 방어구 2종(설계 확정, DEC-129 — docs/06_open_questions.md 참조):
 *   가죽 갑옷(기본): DEF+3/공격속도+0/HP+8/마나+0 — 무기고에서 저렴하게 구매 가능.
 *   강화 판금 갑옷(상위): DEF+7/공격속도-2/HP+15/마나-3 — 방어력·체력은 크게 늘지만 무거워서
 *     공격속도·마나에 페널티(트레이드오프). 던전 수호자 처치 시 확정 드롭, 무기고에서도
 *     고가로 구매 가능.
 *
 * 게임 시작 시에는 어떤 직업이든 방어구를 하나도 장착하지 않은 "맨몸" 상태다(Player 생성자 참조) —
 * 방어구는 이번에 새로 생긴 슬롯이라 무기처럼 "직업별 기본 지급"이 필요 없고, 그 덕분에
 * "기존 시작 스탯은 절대 안 바뀐다"는 회귀 요구사항과도 자동으로 호환된다.
 */

using System.Collections.Generic;

namespace TextRPG.GameLogic
{
    /// <summary>
    /// 방어구 1종의 장착 보너스·구매 정보. DefDelta/SpeedDelta/HpDelta/ManaDelta는 전부
    /// "기본(장비 제외) 스탯에 더하는 보정치"다(음수 가능 — 판금 갑옷의 SpeedDelta/ManaDelta는 음수).
    /// 의도적으로 AttackDelta 필드가 없다 — 방어구는 ATK에 절대 영향을 줄 수 없다.
    /// </summary>
    public class ArmorDefinition
    {
        public string Name { get; }
        public int DefDelta { get; }
        public int SpeedDelta { get; }
        public int HpDelta { get; }
        public int ManaDelta { get; }
        public int ShopPrice { get; }
        public string Description { get; }

        public ArmorDefinition(string name, int defDelta, int speedDelta, int hpDelta, int manaDelta,
            int shopPrice, string description)
        {
            Name = name;
            DefDelta = defDelta;
            SpeedDelta = speedDelta;
            HpDelta = hpDelta;
            ManaDelta = manaDelta;
            ShopPrice = shopPrice;
            Description = description;
        }

        /// <summary>인벤토리에 넣을 새 Item 인스턴스를 만든다(구매/드롭/상자 획득 전부 이 메서드를 재사용).</summary>
        public Item CreateItem()
        {
            return new Item(Name, ItemType.ARMOR, System.Math.Max(1, DefDelta), ShopPrice, Description);
        }
    }

    public static class ArmorDatabase
    {
        public const string LeatherArmor = "가죽 갑옷";
        public const string ReinforcedPlateArmor = "강화 판금 갑옷";

        private static readonly Dictionary<string, ArmorDefinition> Definitions = new Dictionary<string, ArmorDefinition>
        {
            [LeatherArmor] = new ArmorDefinition(LeatherArmor,
                defDelta: 3, speedDelta: 0, hpDelta: 8, manaDelta: 0,
                shopPrice: 10,
                description: "무두질한 가죽으로 만든 가벼운 갑옷입니다. 크게 무겁지 않아 움직임을 방해하지 않습니다. " +
                    "(장착 보너스: DEF+3/HP+8)"),

            [ReinforcedPlateArmor] = new ArmorDefinition(ReinforcedPlateArmor,
                defDelta: 7, speedDelta: -2, hpDelta: 15, manaDelta: -3,
                shopPrice: WeaponDatabase.NewWeaponShopPrice,
                description: "쇠판을 겹겹이 덧댄 묵직한 판금 갑옷입니다. 방어력과 체력은 크게 늘지만 " +
                    "무게 때문에 움직임과 집중력이 둔해집니다. (장착 보너스: DEF+7/HP+15/공격속도-2/마나-3)"),
        };

        public static ArmorDefinition Get(string armorName)
        {
            return armorName != null && Definitions.TryGetValue(armorName, out var def) ? def : null;
        }

        public static IEnumerable<ArmorDefinition> All()
        {
            yield return Definitions[LeatherArmor];
            yield return Definitions[ReinforcedPlateArmor];
        }
    }
}
