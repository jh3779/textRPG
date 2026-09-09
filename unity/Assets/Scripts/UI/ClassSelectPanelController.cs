/*
 * ClassSelectPanelController.cs
 *
 * 📝 역할: SCR-002 직업 선택 화면. docs/03_screen_contract.md SCR-002.
 * 직업 카드 3장(이름/초상화/HP·ATK·DEF/시작 아이템 미리보기) + "확정" 버튼.
 * 상태별 표현: 아무 것도 선택 안 하면 "확정" 버튼 비활성(INV-01).
 *
 * 선택된 카드는 배경색을 살짝 밝게 바꾸는 것에 더해, 카드 상단에 금색 핀 아이콘을 표시한다
 * (DEC-140 — 사용자 명시적 요청으로 DEC-118/DEC-127의 잉크마크 선택 연출을 이 화면에서만 대체).
 */

using System;
using System.Collections.Generic;
using System.Linq;
using TextRPG.GameLogic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TextRPG.UI
{
    public class ClassSelectPanelController : MonoBehaviour
    {
        [Serializable]
        public class ClassCard
        {
            public string classId;
            public Button selectButton;
            public Image cardBackground;
            public Image portraitImage;
            public TMP_Text nameText;
            public TMP_Text statsText;
            public TMP_Text itemsText;
            public GameObject selectionPin; // DEC-140 신규 — 선택 시 카드 상단에 나타나는 핀. null이면(구 씬) 표시만 생략
        }

        [SerializeField] private GameBootstrap bootstrap;
        [SerializeField] private List<ClassCard> cards = new List<ClassCard>();
        [SerializeField] private Button confirmButton;

        private void Awake()
        {
            foreach (var card in cards)
            {
                string id = card.classId;
                card.selectButton.onClick.AddListener(() => OnCardSelected(id));
            }
            confirmButton.onClick.AddListener(OnConfirmClicked);
        }

        public void Refresh()
        {
            foreach (var card in cards)
            {
                var cls = CharacterClassDatabase.Get(card.classId);
                if (cls == null) continue;

                card.nameText.text = cls.DisplayName;
                card.statsText.text =
                    $"HP {cls.BaseHp}\nATK {cls.BaseAttack}\nDEF {cls.BaseDefense}\n" +
                    $"Mana {cls.BaseMana}\n공격속도 {cls.AttackSpeed}";
                var items = cls.CreateStartingItems();
                card.itemsText.text = string.Join(", ", items.Select(i => i.GetName())
                    .GroupBy(n => n)
                    .Select(g => g.Count() > 1 ? $"{g.Key} {g.Count()}자루" : g.Key));
            }

            UpdateSelectionVisual(null);
            confirmButton.interactable = false;
        }

        private void OnCardSelected(string classId)
        {
            bootstrap.Session.SelectPendingClass(classId);
            UpdateSelectionVisual(classId);
            confirmButton.interactable = true;
        }

        private void UpdateSelectionVisual(string selectedClassId)
        {
            foreach (var card in cards)
            {
                bool selected = card.classId == selectedClassId;
                card.cardBackground.color = selected
                    ? new Color32(0xE8, 0xDC, 0xC0, 0xFF)
                    : new Color32(0xDC, 0xCD, 0xA6, 0xFF);

                // DEC-140: 잉크마크 애니메이션 대신 단순 활성화 토글만 한다(과설계 금지).
                if (card.selectionPin != null)
                {
                    card.selectionPin.SetActive(selected);
                }
            }
        }

        private void OnConfirmClicked()
        {
            if (bootstrap.Session.ConfirmClass())
            {
                bootstrap.ShowExplore();
            }
        }
    }
}
