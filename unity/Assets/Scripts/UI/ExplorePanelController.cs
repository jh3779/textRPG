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
                $"HP {session.Player.GetHp()}/{session.Player.GetMaxHp()} · ATK {session.Player.GetAttack()} · " +
                $"Mana {session.Player.GetMana()}/{session.Player.GetMaxMana()} · Gold {session.Player.GetGold()}";
            titleText.text = location.Name;
            bodyText.text = location.Description;

            var choices = session.GetLocationChoices();
            ClearButtons();
            for (int i = 0; i < choices.Count; i++)
            {
                int choiceNumber = i + 1;
                CreateButton($"{choiceNumber}. {choices[i]}", () => OnLocationChoice(choiceNumber));
            }

            // 신규(DEC-123): "아이템 사용"(POTION만) · "휴식하기"(마나 회복, 지역당 1회) —
            // 기존 지역별 번호 선택지 체계를 건드리지 않도록 별도의 항상-표시 버튼으로 추가한다.
            for (int i = 0; i < session.Inventory.GetItemCount(); i++)
            {
                var item = session.Inventory.GetItem(i);
                if (item.GetItemType() != ItemType.POTION)
                {
                    continue;
                }

                int index = i;
                string effect = item.GetName().Contains("마나") ? $"마나 +{item.GetValue()}" : $"HP +{item.GetValue()}";
                CreateButton($"아이템 사용: {item.GetName()} ({effect})", () => OnUseItemClicked(index));
            }

            if (session.CanRestHere())
            {
                CreateButton("휴식하기 (마나 회복)", OnRestClicked);
            }
        }

        private void OnUseItemClicked(int inventoryIndex)
        {
            bootstrap.Session.UseItem(inventoryIndex);
            RenderLocation();
        }

        private void OnRestClicked()
        {
            bootstrap.Session.Rest();
            RenderLocation();
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
                $"{session.Player.GetName()} HP {session.Player.GetHp()}/{session.Player.GetMaxHp()} · " +
                $"Mana {session.Player.GetMana()}/{session.Player.GetMaxMana()}\n" +
                $"{enemy.GetName()} HP {enemy.GetHp()}/{enemy.GetMaxHp()}" +
                (string.IsNullOrEmpty(lastLog) ? "" : $"\n\n{lastLog}");

            ClearButtons();
            CreateButton("1. 일반 공격", () => OnBattleAction(1));

            // 신규(DEC-123): 마나가 부족하면 버튼 자체를 노출하지 않는다("비활성화" 요구사항의 최소 구현).
            if (battle.CanUseManaSkill())
            {
                CreateButton($"3. {battle.GetManaSkillLabel()}", () => OnBattleAction(3));
            }

            // 신규(DEC-123): 재료("마나 결정")가 없으면 버튼을 노출하지 않는다.
            if (session.HasManaRecoveryMaterial())
            {
                CreateButton("4. 마나 회복 (재료 소모)", () => OnBattleAction(4));
            }

            CreateButton("2. 도망친다", () => OnBattleAction(2));
        }

        private void OnBattleAction(int action)
        {
            var session = bootstrap.Session;
            var enemyName = session.CurrentEnemy.GetName();

            BattleResult? result;

            if (action == BattleSystem.ActionRecoverMana)
            {
                if (!session.TryUseManaRecoverySkillInBattle(out result))
                {
                    // 재료가 없는 상태에서 버튼이 눌린 경우(방어적 처리) — 안내 후 다시 선택하게 한다.
                    RenderBattle("재료(마나 결정)가 없습니다.");
                    return;
                }
            }
            else
            {
                result = session.ProcessBattleTurn(action);
            }

            if (!result.HasValue)
            {
                RenderBattle(GetActionLog(action, enemyName));
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

        private static string GetActionLog(int action, string enemyName)
        {
            switch (action)
            {
                case BattleSystem.ActionAttack:
                    return $"{enemyName}에게 공격을 가했다!";
                case BattleSystem.ActionManaSkill:
                    return $"{enemyName}에게 마나 스킬을 사용했다!";
                case BattleSystem.ActionRecoverMana:
                    return "마나를 회복했다!";
                default:
                    return "도망에 실패했다!";
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
            sb.AppendLine($"마나: {p.GetMana()}/{p.GetMaxMana()}");
            sb.AppendLine($"공격력: {p.GetAttack()}");
            sb.AppendLine($"방어력: {p.GetDefense()}");
            sb.AppendLine($"공격속도: {p.GetAttackSpeed()}");
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
