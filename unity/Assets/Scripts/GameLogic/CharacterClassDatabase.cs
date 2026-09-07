/*
 * CharacterClassDatabase.cs
 *
 * 📝 역할: 직업 3종(전사/도적/마법사)의 고정 데이터를 담는다.
 * 수치는 docs/06_open_questions.md OQ-103이 아직 "미결정"으로 남아있지만
 * docs/design-system/wireframes.html S-002(직업 선택 와이어프레임)에 이미 구체적으로
 * 쓰인 값을 그대로 사용한다(전사 HP110/ATK12/DEF5, 도적 HP90/ATK14/DEF2, 마법사 HP75/ATK17/DEF1).
 *
 * ⚠️ 시작 아이템(장검/단검 2자루/지팡이)의 정확한 value/price/description 수치는
 * 콘솔 C++ 원본에 대응하는 데이터가 전혀 없다(원본에는 회복 물약류만 존재) — 이번 포팅에서
 * 새로 만든 값이다. 원본과 "동일해야 하는" 수치가 아니라 신규 콘텐츠이므로, 밸런스가
 * 필요하면 자유롭게 조정 가능하다. 콘솔 버전에는 장비 "장착" 개념 자체가 없으므로
 * (Game.cpp 어디에도 무기 장착/자동 공격력 반영 로직이 없음) 이 아이템들도 인벤토리에
 * 들어가는 표시용 아이템일 뿐, Player.attack에 자동으로 반영되지 않는다(DEC-102 범위 유지).
 */

using System.Collections.Generic;

namespace TextRPG.GameLogic
{
    public static class CharacterClassDatabase
    {
        public const string WarriorId = "warrior";
        public const string RogueId = "rogue";
        public const string MageId = "mage";

        private static readonly Dictionary<string, CharacterClass> Classes = new Dictionary<string, CharacterClass>
        {
            [WarriorId] = new CharacterClass(
                WarriorId, "전사", "char_전사_상반신.png",
                baseHp: 110, baseAttack: 12, baseDefense: 5,
                startingItemsFactory: () => new List<Item>
                {
                    new Item("장검", ItemType.WEAPON, 8, 60, "전사가 사용하는 묵직한 장검입니다.")
                }),

            [RogueId] = new CharacterClass(
                RogueId, "도적", "char_도적_상반신.png",
                baseHp: 90, baseAttack: 14, baseDefense: 2,
                startingItemsFactory: () => new List<Item>
                {
                    new Item("단검", ItemType.WEAPON, 5, 40, "도적이 사용하는 가벼운 단검입니다."),
                    new Item("단검", ItemType.WEAPON, 5, 40, "도적이 사용하는 가벼운 단검입니다.")
                }),

            [MageId] = new CharacterClass(
                MageId, "마법사", "char_마법사_상반신.png",
                baseHp: 75, baseAttack: 17, baseDefense: 1,
                startingItemsFactory: () => new List<Item>
                {
                    new Item("지팡이", ItemType.WEAPON, 10, 70, "마법사가 사용하는 지팡이입니다.")
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
