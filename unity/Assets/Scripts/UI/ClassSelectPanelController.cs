/*
 * ClassSelectPanelController.cs
 *
 * 📝 역할: SCR-002 직업 선택 화면. docs/03_screen_contract.md SCR-002.
 * 직업 카드 3장(이름/초상화/HP·ATK·DEF/시작 아이템 미리보기) + "확정" 버튼.
 * 상태별 표현: 아무 것도 선택 안 하면 "확정" 버튼 비활성(INV-01).
 *
 * 잉크 마크 선택 애니메이션(DEC-118) 등 고급 디테일은 이번 스코프 밖 — 선택된 카드는
 * 배경색을 살짝 밝게 바꾸는 정도로만 표시한다.
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
