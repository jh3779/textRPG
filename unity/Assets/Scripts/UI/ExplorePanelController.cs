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

        /// <summary>
        /// 신규(OQ-107 해결, DEC-124): 전투 중 표시할 적 초상화. Enemy.PortraitVariants[0](이제 후보 풀이
        /// 아니라 전투 시작 시 이미 확정된 단일 파일명, GameSession.StartBattleWithGoblin/StartBattleWithGuardian
        /// 참조)과 파일명으로 매칭한다. locationArt와 동일한 "이름→스프라이트" 매칭 패턴을 그대로 재사용했다
        /// (범용 스프라이트 리소스 시스템을 새로 만들지 않음 — 과설계 금지). 2026-09-08 갱신(DEC-125,
        /// OQ-108 해결): 던전 수호자 4개 변종도 이 매칭 리스트(enemyPortraitArt)에 추가됐다 — 코드 변경 없이
        /// ProjectSetupTool.BuildExplorePanel의 데이터 바인딩만 확장하면 되는 구조였다.
        /// </summary>
        [Serializable]
        public class PortraitArt
        {
            public string fileName;
            public Sprite sprite;
        }

        /// <summary>
        /// 신규(DEC-132): 전투 중 표시할 플레이어 캐릭터 전신 이미지. classId(warrior/rogue/mage)와
        /// 정확히 일치하는 항목을 찾아 표시한다 — enemyPortraitArt(파일명 매칭)와 달리 플레이어는
        /// 전투마다 바뀌지 않는 고정 3종(직업별 1장)이라 classId로 직접 매칭하는 게 더 단순하다.
        /// </summary>
        [Serializable]
        public class ClassPortraitArt
        {
            public string classId;
            public Sprite sprite;
        }

        /// <summary>
        /// 신규(DEC-137): 인벤토리/상점(무기고) 버튼에 표시할 아이템 아이콘. Item에는 아이콘 필드가
        /// 없으므로(게임 로직 변경 금지 — 순수 UI/비주얼 연결 작업) enemyPortraitArt/playerPortraitArt와
        /// 동일한 "이름→스프라이트" 매칭 패턴을 그대로 재사용한다. itemName은 WeaponDatabase/
        /// ArmorDatabase/CharacterClassDatabase의 이름 상수(Item.GetName()이 반환하는 값)와 정확히
        /// 일치해야 한다.
        /// </summary>
        [Serializable]
        public class ItemIconArt
        {
            public string itemName;
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

        [Header("적 초상화 (OQ-107/DEC-124 — 전투 중에만 표시)")]
        [SerializeField] private Image enemyPortraitImage; // 씬에 연결 안 된 환경(null)에서도 안전하게 동작해야 함
        [SerializeField] private List<PortraitArt> enemyPortraitArt = new List<PortraitArt>();

        [Header("플레이어 초상화 (DEC-132 — 전투 중에만 표시, 대립 구도 반대편)")]
        [SerializeField] private Image playerPortraitImage; // 씬에 연결 안 된 환경(null)에서도 안전하게 동작해야 함
        [SerializeField] private List<ClassPortraitArt> playerPortraitArt = new List<ClassPortraitArt>();

        [Header("전투 공격 이펙트 (DEC-133 — 피격 플래시·데미지 숫자 팝업, null이어도 전투 자체는 정상 진행)")]
        [SerializeField] private HitFlashEffect enemyHitFlash;
        [SerializeField] private HitFlashEffect playerHitFlash;
        [SerializeField] private DamagePopupText enemyDamagePopup;
        [SerializeField] private DamagePopupText playerDamagePopup;

        [Header("아이템 아이콘 (DEC-137 — 인벤토리/상점 버튼에 표시)")]
        [SerializeField] private List<ItemIconArt> itemIconArt = new List<ItemIconArt>();

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

        private void RenderLocation(string extraLog = null)
        {
            var session = bootstrap.Session;
            var location = session.Map.GetCurrentLocation();

            SetBackground(location.Name);
            HideEnemyPortrait(); // 신규(DEC-124): 탐색 화면에서는 적 초상화를 표시하지 않는다.
            HidePlayerPortrait(); // 신규(DEC-132): 탐색 화면에서는 플레이어 전투 비주얼도 표시하지 않는다.
            statusLineText.text =
                $"[라운드 {session.GameRound}] {CharacterClassDatabase.Get(session.Player.ClassId)?.DisplayName ?? session.Player.GetName()} " +
                $"HP {session.Player.GetHp()}/{session.Player.GetMaxHp()} · ATK {session.Player.GetAttack()} · " +
                $"DEF {session.Player.GetDefense()} · Mana {session.Player.GetMana()}/{session.Player.GetMaxMana()} · " +
                $"Gold {session.Player.GetGold()} · 무기 {session.Player.EquippedWeaponName} · " +
                $"방어구 {session.Player.EquippedArmorName ?? "없음"}";
            titleText.text = location.Name;
            bodyText.text = string.IsNullOrEmpty(extraLog) ? location.Description : $"{location.Description}\n\n{extraLog}";

            var choices = session.GetLocationChoices();
            ClearButtons();
            for (int i = 0; i < choices.Count; i++)
            {
                int choiceNumber = i + 1;
                CreateButton($"{choiceNumber}. {choices[i]}", () => OnLocationChoice(choiceNumber));
            }

            // 신규(DEC-129): 무기/방어구 "장착" — 인벤토리에 있는 장비 중 하나를 활성 장비로 지정한다.
            // 같은 이름이 여러 개(예: 도적의 "단검" 2자루)여도 이름 기준으로 한 번만 버튼을 만든다.
            CreateEquipButtons(session);

            // 신규(DEC-129): 무기고(지역 2)에서 직업 전용 신규 무기 + 방어구 2종을 구매할 수 있다.
            if (session.Map.GetCurrentLocationIndex() == 2 && session.CurrentState == GameState.PLAYING)
            {
                CreateArmoryPurchaseButtons(session);
            }

            // 신규(DEC-129): 갈림길(지역 1)에서 "상자를 조사한다" — 이번 회차 전체 1회 제한.
            if (session.CanInvestigateChestHere())
            {
                CreateButton("상자를 조사한다", OnInvestigateChestClicked);
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
                CreateButton($"아이템 사용: {item.GetName()} ({effect})", () => OnUseItemClicked(index),
                    icon: GetItemIcon(item.GetName()));
            }

            if (session.CanRestHere())
            {
                CreateButton("휴식하기 (마나 회복)", OnRestClicked);
            }
        }

        /// <summary>신규(DEC-129): 인벤토리의 무기/방어구를 이름 기준으로 중복 없이 나열해 장착 버튼을 만든다.</summary>
        private void CreateEquipButtons(GameSession session)
        {
            var shownWeaponNames = new HashSet<string>();
            var shownArmorNames = new HashSet<string>();

            for (int i = 0; i < session.Inventory.GetItemCount(); i++)
            {
                var item = session.Inventory.GetItem(i);

                if (item.GetItemType() == ItemType.WEAPON && shownWeaponNames.Add(item.GetName()))
                {
                    string weaponName = item.GetName();
                    bool isEquipped = weaponName == session.Player.EquippedWeaponName;
                    CreateButton(isEquipped ? $"장착됨: {weaponName}" : $"장착: {weaponName}",
                        () => OnEquipWeaponClicked(weaponName), interactable: !isEquipped, icon: GetItemIcon(weaponName));
                }
                else if (item.GetItemType() == ItemType.ARMOR && shownArmorNames.Add(item.GetName()))
                {
                    string armorName = item.GetName();
                    bool isEquipped = armorName == session.Player.EquippedArmorName;
                    CreateButton(isEquipped ? $"장착됨: {armorName}" : $"장착: {armorName}",
                        () => OnEquipArmorClicked(armorName), interactable: !isEquipped, icon: GetItemIcon(armorName));
                }
            }

            // 신규(DEC-129): 방어구를 장착 중이면 "해제"(맨몸으로) 버튼도 항상 보여준다.
            if (session.Player.EquippedArmorName != null)
            {
                CreateButton($"해제: {session.Player.EquippedArmorName}", OnUnequipArmorClicked,
                    icon: GetItemIcon(session.Player.EquippedArmorName));
            }
        }

        /// <summary>신규(DEC-129): 무기고(지역 2) 전용 — 직업 전용 신규 무기 1종 + 방어구 2종 구매 버튼.</summary>
        private void CreateArmoryPurchaseButtons(GameSession session)
        {
            var weaponDef = WeaponDatabase.GetNewWeaponForClass(session.Player.ClassId);
            if (weaponDef != null)
            {
                CreateButton($"구매: {weaponDef.Name} ({weaponDef.ShopPrice}골드)", OnPurchaseWeaponClicked,
                    icon: GetItemIcon(weaponDef.Name));
            }

            foreach (var armorDef in ArmorDatabase.All())
            {
                string armorName = armorDef.Name;
                int price = armorDef.ShopPrice;
                CreateButton($"구매: {armorName} ({price}골드)", () => OnPurchaseArmorClicked(armorName),
                    icon: GetItemIcon(armorName));
            }
        }

        private void OnEquipWeaponClicked(string weaponName)
        {
            bootstrap.Session.EquipWeaponByName(weaponName);
            RenderLocation();
        }

        private void OnEquipArmorClicked(string armorName)
        {
            bootstrap.Session.EquipArmorByName(armorName);
            RenderLocation();
        }

        private void OnUnequipArmorClicked()
        {
            bootstrap.Session.UnequipArmor();
            RenderLocation();
        }

        private void OnPurchaseWeaponClicked()
        {
            var result = bootstrap.Session.PurchaseNewWeapon();
            RenderLocation(DescribePurchaseWeaponResult(result));
        }

        private void OnPurchaseArmorClicked(string armorName)
        {
            var result = bootstrap.Session.PurchaseArmor(armorName);
            RenderLocation(DescribePurchaseArmorResult(result));
        }

        private static string DescribePurchaseWeaponResult(PurchaseWeaponResult result)
        {
            switch (result)
            {
                case PurchaseWeaponResult.Success:
                    return "무기를 구매했습니다!";
                case PurchaseWeaponResult.NotEnoughGold:
                    return "골드가 부족합니다.";
                case PurchaseWeaponResult.InventoryFull:
                    return "인벤토리가 가득 차 있습니다.";
                default:
                    return null;
            }
        }

        private static string DescribePurchaseArmorResult(PurchaseArmorResult result)
        {
            switch (result)
            {
                case PurchaseArmorResult.Success:
                    return "방어구를 구매했습니다!";
                case PurchaseArmorResult.NotEnoughGold:
                    return "골드가 부족합니다.";
                case PurchaseArmorResult.InventoryFull:
                    return "인벤토리가 가득 차 있습니다.";
                default:
                    return null;
            }
        }

        private void OnInvestigateChestClicked()
        {
            var result = bootstrap.Session.InvestigateChest();
            string message;
            switch (result)
            {
                case ChestResult.FoundWeapon:
                    message = "낡은 상자 안에서 쓸만한 무기를 발견했다!";
                    break;
                case ChestResult.FoundArmor:
                    message = "낡은 상자 안에서 가죽 갑옷을 발견했다!";
                    break;
                case ChestResult.FoundGoldAndMaterial:
                    message = "낡은 상자 안에서 약간의 금화와 재료를 발견했다!";
                    break;
                case ChestResult.FoundNothing:
                    message = "상자를 열어봤지만 아무것도 없었다...";
                    break;
                default:
                    message = null;
                    break;
            }
            RenderLocation(message);
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
            // 신규(OQ-107 해결, DEC-124): 전투 중에는 확정된 적 초상화를 표시한다. Enemy.PortraitVariants는
            // 이제 후보 풀이 아니라 GameSession이 전투 시작 시 이미 선택을 끝낸 단일 파일명이다.
            SetEnemyPortrait(enemy.PortraitVariants != null && enemy.PortraitVariants.Length > 0
                ? enemy.PortraitVariants[0]
                : null);
            // 신규(DEC-132): 전투 중에는 플레이어 전신 이미지도 반대편(좌측)에 표시해 대립 구도를 만든다
            // (07_visual_style.md DEC-116 — 이번에 처음 실제 구현됨, 06_open_questions.md DEC-132 참조).
            SetPlayerPortrait(session.Player.ClassId);
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

            // 신규(DEC-132): 전투 중 "가방" — 사용 가능한 포션이 하나도 없으면 버튼을 비활성화한다
            // (요구사항의 두 옵션 중 "비활성화" 방식을 택함 — 빈 가방을 열어보는 왕복을 줄이기 위함).
            CreateButton("5. 가방", OnOpenBattleBag, interactable: session.HasUsablePotion());

            CreateButton("2. 도망친다", () => OnBattleAction(2));
        }

        /// <summary>신규(DEC-132): "가방" 선택 — 인벤토리의 포션 목록을 보여주는 서브 메뉴로 전환한다.</summary>
        private void OnOpenBattleBag()
        {
            RenderBattleBag();
        }

        /// <summary>
        /// 신규(DEC-132): 전투 중 가방 서브 메뉴. 포션 사용은 턴을 소모하므로(밸런스 유지), 사용을
        /// 누르면 곧바로 OnUseBattleItemClicked로 진행하고, "뒤로"를 누르면 턴 소모 없이 원래 전투
        /// 행동 선택지로 돌아간다(RenderBattle 재호출 — 뒤로가기 자체는 턴이 아니다).
        /// </summary>
        private void RenderBattleBag()
        {
            var session = bootstrap.Session;
            var battle = session.CurrentBattle;
            var enemy = session.CurrentEnemy;

            statusLineText.text = $"--- 전투 {battle.Round}턴 · 가방 ---";
            titleText.text = "사용할 아이템을 선택하세요";
            bodyText.text =
                $"{session.Player.GetName()} HP {session.Player.GetHp()}/{session.Player.GetMaxHp()} · " +
                $"Mana {session.Player.GetMana()}/{session.Player.GetMaxMana()}\n" +
                $"{enemy.GetName()} HP {enemy.GetHp()}/{enemy.GetMaxHp()}";

            ClearButtons();
            bool any = false;
            for (int i = 0; i < session.Inventory.GetItemCount(); i++)
            {
                var item = session.Inventory.GetItem(i);
                if (item.GetItemType() != ItemType.POTION)
                {
                    continue;
                }

                any = true;
                int index = i;
                string effect = item.GetName().Contains("마나") ? $"마나 +{item.GetValue()}" : $"HP +{item.GetValue()}";
                CreateButton($"{item.GetName()} ({effect})", () => OnUseBattleItemClicked(index),
                    icon: GetItemIcon(item.GetName()));
            }

            if (!any)
            {
                bodyText.text += "\n\n사용할 아이템이 없습니다.";
            }

            CreateButton("뒤로", () => RenderBattle(""));
        }

        /// <summary>신규(DEC-132): 가방에서 포션을 골라 사용 — 성공하면 턴이 소모되어 적이 반격한다.</summary>
        private void OnUseBattleItemClicked(int inventoryIndex)
        {
            var session = bootstrap.Session;
            string itemName = session.Inventory.GetItem(inventoryIndex)?.GetName() ?? "아이템";

            // 신규(DEC-133): TryUseItemInBattle 호출 전 HP를 스냅샷해둔다 — 이 호출이 전투를
            // 끝낼 수도 있고(session.CurrentEnemy가 즉시 null이 됨), 아이템 효과+반격이 한
            // 메서드 안에서 한 번에 처리되므로 호출 전/후로만 순수하게 비교할 수 있다.
            var enemySnapshot = session.CurrentEnemy;
            int playerHpBefore = session.Player.GetHp();
            int enemyHpBefore = enemySnapshot != null ? enemySnapshot.GetHp() : 0;

            bool used = session.TryUseItemInBattle(inventoryIndex, out var itemResult, out var battleResult);
            if (!used || itemResult != ItemUseResult.Success)
            {
                RenderBattle("사용할 수 없는 아이템입니다.");
                return;
            }

            TriggerBattleEffects(playerHpBefore, enemyHpBefore, enemySnapshot);

            if (!battleResult.HasValue)
            {
                RenderBattle($"{itemName}을(를) 사용했다!");
                return;
            }

            switch (battleResult.Value)
            {
                case BattleResult.PLAYER_WIN:
                case BattleResult.PLAYER_LOSE:
                case BattleResult.PLAYER_FLEE:
                    Refresh();
                    break;
            }
        }

        private void OnBattleAction(int action)
        {
            var session = bootstrap.Session;
            var enemySnapshot = session.CurrentEnemy;
            var enemyName = enemySnapshot.GetName();

            // 신규(DEC-133): 행동 전 HP를 스냅샷해둔다 — BattleSystem의 데미지 계산 로직 자체는
            // 건드리지 않고, 행동 전/후 HP 차이만 UI 레이어에서 계산해 피격 이펙트를 트리거한다.
            int playerHpBefore = session.Player.GetHp();
            int enemyHpBefore = enemySnapshot.GetHp();

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

            TriggerBattleEffects(playerHpBefore, enemyHpBefore, enemySnapshot);

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

        /// <summary>
        /// 신규(DEC-133): 행동 전/후 HP를 비교해 피격 플래시(HitFlashEffect) + 데미지/회복 숫자
        /// 팝업(DamagePopupText)을 트리거한다. BattleSystem의 데미지 계산 로직·밸런스 수치는
        /// 전혀 건드리지 않고, 그 결과로 이미 바뀐 HP 차이만 UI 레이어에서 소비한다(지시사항 그대로).
        /// enemySnapshot은 호출부가 행동 직전에 미리 캡처해둔 참조를 그대로 받아야 한다 — 그
        /// 행동으로 전투가 끝나면 session.CurrentEnemy가 즉시 null로 바뀌므로(GameSession.
        /// ResolveBattleResult), 여기서 session.CurrentEnemy를 다시 읽으면 안 된다.
        /// </summary>
        private void TriggerBattleEffects(int playerHpBefore, int enemyHpBefore, Enemy enemySnapshot)
        {
            var session = bootstrap.Session;

            int playerDelta = playerHpBefore - session.Player.GetHp();
            if (playerDelta > 0)
            {
                playerHitFlash?.Flash();
                playerDamagePopup?.Show(playerDelta, isHeal: false);
            }
            else if (playerDelta < 0)
            {
                playerDamagePopup?.Show(-playerDelta, isHeal: true);
            }

            if (enemySnapshot != null)
            {
                int enemyDelta = enemyHpBefore - enemySnapshot.GetHp();
                if (enemyDelta > 0)
                {
                    enemyHitFlash?.Flash();
                    enemyDamagePopup?.Show(enemyDelta, isHeal: false);
                }
                else if (enemyDelta < 0)
                {
                    enemyDamagePopup?.Show(-enemyDelta, isHeal: true);
                }
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

        /// <summary>
        /// 신규(OQ-107 해결, DEC-124): portraitFileName(예: "enemy_고블린_거대형.png")과 정확히 일치하는
        /// enemyPortraitArt 항목을 찾아 표시한다. enemyPortraitImage가 씬에 연결돼 있지 않거나
        /// (인프라 부재 방어), 매칭되는 스프라이트가 없으면 조용히 숨기고 끝낸다 — 텍스트 기반 전투
        /// 자체는 항상 정상 동작해야 하므로 초상화 실패가 전투 진행을 막으면 안 된다.
        /// </summary>
        private void SetEnemyPortrait(string portraitFileName)
        {
            if (enemyPortraitImage == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(portraitFileName))
            {
                foreach (var art in enemyPortraitArt)
                {
                    if (art.fileName == portraitFileName)
                    {
                        enemyPortraitImage.sprite = art.sprite;
                        enemyPortraitImage.enabled = art.sprite != null;
                        return;
                    }
                }
            }

            enemyPortraitImage.enabled = false;
        }

        private void HideEnemyPortrait()
        {
            if (enemyPortraitImage != null)
            {
                enemyPortraitImage.enabled = false;
            }
        }

        /// <summary>
        /// 신규(DEC-132): classId(warrior/rogue/mage)와 정확히 일치하는 playerPortraitArt 항목을
        /// 찾아 표시한다. SetEnemyPortrait과 동일한 방어 패턴(인프라 부재/매칭 실패 시 조용히 숨김) —
        /// 초상화 실패가 전투 진행 자체를 막으면 안 된다.
        /// </summary>
        private void SetPlayerPortrait(string classId)
        {
            if (playerPortraitImage == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(classId))
            {
                foreach (var art in playerPortraitArt)
                {
                    if (art.classId == classId)
                    {
                        playerPortraitImage.sprite = art.sprite;
                        playerPortraitImage.enabled = art.sprite != null;
                        return;
                    }
                }
            }

            playerPortraitImage.enabled = false;
        }

        private void HidePlayerPortrait()
        {
            if (playerPortraitImage != null)
            {
                playerPortraitImage.enabled = false;
            }
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
            sb.AppendLine($"장착 무기: {p.EquippedWeaponName}");
            sb.AppendLine($"장착 방어구: {p.EquippedArmorName ?? "없음"}");
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

        private void CreateButton(string label, UnityEngine.Events.UnityAction onClick, bool interactable = true, Sprite icon = null)
        {
            var go = Instantiate(buttonTemplate.gameObject, buttonRow);
            go.SetActive(true);
            var button = go.GetComponent<Button>();
            var text = go.GetComponentInChildren<TMP_Text>();
            if (text != null) text.text = label;
            button.interactable = interactable; // 신규(DEC-129): "장착됨" 표시용 비활성 버튼 지원
            button.onClick.AddListener(onClick);

            // 신규(DEC-137): icon이 주어지면 버튼 왼쪽의 Icon Image(ProjectSetupTool.BuildExplorePanel이
            // ButtonTemplate에 미리 만들어 둠, 기본은 비활성)를 채우고, 라벨 텍스트가 아이콘과 겹치지
            // 않도록 왼쪽 여백을 넓힌다. icon이 null이면(대다수 버튼) 기존과 완전히 동일하게 동작한다.
            var iconImage = go.transform.Find("Icon")?.GetComponent<Image>();
            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
            }
            if (text != null)
            {
                var labelRT = text.rectTransform;
                labelRT.offsetMin = new Vector2(icon != null ? 44f : 0f, labelRT.offsetMin.y);
            }

            spawnedButtons.Add(go);
        }

        /// <summary>
        /// 신규(DEC-137): itemName과 정확히 일치하는 itemIconArt 항목의 스프라이트를 찾는다.
        /// 매칭 실패(itemIconArt가 비어있거나 이름이 없음)는 null을 반환해 CreateButton이 아이콘
        /// 없이 기존과 동일하게 동작하도록 한다 — 아이콘 부재가 인벤토리/상점 기능을 막으면 안 된다.
        /// </summary>
        private Sprite GetItemIcon(string itemName)
        {
            if (string.IsNullOrEmpty(itemName))
            {
                return null;
            }

            foreach (var art in itemIconArt)
            {
                if (art.itemName == itemName)
                {
                    return art.sprite;
                }
            }
            return null;
        }
    }
}
