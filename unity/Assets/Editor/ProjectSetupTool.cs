/*
 * ProjectSetupTool.cs
 *
 * 📝 역할: 이번 최소 골격(SCR-001/002/003/007/008)을 실제 Canvas/프리팹으로 구성하는
 * 1회성 빌드 스크립트. 손으로 .unity/.prefab YAML을 직접 작성하는 대신 Unity Editor API로
 * 사람이 인스펙터에서 하는 작업(오브젝트 생성, 컴포넌트 부착, 참조 연결)을 그대로 코드로
 * 재현해 안전하게 씬 파일을 생성한다.
 *
 * 실행: Unity 메뉴 "TextRPG/1. Configure Art Import Settings" → "TextRPG/2. Build Main Scene"
 * 순서로 실행하거나, 배치 모드에서 -executeMethod TextRPG.EditorTools.ProjectSetupTool.ConfigureArtImportSettings
 * / BuildMainScene 로 실행한다. 이후 재실행하면 기존 Main.unity를 덮어쓰고 다시 생성한다
 * (수동으로 씬을 고친 뒤 이 스크립트를 다시 돌리면 그 수정 사항은 사라지니 주의).
 */

using System.IO;
using System.Linq;
using TextRPG.GameLogic;
using TextRPG.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TextRPG.EditorTools
{
    public static class ProjectSetupTool
    {
        private const string ArtRoot = "Assets/Art";
        private const string ScenePath = "Assets/Scenes/Main.unity";

        [MenuItem("TextRPG/1. Configure Art Import Settings")]
        public static void ConfigureArtImportSettings()
        {
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { ArtRoot });
            int count = 0;
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
                count++;
            }

            Debug.Log($"[ProjectSetupTool] 텍스처 {count}개를 Sprite(2D and UI)로 재설정했습니다.");
        }

        [MenuItem("TextRPG/2. Build Main Scene")]
        public static void BuildMainScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ---- EventSystem ----
            var eventSystemGO = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            // ---- Canvas ----
            var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 1000); // DEC-105: 데스크톱 16:10 근사
            scaler.matchWidthOrHeight = 0.5f;

            // ---- GameBootstrap ----
            var bootstrapGO = new GameObject("GameBootstrap", typeof(GameBootstrap));
            var bootstrap = bootstrapGO.GetComponent<GameBootstrap>();

            // ---- Panels ----
            var titlePanel = BuildTitlePanel(canvasGO.transform, bootstrap);
            var classSelectPanel = BuildClassSelectPanel(canvasGO.transform, bootstrap);
            var explorePanel = BuildExplorePanel(canvasGO.transform, bootstrap);
            var resultPanel = BuildResultPanel(canvasGO.transform, bootstrap);

            var so = new SerializedObject(bootstrap);
            so.FindProperty("titlePanel").objectReferenceValue = titlePanel;
            so.FindProperty("classSelectPanel").objectReferenceValue = classSelectPanel;
            so.FindProperty("explorePanel").objectReferenceValue = explorePanel;
            so.FindProperty("resultPanel").objectReferenceValue = resultPanel;
            so.ApplyModifiedPropertiesWithoutUndo();

            titlePanel.gameObject.SetActive(true);
            classSelectPanel.gameObject.SetActive(false);
            explorePanel.gameObject.SetActive(false);
            resultPanel.gameObject.SetActive(false);

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath)!);
            EditorSceneManager.SaveScene(scene, ScenePath);

            var scenes = EditorBuildSettings.scenes.ToList();
            if (!scenes.Any(s => s.path == ScenePath))
            {
                scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }

            Debug.Log($"[ProjectSetupTool] 씬을 생성했습니다: {ScenePath}");
        }

        // ------------------------------------------------------------------
        // Title Panel (SCR-001)
        // ------------------------------------------------------------------
        private static TitlePanelController BuildTitlePanel(Transform canvasT, GameBootstrap bootstrap)
        {
            var root = CreateFullStretch("TitlePanel", canvasT);
            var bg = root.gameObject.AddComponent<Image>();
            bg.color = UIColors.Surface;
            AddBackgroundSprite(root, "material_나무", Color.white, 0.35f);

            var card = CreateAnchored("ParchmentCard", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(520, 640), Vector2.zero);
            var cardImg = card.gameObject.AddComponent<Image>();
            cardImg.color = new Color32(0xE8, 0xDC, 0xC0, 0xFF);
            AddBackgroundSprite(card, "material_양피지", Color.white, 1f);
            AddFoldLine(card); // DEC-121/DEC-127: 표지(큰 양피지 패널)에 중앙 접힘선
            AddFiligreeCorners(card, 72f); // DEC-117/DEC-120/DEC-138: 표지 모서리 필리그리 장식

            CreateText("Title", card, "DUNGEON GATE", 44, new Color32(0x3B, 0x33, 0x20, 0xFF), TextAlignmentOptions.Center,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(460, 80), new Vector2(0, -60));

            CreateText("Subtitle", card, "textRPG 콘솔판을 그대로 옮긴 다크 판타지 던전 TRPG",
                18, new Color32(0x5B, 0x4E, 0x33, 0xFF), TextAlignmentOptions.Center,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(440, 60), new Vector2(0, -150));

            var newGameBtn = CreateButton("NewGameButton", card, "새 게임", UIColors.Primary, Color.black,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(320, 56), new Vector2(0, 10));

            var continueBtn = CreateButton("ContinueButton", card, "이어하기",
                new Color32(0, 0, 0, 0), UIColors.OnSurfaceVariant,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(320, 56), new Vector2(0, -60));
            AddOutline(continueBtn.transform, UIColors.Primary);

            // ---- OQ-102/DEC-122: 세이브 덮어쓰기 확인 모달 ----
            var overwriteOverlay = CreateFullStretch("OverwriteConfirmOverlay", root);
            var overwriteScrim = overwriteOverlay.gameObject.AddComponent<Image>();
            overwriteScrim.color = new Color32(0x00, 0x00, 0x00, 200);

            var confirmCard = CreateAnchored("ConfirmCard", overwriteOverlay, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(440, 280), Vector2.zero);
            var confirmCardImg = confirmCard.gameObject.AddComponent<Image>();
            confirmCardImg.color = new Color32(0xE8, 0xDC, 0xC0, 0xFF);
            AddBackgroundSprite(confirmCard, "material_양피지", Color.white, 1f);

            CreateText("Headline", confirmCard, "기존 진행 상황을 덮어쓰시겠습니까?", 22, new Color32(0x3B, 0x33, 0x20, 0xFF),
                TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(380, 70), new Vector2(0, -30));

            CreateText("Body", confirmCard, "새 게임을 시작하면 저장된 모험가의 기록이 사라집니다.", 15,
                new Color32(0x5B, 0x4E, 0x33, 0xFF), TextAlignmentOptions.Center,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(380, 50), new Vector2(0, -110));

            var overwriteYesBtn = CreateButton("YesButton", confirmCard, "예, 새로 시작", UIColors.Primary, Color.black,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(190, 48), new Vector2(-100, 30));

            var overwriteNoBtn = CreateButton("NoButton", confirmCard, "아니오",
                new Color32(0, 0, 0, 0), new Color32(0x3B, 0x33, 0x20, 0xFF),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(150, 48), new Vector2(115, 30));
            AddOutline(overwriteNoBtn.transform, new Color32(0x8A, 0x7F, 0x68, 0xFF));

            overwriteOverlay.gameObject.SetActive(false);

            var controller = root.gameObject.AddComponent<TitlePanelController>();
            BindSerialized(controller,
                ("bootstrap", bootstrap),
                ("newGameButton", newGameBtn),
                ("continueButton", continueBtn),
                ("overwriteConfirmRoot", overwriteOverlay.gameObject),
                ("overwriteConfirmYesButton", overwriteYesBtn),
                ("overwriteConfirmNoButton", overwriteNoBtn));
            return controller;
        }

        // ------------------------------------------------------------------
        // Class Select Panel (SCR-002)
        // ------------------------------------------------------------------
        private static ClassSelectPanelController BuildClassSelectPanel(Transform canvasT, GameBootstrap bootstrap)
        {
            var root = CreateFullStretch("ClassSelectPanel", canvasT);
            var bg = root.gameObject.AddComponent<Image>();
            bg.color = UIColors.Surface;
            AddBackgroundSprite(root, "material_나무", Color.white, 0.35f);

            CreateText("Title", root, "직업을 선택하세요", 32, UIColors.Primary, TextAlignmentOptions.Center,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(600, 60), new Vector2(0, -40));

            string[] ids = { "warrior", "rogue", "mage" };
            string[] portraits = { "char_전사_상반신", "char_도적_상반신", "char_마법사_상반신" };
            var cards = new ClassSelectPanelController.ClassCard[3];

            float cardWidth = 320f;
            float gap = 24f;
            float totalWidth = cardWidth * 3 + gap * 2;
            float startX = -totalWidth / 2f + cardWidth / 2f;

            for (int i = 0; i < 3; i++)
            {
                var cardRoot = CreateAnchored($"Card_{ids[i]}", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(cardWidth, 520), new Vector2(startX + i * (cardWidth + gap), -20));
                var cardBg = cardRoot.gameObject.AddComponent<Image>();
                cardBg.color = new Color32(0xDC, 0xCD, 0xA6, 0xFF);
                AddFiligreeCorners(cardRoot, 56f); // DEC-117/DEC-120/DEC-138: 직업 카드 모서리 필리그리 장식

                var portraitRT = CreateAnchored("Portrait", cardRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(220, 260), new Vector2(0, -20));
                var portraitImg = portraitRT.gameObject.AddComponent<Image>();
                portraitImg.preserveAspect = true;
                var sprite = LoadSprite(portraits[i]);
                if (sprite != null) portraitImg.sprite = sprite;

                // DEC-131 후속 수정: Name(-290, h34 → span -307~-273)과 Stats(-330, h90 → span
                // -375~-285)가 22유닛 겹쳐서 "전사"/"HP114..."가 화면에서 서로 뒤엉켜 보이는 실제
                // 버그가 있었다(사용자가 재빌드본을 실행해 스크린샷으로 확인). 카드 상단(포트레이트
                // 하단 y=-150)부터 겹치지 않게 순서대로 재배치: Name(-180, span -197~-163) →
                // 8유닛 간격 → Stats(-250, span -295~-205) → 10유닛 간격 → Items(-325, span -345~-305).
                var nameText = CreateText("Name", cardRoot, ids[i], 22, new Color32(0x3B, 0x33, 0x20, 0xFF),
                    TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(280, 34), new Vector2(0, -180));

                var statsText = CreateText("Stats", cardRoot, "", 16, new Color32(0x3B, 0x33, 0x20, 0xFF),
                    TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(280, 90), new Vector2(0, -250));

                var itemsText = CreateText("Items", cardRoot, "", 14, new Color32(0x5B, 0x4E, 0x33, 0xFF),
                    TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(280, 40), new Vector2(0, -325));

                var selectBtn = cardRoot.gameObject.AddComponent<Button>();
                var targetGraphic = cardBg;
                selectBtn.targetGraphic = targetGraphic;

                var pin = BuildSelectionPin(cardRoot); // DEC-140: 선택 시 카드 상단에 나타나는 핀(잉크마크 대체)

                cards[i] = new ClassSelectPanelController.ClassCard
                {
                    classId = ids[i],
                    selectButton = selectBtn,
                    cardBackground = cardBg,
                    portraitImage = portraitImg,
                    nameText = nameText,
                    statsText = statsText,
                    itemsText = itemsText,
                    selectionPin = pin
                };
            }

            var confirmBtn = CreateButton("ConfirmButton", root, "확정", UIColors.Primary, Color.black,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(280, 56), new Vector2(0, 40));

            var controller = root.gameObject.AddComponent<ClassSelectPanelController>();
            var so = new SerializedObject(controller);
            so.FindProperty("bootstrap").objectReferenceValue = bootstrap;
            so.FindProperty("confirmButton").objectReferenceValue = confirmBtn;
            var cardsProp = so.FindProperty("cards");
            cardsProp.arraySize = cards.Length;
            for (int i = 0; i < cards.Length; i++)
            {
                var element = cardsProp.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("classId").stringValue = cards[i].classId;
                element.FindPropertyRelative("selectButton").objectReferenceValue = cards[i].selectButton;
                element.FindPropertyRelative("cardBackground").objectReferenceValue = cards[i].cardBackground;
                element.FindPropertyRelative("portraitImage").objectReferenceValue = cards[i].portraitImage;
                element.FindPropertyRelative("nameText").objectReferenceValue = cards[i].nameText;
                element.FindPropertyRelative("statsText").objectReferenceValue = cards[i].statsText;
                element.FindPropertyRelative("itemsText").objectReferenceValue = cards[i].itemsText;
                element.FindPropertyRelative("selectionPin").objectReferenceValue = cards[i].selectionPin;
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            return controller;
        }

        // ------------------------------------------------------------------
        // Explore Panel (SCR-003, 전투(SCR-004)도 같은 패널에서 처리)
        // ------------------------------------------------------------------
        private static ExplorePanelController BuildExplorePanel(Transform canvasT, GameBootstrap bootstrap)
        {
            var root = CreateFullStretch("ExplorePanel", canvasT);

            var bgImageRT = CreateFullStretch("Background", root);
            var bgImage = bgImageRT.gameObject.AddComponent<Image>();
            bgImage.color = Color.white;
            bgImage.preserveAspect = false;

            var scrimRT = CreateFullStretch("Scrim", root);
            var scrim = scrimRT.gameObject.AddComponent<Image>();
            scrim.color = UIColors.ScrimBottom;
            scrim.raycastTarget = false;

            // 신규(OQ-107 해결, DEC-124): 전투 중 적 초상화. 배경(Background)보다 위, 텍스트바/버튼보다는
            // 아래에 두어 대립 구도(VersusStage, C-11)의 최소 버전 역할만 한다 — 카드 프레임 등 고급
            // 비주얼은 이번 범위 밖. 평소(탐색 화면)에는 ExplorePanelController.HideEnemyPortrait()가 꺼둔다.
            //
            // DEC-131 후속 수정: 원래 anchoredPosition.y=150(높이 460, 하단 앵커(0.5,0), pivot 0.5)이면
            // 세로 범위가 [-80, 380]이라 하단 80유닛이 화면 아래로 잘려나가고, 그 위로 ButtonRow(y
            // 30~90, 배경이 완전 투명이라 뒤에 있는 것이 그대로 비쳐 보임)와 정면으로 겹쳤다(사용자가
            // 전투 화면 스크린샷으로 실제 확인·보고). y=330으로 올려 범위를 [100, 560]으로 만들면
            // ButtonRow 상단(90)보다 10유닛 위에서 끝나 더는 겹치지 않고, 화면 하단 클리핑도 없어진다.
            // 이 값은 EnemyPortrait 하나를 여러 적(고블린/던전 수호자 변종 포함)이 공유해서 쓰므로
            // (ExplorePanelController.SetEnemyPortrait()는 sprite만 갈아끼우고 위치는 안 건드림) 이
            // 한 곳만 고치면 모든 적 포트레이트에 동일하게 적용된다.
            //
            // 신규(DEC-132): DEC-116(07_visual_style.md)이 요구한 "플레이어와 몬스터가 그 자리에서
            // 맞붙는 대립 구도"가 지금까지 몬스터 포트레이트만 있고 플레이어 쪽이 아예 없어 실제로는
            // 구현되지 않은 상태였다(사용자가 빌드 스크린샷으로 확인). anchoredPosition.x를 0(중앙)에서
            // +280으로 옮겨 화면 오른쪽에 두고, 반대편(-280, 아래 PlayerPortrait)에 플레이어를 같은
            // 크기(360×460)로 세워 서로 마주보게 했다 — 참조 해상도 1600×1000 기준으로 [100,460]/
            // [-460,-100] 범위에 각각 들어가 화면 밖으로 벗어나지 않으면서도 중앙에 배경이 보이는
            // 여백(200유닛)을 남긴다.
            var enemyPortraitRT = CreateAnchored("EnemyPortrait", root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(360, 460), new Vector2(280, 330));
            var enemyPortraitImg = enemyPortraitRT.gameObject.AddComponent<Image>();
            enemyPortraitImg.preserveAspect = true;
            enemyPortraitImg.raycastTarget = false;
            enemyPortraitImg.enabled = false;

            // 신규(DEC-137): DEC-116(07_visual_style.md)이 요구한 "인물 이미지 가장자리 페이드"를
            // 처음 실제로 연결한다 — UIPortraitEdgeMask 커스텀 셰이더(TextRPG/UI/PortraitEdgeMask)를
            // Image.material로 꽂고, art-assets에 있던 마스크(mask_enemy_softedge.png)의 알파 채널로
            // 원본 알파를 곱해 가장자리를 페이드시킨다. 셰이더/마스크가 없으면(예: 아직 임포트 전)
            // CreatePortraitEdgeMaskMaterial()이 null을 반환해 조용히 건너뛰고 기존처럼 사각형 그대로
            // 보인다 — 마스킹 실패가 전투 진행을 막으면 안 된다.
            var enemyMaskMaterial = CreatePortraitEdgeMaskMaterial("mask_enemy_softedge");
            if (enemyMaskMaterial != null)
            {
                enemyPortraitImg.material = enemyMaskMaterial;
            }

            // 신규(DEC-132): 전투 중 플레이어 캐릭터 전신 이미지 — EnemyPortrait과 대칭(x=-280)으로 배치해
            // "대립 구도"를 만든다. 평소(탐색 화면)에는 ExplorePanelController.HidePlayerPortrait()가 꺼둔다.
            var playerPortraitRT = CreateAnchored("PlayerPortrait", root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(360, 460), new Vector2(-280, 330));
            var playerPortraitImg = playerPortraitRT.gameObject.AddComponent<Image>();
            playerPortraitImg.preserveAspect = true;
            playerPortraitImg.raycastTarget = false;
            playerPortraitImg.enabled = false;

            // 신규(DEC-137): PlayerPortrait에도 동일한 마스킹 셰이더를 적용한다(마스크만 mask_character_softedge.png로
            // 다름). DEC-132 시점 주석은 "EnemyPortrait에도 마스킹이 없어 동일하게 생략했다"고 적었지만,
            // 이번에 두 곳 모두 실제로 채워졌으므로 그 주석은 더 이상 사실이 아니다 — 06_open_questions.md
            // DEC-137에 이 이력을 정직하게 남긴다.
            var playerMaskMaterial = CreatePortraitEdgeMaskMaterial("mask_character_softedge");
            if (playerMaskMaterial != null)
            {
                playerPortraitImg.material = playerMaskMaterial;
            }

            // 신규(DEC-133): 전투 공격 이펙트 — 피격 플래시(HitFlashEffect)는 각 포트레이트 Image
            // 자신을 target으로 삼아 색만 잠깐 바꿨다 되돌린다(같은 GameObject에 부착).
            var enemyHitFlash = enemyPortraitRT.gameObject.AddComponent<HitFlashEffect>();
            BindSerialized(enemyHitFlash, ("target", enemyPortraitImg));

            var playerHitFlash = playerPortraitRT.gameObject.AddComponent<HitFlashEffect>();
            BindSerialized(playerHitFlash, ("target", playerPortraitImg));

            // 신규(DEC-133): 데미지/회복 숫자 팝업 — 각 포트레이트의 자식으로 붙여 그 근처에서
            // 나타났다 위로 떠오르며 사라지도록 한다. 평소엔 비활성(SetActive(false))으로 시작한다.
            var enemyDamagePopup = BuildDamagePopup(enemyPortraitRT, "EnemyDamagePopup");
            var playerDamagePopup = BuildDamagePopup(playerPortraitRT, "PlayerDamagePopup");

            var statusLine = CreateText("StatusLine", root, "[라운드 0]", 16, Color.white, TextAlignmentOptions.Left,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(-24, 30), new Vector2(0, -16));

            var textBar = CreateAnchored("TextBar", root, new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(-20, 140), new Vector2(0, 140));
            var textBarImg = textBar.gameObject.AddComponent<Image>();
            textBarImg.color = UIColors.TextBarBackground;

            var titleText = CreateText("LocationTitle", textBar, "", 20, UIColors.Primary, TextAlignmentOptions.TopLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(-24, 28), new Vector2(12, -10));

            var bodyText = CreateText("LocationBody", textBar, "", 16, UIColors.OnSurface, TextAlignmentOptions.TopLeft,
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(-24, -40), new Vector2(12, -10));

            var buttonRow = CreateAnchored("ButtonRow", root, new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(-20, 60), new Vector2(0, 60));
            var layout = buttonRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            layout.childAlignment = TextAnchor.MiddleLeft;

            var buttonTemplate = CreateButton("ButtonTemplate", buttonRow, "1. 선택지",
                new Color32(0, 0, 0, 0), UIColors.Primary,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(220, 44), Vector2.zero);
            AddOutline(buttonTemplate.transform, UIColors.Primary);

            // 신규(DEC-137): 아이템 아이콘(무기/방어구/포션) 버튼에 쓸 아이콘 자리. 기본은 비활성
            // (enabled=false)이라 아이콘이 없는 대다수 버튼(이동/선택지 등)은 기존과 완전히 동일하게
            // 보인다 — ExplorePanelController.CreateButton()이 아이콘이 있을 때만 sprite를 채우고
            // 활성화한다(범용 아이콘 시스템이 아니라 이 버튼 템플릿 하나에만 적용 — 과설계 금지).
            var iconRT = CreateAnchored("Icon", buttonTemplate.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(28, 28), new Vector2(24, 0));
            var iconImg = iconRT.gameObject.AddComponent<Image>();
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;
            iconImg.enabled = false;

            // ---- 상태 확인 오버레이 (SCR-005/006 최소 버전) ----
            var overlayRoot = CreateFullStretch("StatusOverlay", root);
            var overlayBg = overlayRoot.gameObject.AddComponent<Image>();
            overlayBg.color = new Color32(0x00, 0x00, 0x00, 200);

            var overlayCard = CreateAnchored("OverlayCard", overlayRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(560, 640), Vector2.zero);
            var overlayCardImg = overlayCard.gameObject.AddComponent<Image>();
            overlayCardImg.color = UIColors.SurfaceContainerHigh;

            var overlayText = CreateText("OverlayText", overlayCard, "", 16, UIColors.OnSurface, TextAlignmentOptions.TopLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(-40, -70), new Vector2(20, -20));

            var closeBtn = CreateButton("CloseButton", overlayCard, "닫기", UIColors.Primary, Color.black,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(160, 44), new Vector2(0, 16));

            overlayRoot.gameObject.SetActive(false);

            var controller = root.gameObject.AddComponent<ExplorePanelController>();
            BindSerialized(controller,
                ("bootstrap", bootstrap),
                ("backgroundImage", bgImage),
                ("statusLineText", statusLine),
                ("titleText", titleText),
                ("bodyText", bodyText),
                ("buttonRow", buttonRow),
                ("buttonTemplate", buttonTemplate),
                ("statusOverlayRoot", overlayRoot.gameObject),
                ("statusOverlayText", overlayText),
                ("statusOverlayCloseButton", closeBtn),
                ("enemyPortraitImage", enemyPortraitImg),
                ("playerPortraitImage", playerPortraitImg),
                ("enemyHitFlash", enemyHitFlash),
                ("playerHitFlash", playerHitFlash),
                ("enemyDamagePopup", enemyDamagePopup),
                ("playerDamagePopup", playerDamagePopup));

            // 지역별 배경 스프라이트(SCR-003 가시 의무: "현재 지역을 암시하는 배경 씬 일러스트")
            var so = new SerializedObject(controller);
            var artProp = so.FindProperty("locationArt");
            string[] names = { "던전 입구", "갈림길", "낡은 무기고", "어두운 통로", "보스의 방" };
            string[] files = { "scene_던전입구", "scene_갈림길", "scene_무기고", "scene_어두운통로", "scene_보스의방" };
            artProp.arraySize = names.Length;
            for (int i = 0; i < names.Length; i++)
            {
                var element = artProp.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("locationName").stringValue = names[i];
                element.FindPropertyRelative("sprite").objectReferenceValue = LoadSprite(files[i]);
            }

            // 신규(OQ-107 해결, DEC-124): 고블린 4개 변종 초상화. fileName은 GameSession이 전투 시작 시
            // Enemy.PortraitVariants[0]에 넣는 문자열(확장자 .png 포함)과 정확히 일치해야 한다.
            // 2026-09-08 갱신(DEC-125, OQ-108 해결): 던전 수호자 4개 변종(균형형·중장형·기동형·마도형)도
            // 같은 리스트에 이어 추가한다 — GameSession.StartBattleWithGuardian()이 조우 시점에 이 중
            // 하나를 랜덤 선택해 PortraitVariants[0]에 넣으므로(스탯은 항상 동일), 매칭 코드 변경 없이
            // 데이터만 추가하면 된다.
            var portraitArtProp = so.FindProperty("enemyPortraitArt");
            string[] goblinVariants = { "약소형", "날렵형", "거대형", "주술사형" };
            string[] guardianVariants = { "균형형", "중장형", "기동형", "마도형" };
            portraitArtProp.arraySize = goblinVariants.Length + guardianVariants.Length;
            for (int i = 0; i < goblinVariants.Length; i++)
            {
                string fileBase = $"enemy_고블린_{goblinVariants[i]}";
                var element = portraitArtProp.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("fileName").stringValue = $"{fileBase}.png";
                element.FindPropertyRelative("sprite").objectReferenceValue = LoadSprite(fileBase);
            }
            for (int i = 0; i < guardianVariants.Length; i++)
            {
                string fileBase = $"enemy_던전수호자_{guardianVariants[i]}";
                var element = portraitArtProp.GetArrayElementAtIndex(goblinVariants.Length + i);
                element.FindPropertyRelative("fileName").stringValue = $"{fileBase}.png";
                element.FindPropertyRelative("sprite").objectReferenceValue = LoadSprite(fileBase);
            }

            // 신규(DEC-132): 전투 중 플레이어 전신 이미지 3종(직업별 1장). 직업 선택 화면(BuildClassSelectPanel)의
            // "_상반신"(흉상 크롭) 대신 전신 이미지를 쓴다 — 대립 구도는 캐릭터 전신이 서 있는 구도가 맞다.
            var playerPortraitArtProp = so.FindProperty("playerPortraitArt");
            string[] classIds = { "warrior", "rogue", "mage" };
            string[] classPortraitFiles = { "char_전사", "char_도적", "char_마법사" };
            playerPortraitArtProp.arraySize = classIds.Length;
            for (int i = 0; i < classIds.Length; i++)
            {
                var element = playerPortraitArtProp.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("classId").stringValue = classIds[i];
                element.FindPropertyRelative("sprite").objectReferenceValue = LoadSprite(classPortraitFiles[i]);
            }

            // 신규(DEC-137): 인벤토리/상점(무기고) 버튼에 표시할 아이템 아이콘. WeaponDatabase/
            // ArmorDatabase/CharacterClassDatabase의 아이템 이름 상수와 정확히 일치하는 문자열로
            // 매칭한다(enemyPortraitArt/playerPortraitArt와 동일한 "이름→스프라이트" 패턴 재사용 —
            // 범용 아이템 리소스 시스템을 새로 만들지 않음). 기존 포션 2종(회복 물약/작은 회복 물약)도
            // 지금까지 아이콘이 실제로 연결된 적이 없었으므로 이번에 같이 연결한다.
            var itemIconArtProp = so.FindProperty("itemIconArt");
            string[] itemNames =
            {
                WeaponDatabase.Longsword, WeaponDatabase.Greatsword,
                WeaponDatabase.Dagger, WeaponDatabase.VenomFangDagger,
                WeaponDatabase.Staff, WeaponDatabase.CrystalStaff,
                ArmorDatabase.LeatherArmor, ArmorDatabase.ReinforcedPlateArmor,
                CharacterClassDatabase.ManaRecoveryMaterialName, CharacterClassDatabase.ManaPotionName,
                "회복 물약", "작은 회복 물약",
            };
            string[] itemIconFiles =
            {
                "item_전사장검", "item_전사대검",
                "item_도적단검2자루", "item_독아단검",
                "item_마법사지팡이", "item_수정지팡이",
                "item_가죽갑옷", "item_강화판금갑옷",
                "item_마나결정", "item_마나포션",
                "item_회복물약", "item_작은회복물약",
            };
            itemIconArtProp.arraySize = itemNames.Length;
            for (int i = 0; i < itemNames.Length; i++)
            {
                var element = itemIconArtProp.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("itemName").stringValue = itemNames[i];
                element.FindPropertyRelative("sprite").objectReferenceValue = LoadSprite(itemIconFiles[i]);
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            // 신규(DEC-139/OQ-110 해결): 던전 보드(C-06) — 5개 고정 지역을 가로로 나열한 작은
            // 상태 표시 패널. 클릭 이동 기능은 없다(기존 선택지/버튼으로만 이동, 순수 시각 안내).
            BuildDungeonBoard(root, controller);

            return controller;
        }

        /// <summary>
        /// 신규(DEC-139): OQ-110 해결 — 콘솔 원작 5개 고정 지역(던전입구→갈림길→무기고→어두운통로→
        /// 보스의방, Map.cs 그대로)을 가로로 나열한 작은 보드 패널을 탐색 화면 상단(StatusLine
        /// 아래, fullbleed 배경은 그대로 유지)에 얹는다. 하드코딩된 S자형 경로선+원형 노드 7개짜리
        /// board_dungeon_background.png(OQ-110에서 5개 지역과 안 맞는다고 확인된 파일)는 쓰지 않고,
        /// 대신 기존 material_양피지 재질 배경 위에 지역별 개별 노드 아이콘 5장(node_entrance/fork/
        /// armory/corridor/boss)을 순서대로 배치했다 — 지역이 늘거나 순서가 바뀌면 그림 하나를 다시
        /// 그릴 필요 없이 이 5장만 교체/재배치하면 된다.
        ///
        /// "5개 고정 노드 + 토큰 하나"라는 지금 요구사항 전용 최소 구현이다(범용 보드/타일맵
        /// 프레임워크 아님). 연결선은 art-assets/board_route_completed.png 대신 단순 색상 Image로
        /// 대체했다(지시서가 허용한 "가장 간단한 방법") — 이미 지나온 구간은 UIColors.Primary(금색),
        /// 아직 안 지나온 구간은 UIColors.OutlineVariant(무채색)로 칠한다. node_event.png(예비 지역
        /// 슬롯)와 board_node_battle.png/board_node_normal.png는 5개 고정 지역 설계에 맞지 않아
        /// 이번엔 쓰지 않는다(예비 자산으로 남겨둠 — docs/06_open_questions.md DEC-139 참조).
        ///
        /// 노드를 클릭해 이동하는 기능은 만들지 않는다 — 이 게임은 선형 구조라 실제 이동은 기존
        /// 지역 선택지/버튼으로만 가능하고, 보드는 "지금 어디 있고 다음에 어디로 갈 수 있는지"를
        /// 보여주는 상태 표시 전용이다.
        /// </summary>
        private static void BuildDungeonBoard(RectTransform explorePanelRoot, ExplorePanelController controller)
        {
            string[] nodeIconFiles = { "node_entrance", "node_fork", "node_armory", "node_corridor", "node_boss" };
            float[] nodeX = { -290f, -145f, 0f, 145f, 290f };
            const float nodeSize = 44f;
            const float overlaySize = 52f;
            const float tokenSize = 60f;

            // StatusLine(화면 최상단, 세로 범위 약 [-31,-1])과 겹치지 않도록 그 아래(-140~-50)에
            // 배치한다 — DEC-116 fullbleed 배경은 가리지 않고, 그 위에 얹는 작은 패널 하나일 뿐이다.
            var board = CreateAnchored("DungeonBoard", explorePanelRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(660, 90), new Vector2(0, -95));
            var boardBg = board.gameObject.AddComponent<Image>();
            boardBg.color = new Color32(0xE8, 0xDC, 0xC0, 0xE6);
            boardBg.raycastTarget = false; // 클릭 이동 기능 없음 — 순수 상태 표시 전용
            AddBackgroundSprite(board, "material_양피지", Color.white, 1f);

            var routeConnectors = new Image[nodeIconFiles.Length - 1];
            for (int i = 0; i < routeConnectors.Length; i++)
            {
                float midX = (nodeX[i] + nodeX[i + 1]) / 2f;
                float width = Mathf.Abs(nodeX[i + 1] - nodeX[i]) - nodeSize;
                var routeRT = CreateAnchored($"Route_{i}", board, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(width, 4f), new Vector2(midX, 0f));
                var routeImg = routeRT.gameObject.AddComponent<Image>();
                routeImg.color = UIColors.OutlineVariant;
                routeImg.raycastTarget = false;
                routeConnectors[i] = routeImg;
            }

            var nodeAnchors = new RectTransform[nodeIconFiles.Length];
            var clearedOverlays = new Image[nodeIconFiles.Length];
            var lockedOverlays = new Image[nodeIconFiles.Length];

            for (int i = 0; i < nodeIconFiles.Length; i++)
            {
                var nodeRT = CreateAnchored($"Node_{i}_{nodeIconFiles[i]}", board, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(nodeSize, nodeSize), new Vector2(nodeX[i], 0f));
                var nodeImg = nodeRT.gameObject.AddComponent<Image>();
                nodeImg.sprite = LoadSprite(nodeIconFiles[i]);
                nodeImg.preserveAspect = true;
                nodeImg.raycastTarget = false;
                nodeAnchors[i] = nodeRT;

                var clearedRT = CreateAnchored("ClearedOverlay", nodeRT, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(overlaySize, overlaySize), Vector2.zero);
                var clearedImg = clearedRT.gameObject.AddComponent<Image>();
                clearedImg.sprite = LoadSprite("board_node_cleared");
                clearedImg.preserveAspect = true;
                clearedImg.raycastTarget = false;
                clearedRT.gameObject.SetActive(false);
                clearedOverlays[i] = clearedImg;

                var lockedRT = CreateAnchored("LockedOverlay", nodeRT, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(overlaySize, overlaySize), Vector2.zero);
                var lockedImg = lockedRT.gameObject.AddComponent<Image>();
                lockedImg.sprite = LoadSprite("board_node_locked");
                lockedImg.preserveAspect = true;
                lockedImg.raycastTarget = false;
                lockedRT.gameObject.SetActive(false);
                lockedOverlays[i] = lockedImg;
            }

            // 현재 위치 토큰 — 모든 노드/오버레이보다 위에 그려지도록 마지막에 생성한다.
            var tokenRT = CreateAnchored("CurrentLocationToken", board, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(tokenSize, tokenSize), new Vector2(nodeX[0], 0f));
            var tokenImg = tokenRT.gameObject.AddComponent<Image>();
            tokenImg.sprite = LoadSprite("token_current_location");
            tokenImg.preserveAspect = true;
            tokenImg.raycastTarget = false;
            tokenRT.SetAsLastSibling();

            var boardSO = new SerializedObject(controller);
            SetObjectArray(boardSO, "boardNodeAnchors", nodeAnchors);
            SetObjectArray(boardSO, "boardNodeClearedOverlays", clearedOverlays);
            SetObjectArray(boardSO, "boardNodeLockedOverlays", lockedOverlays);
            SetObjectArray(boardSO, "boardRouteConnectors", routeConnectors);
            boardSO.FindProperty("boardToken").objectReferenceValue = tokenRT;
            boardSO.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// 신규(DEC-139): enemyPortraitArt 등 기존 List&lt;Serializable&gt; 배열 채우기 패턴과 달리,
        /// 던전 보드는 단순 Object[] 배열 필드(RectTransform[]/Image[])라 이 헬퍼로 공용 처리한다.
        /// </summary>
        private static void SetObjectArray(SerializedObject so, string propertyName, Object[] values)
        {
            var prop = so.FindProperty(propertyName);
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }

        // ------------------------------------------------------------------
        // Result Panel (SCR-007/008)
        // ------------------------------------------------------------------
        private static ResultPanelController BuildResultPanel(Transform canvasT, GameBootstrap bootstrap)
        {
            var root = CreateFullStretch("ResultPanel", canvasT);
            var bg = root.gameObject.AddComponent<Image>();
            bg.color = UIColors.Surface;

            var card = CreateAnchored("ParchmentCard", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(480, 360), Vector2.zero);
            var cardImg = card.gameObject.AddComponent<Image>();
            cardImg.color = new Color32(0xE8, 0xDC, 0xC0, 0xFF);
            AddFoldLine(card); // DEC-121/DEC-127: 결과지(큰 양피지 패널)에 중앙 접힘선
            AddFiligreeCorners(card, 56f); // DEC-117/DEC-120/DEC-138: 결과지 모서리 필리그리 장식

            var headline = CreateText("Headline", card, "GAME OVER", 30, new Color32(0x8B, 0x2E, 0x2E, 0xFF),
                TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(420, 50), new Vector2(0, -30));

            var description = CreateText("Description", card, "", 16, new Color32(0x5B, 0x4E, 0x33, 0xFF),
                TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(420, 40), new Vector2(0, -90));

            // DEC-121/DEC-127: 깃펜 필기 연출 — 서술 텍스트 하단 진행 기준선을 따라가는 펜 아이콘.
            // 특정 글자(caret) 위치가 아니라 Description 텍스트 박스 폭 전체를 트랙으로 사용한다
            // (unity-mapping.html M-02 2026-09-07 수정 지침 — 줄바꿈에 영향받지 않기 위함).
            // 신규(DEC-138): 잉크브라운으로 물들인 원형 대신 실제 깃펜 6프레임(vfx_quill_*)을
            // 사용한다. 원본 비율(약 0.88~0.90)이 원형과 달리 세로로 조금 긴 직사각형에 가까워
            // preserveAspect를 켠다.
            var penIconRT = CreateAnchored("QuillPen", description.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(18, 22), new Vector2(0, -6));
            var penImage = penIconRT.gameObject.AddComponent<Image>();
            penImage.sprite = LoadSprite("vfx_quill_idle");
            penImage.preserveAspect = true;
            penImage.raycastTarget = false;
            var penCanvasGroup = penIconRT.gameObject.AddComponent<CanvasGroup>();
            penCanvasGroup.alpha = 0f;

            var penFrames = new[]
            {
                LoadSprite("vfx_quill_idle"),
                LoadSprite("vfx_quill_writing_01"),
                LoadSprite("vfx_quill_writing_02"),
                LoadSprite("vfx_quill_writing_03"),
                LoadSprite("vfx_quill_writing_04"),
                LoadSprite("vfx_quill_writing_end"),
            };

            var quillReveal = description.gameObject.AddComponent<QuillRevealText>();
            BindSerialized(quillReveal,
                ("text", description),
                ("penIcon", penIconRT),
                ("penCanvasGroup", penCanvasGroup),
                ("penImage", penImage));

            // penFrames는 Sprite[]라 BindSerialized(단일 Object 바인딩용)로는 못 넣는다 — 배열
            // 프로퍼티는 itemIconArtProp(위 GameBootstrap 설정)과 동일한 패턴으로 직접 채운다.
            var quillSO = new SerializedObject(quillReveal);
            var penFramesProp = quillSO.FindProperty("penFrames");
            penFramesProp.arraySize = penFrames.Length;
            for (int i = 0; i < penFrames.Length; i++)
            {
                penFramesProp.GetArrayElementAtIndex(i).objectReferenceValue = penFrames[i];
            }
            quillSO.ApplyModifiedPropertiesWithoutUndo();

            var stats = CreateText("Stats", card, "", 14, new Color32(0x5B, 0x4E, 0x33, 0xFF),
                TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(420, 30), new Vector2(0, -140));

            var backBtn = CreateButton("BackToTitleButton", card, "타이틀로 돌아가기", UIColors.Primary, Color.black,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(280, 52), new Vector2(0, 30));

            var controller = root.gameObject.AddComponent<ResultPanelController>();
            BindSerialized(controller,
                ("bootstrap", bootstrap),
                ("headlineText", headline),
                ("descriptionQuill", quillReveal),
                ("descriptionText", description),
                ("statsText", stats),
                ("backToTitleButton", backBtn));

            return controller;
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------
        private static RectTransform CreateFullStretch(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        private static RectTransform CreateAnchored(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 sizeDelta, Vector2 anchoredPosition)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.sizeDelta = sizeDelta;
            rt.anchoredPosition = anchoredPosition;
            return rt;
        }

        private static TMP_Text CreateText(string name, Transform parent, string text, float fontSize, Color color,
            TextAlignmentOptions alignment, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta, Vector2 anchoredPosition)
        {
            var rt = CreateAnchored(name, parent, anchorMin, anchorMax, sizeDelta, anchoredPosition);
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static Button CreateButton(string name, Transform parent, string label, Color bgColor, Color textColor,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta, Vector2 anchoredPosition)
        {
            var rt = CreateAnchored(name, parent, anchorMin, anchorMax, sizeDelta, anchoredPosition);
            var image = rt.gameObject.AddComponent<Image>();
            image.color = bgColor;
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            CreateText("Label", rt, label, 18, textColor, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            return button;
        }

        /// <summary>
        /// DEC-140 신규: 직업 카드가 선택됐을 때 카드 상단 중앙(카드 바깥쪽 위, 카드를 가리지 않는
        /// 위치)에 나타나는 핀 아이콘. 새 이미지를 만들지 않고 DEC-139에서 던전 보드 "현재 위치" 토큰으로
        /// 쓴 `token_current_location.png`(금색 장식 오브젝트)를 그대로 재사용한다 — "선택됨/현재 위치"를
        /// 금색 토큰으로 표시하는 의미가 이미 프로젝트 안에서 일관되게 쓰이고 있어 재사용이 자연스럽다.
        /// 사용자 명시적 요청(DEC-118/DEC-127의 잉크마크 선택 효과를 이 화면에 한해 대체)에 따라, 화려한
        /// 애니메이션 없이 단순 활성화 토글만 한다(과설계 금지) — ClassSelectPanelController.UpdateSelectionVisual()이
        /// SetActive(true/false)만 호출한다.
        /// </summary>
        private static GameObject BuildSelectionPin(RectTransform cardRoot)
        {
            const float pinSize = 48f;
            var pinRT = CreateAnchored("SelectionPin", cardRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(pinSize, pinSize), new Vector2(0f, pinSize / 2f + 6f));
            var pinImg = pinRT.gameObject.AddComponent<Image>();
            pinImg.sprite = LoadSprite("token_current_location");
            pinImg.preserveAspect = true;
            pinImg.raycastTarget = false;
            pinRT.gameObject.SetActive(false);
            return pinRT.gameObject;
        }

        /// <summary>
        /// DEC-133 신규: 데미지/회복 숫자 팝업(DamagePopupText)을 portraitRT의 자식으로 만든다.
        /// 텍스트 색은 DamagePopupText.Show()가 매번 호출 시점에 다시 지정하므로(피해=Tertiary,
        /// 회복=HealNumber) 여기서는 기본값만 넣어둔다. 평소엔 비활성 상태로 시작해 전투 밖에서는
        /// 화면에 아무 영향도 주지 않는다.
        /// </summary>
        private static DamagePopupText BuildDamagePopup(RectTransform portraitRT, string name)
        {
            var popupText = CreateText(name, portraitRT, "", 28, UIColors.Tertiary, TextAlignmentOptions.Center,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(200, 60), new Vector2(0, 40));
            var rt = popupText.rectTransform;
            var canvasGroup = rt.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;

            var popup = rt.gameObject.AddComponent<DamagePopupText>();
            BindSerialized(popup, ("text", popupText), ("canvasGroup", canvasGroup), ("rectTransform", rt));
            rt.gameObject.SetActive(false);
            return popup;
        }

        /// <summary>
        /// DEC-121/DEC-127: 큰 양피지 패널(표지·결과지) 중앙에 은은한 세로 접힘선(FoldLine)을
        /// 추가한다. 원본 CSS .foldline은 그라디언트지만, 스크립트로 만드는 단순화 버전은 새
        /// 텍스처를 만들지 않기 위해 단색 반투명 Image로 근사한다(CSS top:5%/bottom:5%와 동일 비율).
        /// </summary>
        private static void AddFoldLine(RectTransform parent)
        {
            var go = new GameObject("FoldLine", typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0.5f, 0.05f);
            rt.anchorMax = new Vector2(0.5f, 0.95f);
            rt.sizeDelta = new Vector2(3f, 0f);
            rt.anchoredPosition = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = UIColors.FoldLine;
            img.raycastTarget = false;
        }

        /// <summary>
        /// 신규(DEC-138): DEC-117/DEC-120에서 설계됐지만 벡터 곡선 필리그리를 Unity 기본 도형으로
        /// 흉내내기 어려워(DEC-127 리뷰) 미구현이었던 모서리 장식을, 실제 art-assets/ui_filigree_*.png
        /// 4장이 생긴 지금 연결한다. 큰 양피지 패널(표지/직업 카드/결과지)에만 적용하고, 탐색·전투
        /// fullbleed 화면에는 적용하지 않는다(DEC-116). cornerSize(정사각형 한 변, px)는 카드 크기
        /// 대비 과하지 않도록 호출부에서 지정한다 — 범용 데코레이터가 아니라 이 세 호출부 전용.
        /// </summary>
        private static void AddFiligreeCorners(RectTransform parent, float cornerSize)
        {
            AddFiligreeCorner(parent, "ui_filigree_top_left", new Vector2(0f, 1f), cornerSize);
            AddFiligreeCorner(parent, "ui_filigree_top_right", new Vector2(1f, 1f), cornerSize);
            AddFiligreeCorner(parent, "ui_filigree_bottom_left", new Vector2(0f, 0f), cornerSize);
            AddFiligreeCorner(parent, "ui_filigree_bottom_right", new Vector2(1f, 0f), cornerSize);
        }

        private static void AddFiligreeCorner(RectTransform parent, string spriteName, Vector2 corner, float size)
        {
            var sprite = LoadSprite(spriteName);
            if (sprite == null)
            {
                return;
            }

            var rt = CreateAnchored(spriteName, parent, corner, corner, new Vector2(size, size), Vector2.zero);
            rt.pivot = corner; // 이미지가 모서리에서 카드 안쪽으로만 펼쳐지도록(카드 전체를 덮지 않음)
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
        }

        private static void AddOutline(Transform target, Color color)
        {
            var outline = target.gameObject.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(1.5f, -1.5f);
        }

        private static void AddBackgroundSprite(RectTransform target, string spriteName, Color tint, float alpha)
        {
            var sprite = LoadSprite(spriteName);
            if (sprite == null) return;

            var rt = CreateFullStretch(spriteName, target);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Simple;
            img.preserveAspect = false;
            var c = tint;
            c.a = alpha;
            img.color = c;
            img.raycastTarget = false;
            rt.SetAsFirstSibling();
        }

        private static Sprite LoadSprite(string fileNameWithoutExtension)
        {
            var guids = AssetDatabase.FindAssets($"{fileNameWithoutExtension} t:Sprite", new[] { ArtRoot });
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) == fileNameWithoutExtension)
                {
                    return AssetDatabase.LoadAssetAtPath<Sprite>(path);
                }
            }
            return null;
        }

        /// <summary>
        /// 신규(DEC-137): LoadSprite와 동일한 "파일명으로 Art 폴더 전체를 재귀 검색" 패턴이지만,
        /// 마스크 텍스처는 Sprite가 아니라 셰이더의 _MaskTex 슬롯에 꽂을 원본 Texture2D가 필요해
        /// t:Texture2D로 검색한다(TextureImporterType이 Sprite여도 메인 오브젝트는 여전히 Texture2D).
        /// </summary>
        private static Texture2D LoadTexture(string fileNameWithoutExtension)
        {
            var guids = AssetDatabase.FindAssets($"{fileNameWithoutExtension} t:Texture2D", new[] { ArtRoot });
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) == fileNameWithoutExtension)
                {
                    return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                }
            }
            return null;
        }

        /// <summary>
        /// 신규(DEC-137): PlayerPortrait/EnemyPortrait 전용 가장자리 페이드 머티리얼을 만든다.
        /// 셰이더(Assets/Art/Shaders/UIPortraitEdgeMask.shader)나 마스크 텍스처를 찾지 못하면 null을
        /// 반환해 호출부가 조용히 마스킹을 건너뛰게 한다 — 마스킹은 장식적 효과이지 전투 진행에
        /// 필수가 아니므로, 실패해도 기존처럼 사각형 이미지가 그대로 보이는 것으로 안전하게 대체된다
        /// (범용 마스킹 프레임워크가 아니라 이 두 호출부 전용 — 과설계 금지).
        /// </summary>
        private static Material CreatePortraitEdgeMaskMaterial(string maskFileNameWithoutExtension)
        {
            var shader = Shader.Find("TextRPG/UI/PortraitEdgeMask");
            if (shader == null)
            {
                Debug.LogWarning("[ProjectSetupTool] TextRPG/UI/PortraitEdgeMask 셰이더를 찾을 수 없어 " +
                    $"'{maskFileNameWithoutExtension}' 마스킹을 건너뜁니다.");
                return null;
            }

            var maskTexture = LoadTexture(maskFileNameWithoutExtension);
            if (maskTexture == null)
            {
                Debug.LogWarning($"[ProjectSetupTool] 마스크 텍스처 '{maskFileNameWithoutExtension}'를 찾을 수 없어 " +
                    "마스킹을 건너뜁니다.");
                return null;
            }

            var material = new Material(shader) { name = $"Mat_{maskFileNameWithoutExtension}" };
            material.SetTexture("_MaskTex", maskTexture);
            return material;
        }

        private static void BindSerialized(Object target, params (string field, Object value)[] bindings)
        {
            var so = new SerializedObject(target);
            foreach (var (field, value) in bindings)
            {
                so.FindProperty(field).objectReferenceValue = value;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
