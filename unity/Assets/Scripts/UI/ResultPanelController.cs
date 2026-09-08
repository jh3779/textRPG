/*
 * ResultPanelController.cs
 *
 * 📝 역할: SCR-007(게임 오버)·SCR-008(승리) 공용 패널.
 * docs/03_screen_contract.md: 승패 데이터만 다르게 바인딩(unity-mapping.html M-03 ResultPanel).
 * 금지 사항 동일 적용: 즉시 재시작 금지 — "타이틀로 돌아가기"만 제공하고,
 * 새 게임은 반드시 타이틀에서 다시 눌러야 한다.
 *
 * DEC-121/DEC-127: 서술 텍스트(description)는 QuillRevealText로 "깃펜으로 기록되는" 연출을
 * 적용한다 — 결과 화면이 이 프로젝트에서 유일하게 항상 존재하는 "서술형 텍스트 화면"이라
 * (전용 QuestPanel이 아직 없음, unity-mapping.html/06_open_questions.md 참조) 여기에 적용했다.
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
        [SerializeField] private QuillRevealText descriptionQuill; // DEC-127 신규 — null이면 즉시 텍스트만 표시
        [SerializeField] private TMP_Text descriptionText; // descriptionQuill이 없는 구 씬을 위한 폴백
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
            string description = victory
                ? "던전의 주인을 물리치고 던전을 클리어했습니다."
                : "체력이 0이 되어 모험이 끝났습니다.";

            if (descriptionQuill != null)
            {
                descriptionQuill.Play(description);
            }
            else if (descriptionText != null)
            {
                descriptionText.text = description;
            }

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
