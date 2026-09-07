/*
 * TitlePanelController.cs
 *
 * 📝 역할: SCR-001 타이틀 화면. docs/03_screen_contract.md SCR-001.
 * "새 게임" → CLASS_SELECT, "이어하기" → 세이브 있으면 즉시 PLAYING(직업 복원, CLASS_SELECT 건너뜀).
 * 세이브가 없으면 이어하기 버튼을 비활성화한다(가시 의무 그대로).
 */

using TextRPG.Persistence;
using UnityEngine;
using UnityEngine.UI;

namespace TextRPG.UI
{
    public class TitlePanelController : MonoBehaviour
    {
        [SerializeField] private GameBootstrap bootstrap;
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button continueButton;

        private void Awake()
        {
            newGameButton.onClick.AddListener(OnNewGameClicked);
            continueButton.onClick.AddListener(OnContinueClicked);
        }

        public void Refresh(bool saveExists)
        {
            continueButton.interactable = saveExists;
        }

        private void OnNewGameClicked()
        {
            bootstrap.Session.BeginNewGameFlow();
            bootstrap.ShowClassSelect();
        }

        private void OnContinueClicked()
        {
            if (SaveSystem.Load(bootstrap.Session))
            {
                bootstrap.ShowExplore();
            }
            else
            {
                Refresh(SaveSystem.SaveExists());
            }
        }
    }
}
