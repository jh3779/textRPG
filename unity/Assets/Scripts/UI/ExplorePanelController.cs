/*
 * ExplorePanelController.cs
 *
 * 📝 역할: SCR-003 탐색 화면(docs/03_screen_contract.md SCR-003) +
 * 전투(SCR-004)를 같은 fullbleed 레이아웃 안에서 처리하는 최소 버전.
 *
 * 정본 레이아웃: docs/design-system/wireframes.html S-003 (.fullbleed 배경 + .statusline +
 * .textbar + .btnrow .btn-light 버튼). 던전 보드 타일(C-06)·대립 구도(VersusStage, C-11) 같은
 * 고급 비주얼은 이번 최소 골격 범위 밖 — 텍스트/버튼 기반으로 동일한 정보를 보여준다.
 *
 * 전투는 별도 BattlePanel을 새로 만들지 않고, 같은 패널 안에서 상태만 바꿔 그린다
 * (버튼이 "공격한다"/"도망친다"로, 본문이 전투 로그로 바뀜) — 콘솔 버전처럼 화면 전환
 * 없이 같은 화면 흐름 안에서 전투가 이어지는 구조를 그대로 반영한 것이다.
 */

using System;
using System.Collections.Generic;
using TextRPG.GameLogic;
using TextRPG.Persistence;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TextRPG.UI
{
    public class ExplorePanelController : MonoBehaviour
    {
        [Serializable]
        public class LocationArt
        {
            public string locationName;
            public Sprite sprite;
        }

        [SerializeField] private GameBootstrap bootstrap;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private TMP_Text statusLineText;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private Transform buttonRow;
        [SerializeField] private Button buttonTemplate; // 비활성 오브젝트로 두고 복제해서 사용
        [SerializeField] private List<LocationArt> locationArt = new List<LocationArt>();

        [Header("상태 확인 오버레이 (SCR-005/006 최소 버전)")]
        [SerializeField] private GameObject statusOverlayRoot;
        [SerializeField] private TMP_Text statusOverlayText;
        [SerializeField] private Button statusOverlayCloseButton;

        private readonly List<GameObject> spawnedButtons = new List<GameObject>();

        private void Awake()
        {
            buttonTemplate.gameObject.SetActive(false);
            statusOverlayCloseButton.onClick.AddListener(HideStatusOverlay);
        }

        public void Refresh()
        {
            HideStatusOverlay();
            var session = bootstrap.Session;

            // "이어하기"로 위치 3에 착지했는데 아직 고블린을 안 잡았다면 즉시 전투(원본과 동일 동작).
            session.ProcessLocationEntry();

            if (session.CurrentState == GameState.GAME_OVER || session.CurrentState == GameState.VICTORY)
            {
                bootstrap.ShowResult();
                return;
            }

            if (session.CurrentState == GameState.BATTLE)
            {
                RenderBattle("");
                return;
            }

            session.IncrementRound();
            RenderLocation();
        }

        private void RenderLocation()
        {
            var session = bootstrap.Session;
            var location = session.Map.GetCurrentLocation();

            SetBackground(location.Name);
            statusLineText.text =
                $"[라운드 {session.GameRound}] {CharacterClassDatabase.Get(session.Player.ClassId)?.DisplayName ?? session.Player.GetName()} " +
                $"HP {session.Player.GetHp()}/{session.Player.GetMaxHp()} · ATK {session.Player.GetAttack()} · Gold {session.Player.GetGold()}";
            titleText.text = location.Name;
            bodyText.text = location.Description;

            var choices = session.GetLocationChoices();
            ClearButtons();
            for (int i = 0; i < choices.Count; i++)
            {
                int choiceNumber = i + 1;
                CreateButton($"{choiceNumber}. {choices[i]}", () => OnLocationChoice(choiceNumber));
            }
        }

        private void OnLocationChoice(int choice)
        {
            var session = bootstrap.Session;
            var result = session.ChooseLocationAction(choice);

            switch (result)
            {
                case LocationActionResult.ShowStatus:
                    ShowStatusOverlay();
                    break;
                case LocationActionResult.SaveAndQuit:
                    SaveSystem.Save(session);
                    bootstrap.ShowTitle();
                    break;
                case LocationActionResult.Moved:
                    Refresh();
                    break;
                case LocationActionResult.None:
                default:
                    break;
            }
        }

        private void RenderBattle(string lastLog)
        {
            var session = bootstrap.Session;
            var battle = session.CurrentBattle;
            var enemy = session.CurrentEnemy;

            SetBackground(session.Map.GetCurrentLocation().Name);
            statusLineText.text = $"--- 전투 {battle.Round}턴 ---";
            titleText.text = $"{session.Player.GetName()} VS {enemy.GetName()}";
            bodyText.text =
                $"{session.Player.GetName()} HP {session.Player.GetHp()}/{session.Player.GetMaxHp()}\n" +
                $"{enemy.GetName()} HP {enemy.GetHp()}/{enemy.GetMaxHp()}" +
                (string.IsNullOrEmpty(lastLog) ? "" : $"\n\n{lastLog}");

            ClearButtons();
            CreateButton("1. 공격한다", () => OnBattleAction(1));
            CreateButton("2. 도망친다", () => OnBattleAction(2));
        }

        private void OnBattleAction(int action)
        {
            var session = bootstrap.Session;
            var enemyName = session.CurrentEnemy.GetName();

            var result = session.ProcessBattleTurn(action);

            if (!result.HasValue)
            {
                RenderBattle(action == 1 ? $"{enemyName}에게 공격을 가했다!" : "도망에 실패했다!");
                return;
            }

            switch (result.Value)
            {
                case BattleResult.PLAYER_WIN:
                case BattleResult.PLAYER_LOSE:
                case BattleResult.PLAYER_FLEE:
                    Refresh();
                    break;
            }
        }

        private void SetBackground(string locationName)
        {
            foreach (var art in locationArt)
            {
                if (art.locationName == locationName)
                {
                    backgroundImage.sprite = art.sprite;
                    backgroundImage.enabled = art.sprite != null;
                    return;
                }
            }
            backgroundImage.enabled = false;
        }

        private void ShowStatusOverlay()
        {
            var session = bootstrap.Session;
            var p = session.Player;
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("[플레이어 상태]");
            sb.AppendLine($"이름: {p.GetName()}");
            sb.AppendLine($"레벨: {p.GetLevel()}");
            sb.AppendLine($"HP: {p.GetHp()}/{p.GetMaxHp()}");
            sb.AppendLine($"공격력: {p.GetAttack()}");
            sb.AppendLine($"방어력: {p.GetDefense()}");
            sb.AppendLine($"경험치: {p.GetExperience()}/{p.GetLevel() * 100}");
            sb.AppendLine($"골드: {p.GetGold()}");
            sb.AppendLine();
            sb.AppendLine($"[인벤토리] {session.Inventory.GetItemCount()}/{session.Inventory.GetCapacity()}");
            for (int i = 0; i < session.Inventory.GetItemCount(); i++)
            {
                var item = session.Inventory.GetItem(i);
                sb.AppendLine($"{i + 1}. {item.GetName()} [{item.GetTypeAsString()}] 효과 {item.GetValue()}");
            }

            statusOverlayText.text = sb.ToString();
            statusOverlayRoot.SetActive(true);
        }

        private void HideStatusOverlay()
        {
            if (statusOverlayRoot != null)
            {
                statusOverlayRoot.SetActive(false);
            }
        }

        private void ClearButtons()
        {
            foreach (var go in spawnedButtons)
            {
                Destroy(go);
            }
            spawnedButtons.Clear();
        }

        private void CreateButton(string label, UnityEngine.Events.UnityAction onClick)
        {
            var go = Instantiate(buttonTemplate.gameObject, buttonRow);
            go.SetActive(true);
            var button = go.GetComponent<Button>();
            var text = go.GetComponentInChildren<TMP_Text>();
            if (text != null) text.text = label;
            button.onClick.AddListener(onClick);
            spawnedButtons.Add(go);
        }
    }
}
