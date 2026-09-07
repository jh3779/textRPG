/*
 * CharacterClass.cs
 *
 * 📝 역할: 신규 엔티티(콘솔 버전에는 없음). docs/04_data_model.md ENT-101 그대로.
 * classId / displayName / portraitAsset / baseHp / baseAttack / baseDefense / startingItems.
 *
 * DEC-102: 스킬트리·성장 분기는 없음 — 시작 스탯 + 시작 아이템만 다르다(DEC-111).
 * OQ-103: 직업 수·이름·정확한 수치는 아직 "미결정"으로 문서에 남아있지만,
 * docs/design-system/wireframes.html S-002에 이미 구체적인 값(전사/도적/마법사)이
 * 쓰여 있어 이번 포팅에서는 그 값을 그대로 사용한다 — 사용자가 나중에 바꿀 수 있도록
 * OQ-103은 "잠정 확정(가이드 문서 기준)"으로만 표시하고 완전히 닫지 않는다(06_open_questions.md 참조).
 */

using System.Collections.Generic;

namespace TextRPG.GameLogic
{
    public class CharacterClass
    {
        public string ClassId { get; }
        public string DisplayName { get; }
        public string PortraitAsset { get; }
        public int BaseHp { get; }
        public int BaseAttack { get; }
        public int BaseDefense { get; }

        // 💡 Item은 참조 타입이라 인스턴스를 그대로 공유하면 여러 플레이어가 같은 Item 객체를
        // 참조하게 된다(현재 Item은 사실상 불변이라 문제 없지만, 안전하게 팩토리 함수로
        // 매번 새 인스턴스를 만들어 반환한다).
        private readonly System.Func<List<Item>> startingItemsFactory;

        public CharacterClass(string classId, string displayName, string portraitAsset,
            int baseHp, int baseAttack, int baseDefense, System.Func<List<Item>> startingItemsFactory)
        {
            ClassId = classId;
            DisplayName = displayName;
            PortraitAsset = portraitAsset;
            BaseHp = baseHp;
            BaseAttack = baseAttack;
            BaseDefense = baseDefense;
            this.startingItemsFactory = startingItemsFactory;
        }

        public List<Item> CreateStartingItems() => startingItemsFactory();
    }
}
