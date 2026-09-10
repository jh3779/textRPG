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
 *
 * 2026-09-08 확장(DEC-123, Unity 한정 — 콘솔 정본과는 별개, DEC-102/DEC-111 범위를 벗어남을
 * docs/06_open_questions.md에 명시): 무기 보너스가 반영된 최종 스탯(BaseHp/BaseAttack은 이미
 * 무기 보너스가 더해진 값 — CharacterClassDatabase 주석 참조), 신규 필드 BaseMana/AttackSpeed/
 * HasDoubleAttack, 그리고 직업별 마나 소모 스킬(ManaSkill/ManaSkillName/ManaSkillCost)을 추가한다.
 *
 * 2026-09-08 리팩터(DEC-129, Unity 한정): 직업별 신규 무기 추가 + 장착/교체 시스템을 위해
 * "기본 스탯 + 무기 보너스가 이미 합산된 최종값을 하드코딩" 구조를 "기본(무기 제외) 스탯 +
 * 현재 장착 무기의 보너스를 동적으로 합산"으로 리팩터링했다. BaseHpRaw/BaseAttackRaw/BaseManaRaw가
 * 새로운 "진짜" 저장 필드이고, 기존 이름(BaseHp/BaseAttack/BaseMana/AttackSpeed/HasDoubleAttack)은
 * "이 직업이 시작 시 장착하는 기본 무기(DefaultWeaponName)를 낀 최종값"을 매번 계산해 반환하는
 * 계산 프로퍼티로 바뀌었다 — 외부(ClassSelectPanelController, RegressionSmokeTest 등) 호출부는
 * 전혀 수정할 필요가 없고, 반환되는 최종 수치도 DEC-123 확정값과 정확히 동일하게 유지된다
 * (WeaponDatabase.cs에 각 직업의 기본 무기 보너스를 그대로 옮겨뒀기 때문 — 회귀 테스트로 검증).
 */

using System.Collections.Generic;

namespace TextRPG.GameLogic
{
    public class CharacterClass
    {
        public string ClassId { get; }
        public string DisplayName { get; }
        public string PortraitAsset { get; }

        /// <summary>신규(DEC-129): 무기 보너스를 제외한 기본 HP/ATK/마나. DEF는 어떤 무기도 건드리지 않으므로 그대로 최종값이다.</summary>
        public int BaseHpRaw { get; }
        public int BaseAttackRaw { get; }
        public int BaseDefense { get; }
        public int BaseManaRaw { get; }

        /// <summary>신규(DEC-129): 이 직업이 캐릭터 생성 시 자동으로 장착하는 기본(기존) 무기 이름. WeaponDatabase 참조.</summary>
        public string DefaultWeaponName { get; }

        private WeaponDefinition DefaultWeapon => WeaponDatabase.Get(DefaultWeaponName);

        /// <summary>기존 호출부 호환용 계산 프로퍼티 — "기본 스탯 + 기본 무기 보너스"의 최종값(DEC-123 수치와 동일).</summary>
        public int BaseHp => BaseHpRaw + (DefaultWeapon?.HpDelta ?? 0);
        public int BaseAttack => BaseAttackRaw + (DefaultWeapon?.AttackDelta ?? 0);
        public int BaseMana => BaseManaRaw + (DefaultWeapon?.ManaDelta ?? 0);

        /// <summary>신규(DEC-123): 공격속도(기본 무기 보너스 포함). 매 턴 이 값과 적의 공격속도를 비교해 선공을 정한다.</summary>
        public int AttackSpeed => DefaultWeapon?.SpeedDelta ?? 0;

        /// <summary>신규(DEC-123): 도적 쌍검 패시브 — true면 기본 공격이 1회가 아니라 2회 독립 타격한다(기본 무기 기준).</summary>
        public bool HasDoubleAttack => DefaultWeapon?.HasDoubleAttack ?? false;

        /// <summary>신규(DEC-123): 이 직업이 사용하는 마나 소모 스킬 종류.</summary>
        public ManaSkillType ManaSkill { get; }

        /// <summary>신규(DEC-123): 마나 스킬 화면 표시명(예: "강타").</summary>
        public string ManaSkillName { get; }

        /// <summary>신규(DEC-123): 마나 스킬 1회 사용 비용.</summary>
        public int ManaSkillCost { get; }

        // 💡 Item은 참조 타입이라 인스턴스를 그대로 공유하면 여러 플레이어가 같은 Item 객체를
        // 참조하게 된다(현재 Item은 사실상 불변이라 문제 없지만, 안전하게 팩토리 함수로
        // 매번 새 인스턴스를 만들어 반환한다).
        private readonly System.Func<List<Item>> startingItemsFactory;

        public CharacterClass(string classId, string displayName, string portraitAsset,
            int baseHpRaw, int baseAttackRaw, int baseDefense, int baseManaRaw,
            string defaultWeaponName,
            ManaSkillType manaSkill, string manaSkillName, int manaSkillCost,
            System.Func<List<Item>> startingItemsFactory)
        {
            ClassId = classId;
            DisplayName = displayName;
            PortraitAsset = portraitAsset;
            BaseHpRaw = baseHpRaw;
            BaseAttackRaw = baseAttackRaw;
            BaseDefense = baseDefense;
            BaseManaRaw = baseManaRaw;
            DefaultWeaponName = defaultWeaponName;
            ManaSkill = manaSkill;
            ManaSkillName = manaSkillName;
            ManaSkillCost = manaSkillCost;
            this.startingItemsFactory = startingItemsFactory;
        }

        public List<Item> CreateStartingItems() => startingItemsFactory();
    }
}
