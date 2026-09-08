/*
 * CharacterClassDatabase.cs
 *
 * 📝 역할: 직업 3종(전사/도적/마법사)의 고정 데이터를 담는다.
 *
 * 2026-09-08 개편(DEC-123, Unity 한정): 사용자가 확정한 새 전투 시스템 설계를 반영한다.
 * - 기본 ATK 변경: 전사 12→10, 도적 14→16, 마법사 17(변경 없음).
 * - 무기 보너스(신규, 처음으로 장비가 스탯에 영향): 장검(HP+4/ATK+3/공격속도-1),
 *   단검 2자루(공격속도+3/HP-5/기본 공격 2회 타격), 지팡이(공격속도+1/ATK+2/마나+2).
 *   BaseHp/BaseAttack/BaseMana/AttackSpeed는 전부 "기본 스탯 + 무기 보너스"를 더한 최종값이다
 *   (아래 각 항목에 계산식을 주석으로 남겨 코드로 재계산·검증 가능하게 했다).
 * - 마나 스탯 신규 추가 및 직업별 마나 소모 스킬 3종(강타/맹독 일격/화염구).
 *
 * 최종 결과(검증용, RegressionSmokeTest.TestCharacterClassStats와 대조):
 *   전사   HP114 ATK13 DEF5 Mana10 AtkSpd-1
 *   도적   HP85  ATK16 DEF2 Mana15 AtkSpd+3 (기본 공격 2회 타격)
 *   마법사 HP75  ATK19 DEF1 Mana32 AtkSpd+1
 *
 * ⚠️ 시작 아이템(장검/단검 2자루/지팡이)의 정확한 value/price/description 수치는
 * 콘솔 C++ 원본에 대응하는 데이터가 전혀 없다(원본에는 회복 물약류만 존재) — 이번 포팅에서
 * 새로 만든 값이다. 원본과 "동일해야 하는" 수치가 아니라 신규 콘텐츠이므로, 밸런스가
 * 필요하면 자유롭게 조정 가능하다. DEC-123부터는 이 3개 시작 무기가 실제로 스탯에 영향을
 * 준다(장착 개념을 범용으로 만들지 않고, 이 3종에 한해 하드코딩 수준으로만 반영 — 과설계 금지).
 */

using System.Collections.Generic;

namespace TextRPG.GameLogic
{
    public static class CharacterClassDatabase
    {
        public const string WarriorId = "warrior";
        public const string RogueId = "rogue";
        public const string MageId = "mage";

        /// <summary>신규(DEC-123): 탐색 화면의 "장비를 챙긴다"에서 획득하는 마나 회복 포션 이름.</summary>
        public const string ManaPotionName = "마나 물약";

        /// <summary>
        /// 신규(DEC-123): 전투 중 "마나 회복" 행동이 소모하는 재료 아이템 이름(CONSUMABLE).
        /// 마나가 아니라 이 재료를 소모한다 — 마나 회복 스킬이 마나를 쓰면 앞뒤가 안 맞기 때문.
        /// </summary>
        public const string ManaRecoveryMaterialName = "마나 결정";

        private static readonly Dictionary<string, CharacterClass> Classes = new Dictionary<string, CharacterClass>
        {
            // 전사: 기본 ATK 12→10(DEC-123), 장검(HP+4/ATK+3/공격속도-1) 장착.
            // 최종 HP=110+4=114, ATK=10+3=13, DEF=5(변경 없음), Mana=10(무기 보너스 없음), AtkSpd=0-1=-1.
            [WarriorId] = new CharacterClass(
                WarriorId, "전사", "char_전사_상반신.png",
                baseHp: 110 + 4, baseAttack: 10 + 3, baseDefense: 5,
                baseMana: 10, attackSpeed: 0 - 1, hasDoubleAttack: false,
                manaSkill: ManaSkillType.PowerStrike, manaSkillName: "강타", manaSkillCost: 5,
                startingItemsFactory: () => new List<Item>
                {
                    new Item("장검", ItemType.WEAPON, 8, 60,
                        "전사가 사용하는 묵직한 장검입니다. (장착 보너스: HP+4/ATK+3/공격속도-1)")
                }),

            // 도적: 기본 ATK 14→16(DEC-123), 단검 2자루(공격속도+3/HP-5/기본 공격 2회 타격) 장착.
            // 최종 HP=90-5=85, ATK=16(무기 자체 ATK 보너스 없음), DEF=2(변경 없음), Mana=15, AtkSpd=0+3=3.
            [RogueId] = new CharacterClass(
                RogueId, "도적", "char_도적_상반신.png",
                baseHp: 90 - 5, baseAttack: 16, baseDefense: 2,
                baseMana: 15, attackSpeed: 0 + 3, hasDoubleAttack: true,
                manaSkill: ManaSkillType.PoisonStrike, manaSkillName: "맹독 일격", manaSkillCost: 7,
                startingItemsFactory: () => new List<Item>
                {
                    new Item("단검", ItemType.WEAPON, 5, 40,
                        "도적이 사용하는 가벼운 단검입니다. (2자루 동시 장착 보너스: 공격속도+3/HP-5, 기본 공격 2회 타격)"),
                    new Item("단검", ItemType.WEAPON, 5, 40,
                        "도적이 사용하는 가벼운 단검입니다. (2자루 동시 장착 보너스: 공격속도+3/HP-5, 기본 공격 2회 타격)")
                }),

            // 마법사: 기본 ATK 17(변경 없음), 지팡이(공격속도+1/ATK+2/마나+2) 장착.
            // 최종 HP=75(변경 없음), ATK=17+2=19, DEF=1(변경 없음), Mana=30(기본)+2=32, AtkSpd=0+1=1.
            [MageId] = new CharacterClass(
                MageId, "마법사", "char_마법사_상반신.png",
                baseHp: 75, baseAttack: 17 + 2, baseDefense: 1,
                baseMana: 30 + 2, attackSpeed: 0 + 1, hasDoubleAttack: false,
                manaSkill: ManaSkillType.Fireball, manaSkillName: "화염구", manaSkillCost: 10,
                startingItemsFactory: () => new List<Item>
                {
                    new Item("지팡이", ItemType.WEAPON, 10, 70,
                        "마법사가 사용하는 지팡이입니다. (장착 보너스: 공격속도+1/ATK+2/마나+2)")
                }),
        };

        /// <summary>화면 표시 순서(전사·도적·마법사, 와이어프레임 순서 그대로).</summary>
        public static readonly string[] OrderedClassIds = { WarriorId, RogueId, MageId };

        public static CharacterClass Get(string classId)
        {
            return Classes.TryGetValue(classId, out var result) ? result : null;
        }

        public static IEnumerable<CharacterClass> All()
        {
            foreach (var id in OrderedClassIds)
            {
                yield return Classes[id];
            }
        }
    }
}
