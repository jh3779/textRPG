/*
 * ResultPanelController.cs
 *
 * 📝 역할: SCR-007(게임 오버)·SCR-008(승리) 공용 패널.
 * docs/03_screen_contract.md: 승패 데이터만 다르게 바인딩(unity-mapping.html M-03 ResultPanel).
 * 금지 사항 동일 적용: 즉시 재시작 금지 — "타이틀로 돌아가기"만 제공하고,
 * 새 게임은 반드시 타이틀에서 다시 눌러야 한다.
 */

using TextRPG.GameLogic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TextRPG.UI
{
    public class ResultPanelController : MonoBehaviour
    {
        [SerializeField] private GameBootstrap bootstrap;
        [SerializeField] private TMP_Text headlineText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text statsText;
        [SerializeField] private Button backToTitleButton;

        private void Awake()
        {
            backToTitleButton.onClick.AddListener(OnBackToTitleClicked);
        }

        public void Refresh()
        {
            var session = bootstrap.Session;
            var p = session.Player;
            bool victory = session.CurrentState == GameState.VICTORY;

            headlineText.text = victory ? "던전 클리어!" : "GAME OVER";
            descriptionText.text = victory
                ? "던전의 주인을 물리치고 던전을 클리어했습니다."
                : "체력이 0이 되어 모험이 끝났습니다.";

            string className = CharacterClassDatabase.Get(p.ClassId)?.DisplayName ?? "모험가";
            statsText.text = $"{className} · Lv{p.GetLevel()} · HP{p.GetHp()}/{p.GetMaxHp()} · " +
                $"Mana{p.GetMana()}/{p.GetMaxMana()} · ATK{p.GetAttack()} · Gold{p.GetGold()}";
        }

        private void OnBackToTitleClicked()
        {
            bootstrap.Session.ReturnToTitle();
            bootstrap.ShowTitle();
        }
    }
}
