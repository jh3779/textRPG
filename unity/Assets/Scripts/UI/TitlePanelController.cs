/*
 * TitlePanelController.cs
 *
 * 📝 역할: SCR-001 타이틀 화면. docs/03_screen_contract.md SCR-001.
 * "새 게임" → CLASS_SELECT, "이어하기" → 세이브 있으면 즉시 PLAYING(직업 복원, CLASS_SELECT 건너뜀).
 * 세이브가 없으면 이어하기 버튼을 비활성화한다(가시 의무 그대로).
 *
 * OQ-102(DEC-122, 2026-09-08): 기존 세이브가 있는 상태에서 "새 게임"을 누르면 곧바로
 * CLASS_SELECT로 넘어가지 않고, 덮어쓰기 확인 모달(overwriteConfirmRoot)을 먼저 띄운다.
 * "예"를 눌러야만 실제로 새 게임 플로우(BeginNewGameFlow → ShowClassSelect)가 진행되고,
 * "아니오"를 누르면 모달만 닫히고 타이틀 화면에 그대로 남는다(아무 것도 덮어쓰지 않음).
 * 실제 파일 덮어쓰기 자체는 이 시점이 아니라 ExplorePanelController의 "저장하고 종료"에서
 * SaveSystem.Save()가 호출될 때 일어나지만(콘솔 버전 SaveAndQuit 흐름 그대로 유지), 사용자
 * 입장에서는 "새 게임을 선택하는 시점"이 곧 "기존 진행을 포기하는 결정"이므로 그 시점에
 * 확인을 받는다.
 *
 * review-verify-agent Minor(2026-09-08): overwriteConfirmRoot 참조가 씬에서 끊어지면
 * (예: 수작업 씬 편집·프리팹 재생성 실수) "새 게임" 버튼이 콘솔 로그도 없이 완전히
 * 무반응 상태가 될 수 있었다. ShowOverwriteConfirm()이 실패를 bool로 알리고,
 * OnNewGameClicked()이 그 경우 Debug.LogError로 원인을 남긴 뒤 확인 없이 새 게임을
 * 바로 진행하는 폴백으로 처리해, "조용히 아무 일도 안 일어나는" 상태를 없앴다.
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

        [Header("OQ-102/DEC-122: 세이브 덮어쓰기 확인 모달")]
        [SerializeField] private GameObject overwriteConfirmRoot;
        [SerializeField] private Button overwriteConfirmYesButton;
        [SerializeField] private Button overwriteConfirmNoButton;

        private void Awake()
        {
            newGameButton.onClick.AddListener(OnNewGameClicked);
            continueButton.onClick.AddListener(OnContinueClicked);

            if (overwriteConfirmYesButton != null)
            {
                overwriteConfirmYesButton.onClick.AddListener(OnOverwriteConfirmYesClicked);
            }
            if (overwriteConfirmNoButton != null)
            {
                overwriteConfirmNoButton.onClick.AddListener(OnOverwriteConfirmNoClicked);
            }
        }

        public void Refresh(bool saveExists)
        {
            continueButton.interactable = saveExists;
            HideOverwriteConfirm();
        }

        private void OnNewGameClicked()
        {
            if (SaveSystem.SaveExists())
            {
                if (!ShowOverwriteConfirm())
                {
                    // 모달 참조가 끊어진 비정상 상태 — 버튼이 죽은 것처럼 보이게 두지 않고
                    // 안전한 폴백(확인 없이 기존 동작대로 새 게임 진행)으로 처리한다.
                    StartNewGame();
                }
                return;
            }

            StartNewGame();
        }

        private void OnOverwriteConfirmYesClicked()
        {
            HideOverwriteConfirm();
            StartNewGame();
        }

        private void OnOverwriteConfirmNoClicked()
        {
            // 취소: 모달만 닫고 타이틀 화면 그대로 유지. 세션/세이브 파일 어느 쪽도 건드리지 않는다.
            HideOverwriteConfirm();
        }

        private void StartNewGame()
        {
            bootstrap.Session.BeginNewGameFlow();
            bootstrap.ShowClassSelect();
        }

        /// <summary>
        /// 모달을 띄운다. overwriteConfirmRoot 참조가 끊어져 있으면(씬/프리팹 배선 누락)
        /// false를 반환한다 — 호출부(OnNewGameClicked)가 이 경우를 반드시 처리해서
        /// "버튼을 눌러도 아무 반응이 없는" 상태가 발생하지 않도록 해야 한다.
        /// </summary>
        private bool ShowOverwriteConfirm()
        {
            if (overwriteConfirmRoot == null)
            {
                Debug.LogError("[TitlePanelController] overwriteConfirmRoot 참조가 없습니다 " +
                    "(OQ-102/DEC-122 덮어쓰기 확인 모달 배선 누락) — 확인 없이 새 게임을 바로 진행합니다.");
                return false;
            }

            overwriteConfirmRoot.SetActive(true);
            return true;
        }

        private void HideOverwriteConfirm()
        {
            if (overwriteConfirmRoot != null)
            {
                overwriteConfirmRoot.SetActive(false);
            }
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
