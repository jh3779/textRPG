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

                var inkMark = BuildInkMarkOverlay(cardRoot); // DEC-118/DEC-127: 선택 시 잉크마크 연출

                cards[i] = new ClassSelectPanelController.ClassCard
                {
                    classId = ids[i],
                    selectButton = selectBtn,
                    cardBackground = cardBg,
                    portraitImage = portraitImg,
                    nameText = nameText,
                    statsText = statsText,
                    itemsText = itemsText,
                    inkMarkOverlay = inkMark
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
                element.FindPropertyRelative("inkMarkOverlay").objectReferenceValue = cards[i].inkMarkOverlay;
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

            // 신규(DEC-132): 전투 중 플레이어 캐릭터 전신 이미지 — EnemyPortrait과 대칭(x=-280)으로 배치해
            // "대립 구도"를 만든다. 평소(탐색 화면)에는 ExplorePanelController.HidePlayerPortrait()가 꺼둔다.
            // 인물 이미지 가장자리 마스킹(DEC-116 원문)은 EnemyPortrait에도 아직 구현돼 있지 않아(코드
            // 전수 확인, 관련 셰이더/머티리얼/마스크 이미지 없음) 이번에도 동일하게 생략했다 — 대립 구도
            // 배치 자체가 이번 요청의 핵심이고, 원본 배경이 제거된 소스가 아니라는 문서상 전제가 여전히
            // 유효하므로 완벽한 마스킹은 과설계로 보고 다음 작업으로 남긴다(06_open_questions.md DEC-132).
            var playerPortraitRT = CreateAnchored("PlayerPortrait", root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(360, 460), new Vector2(-280, 330));
            var playerPortraitImg = playerPortraitRT.gameObject.AddComponent<Image>();
            playerPortraitImg.preserveAspect = true;
            playerPortraitImg.raycastTarget = false;
            playerPortraitImg.enabled = false;

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
                ("playerPortraitImage", playerPortraitImg));

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

            so.ApplyModifiedPropertiesWithoutUndo();

            return controller;
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

            var headline = CreateText("Headline", card, "GAME OVER", 30, new Color32(0x8B, 0x2E, 0x2E, 0xFF),
                TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(420, 50), new Vector2(0, -30));

            var description = CreateText("Description", card, "", 16, new Color32(0x5B, 0x4E, 0x33, 0xFF),
                TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(420, 40), new Vector2(0, -90));

            // DEC-121/DEC-127: 깃펜 필기 연출 — 서술 텍스트 하단 진행 기준선을 따라가는 펜 아이콘.
            // 특정 글자(caret) 위치가 아니라 Description 텍스트 박스 폭 전체를 트랙으로 사용한다
            // (unity-mapping.html M-02 2026-09-07 수정 지침 — 줄바꿈에 영향받지 않기 위함).
            var penIconRT = CreateAnchored("QuillPen", description.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(12, 12), new Vector2(0, -4));
            var penImage = penIconRT.gameObject.AddComponent<Image>();
            penImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            penImage.color = UIColors.QuillInk;
            penImage.raycastTarget = false;
            var penCanvasGroup = penIconRT.gameObject.AddComponent<CanvasGroup>();
            penCanvasGroup.alpha = 0f;

            var quillReveal = description.gameObject.AddComponent<QuillRevealText>();
            BindSerialized(quillReveal,
                ("text", description),
                ("penIcon", penIconRT),
                ("penCanvasGroup", penCanvasGroup));

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
        /// DEC-118/DEC-127: 잉크마크 선택 표식(InkMarkOverlay)을 cardRoot 위에 카드보다 살짝 크게
        /// (CSS .inkmark의 inset:-7px 근사) 겹쳐 생성한다. 처음엔 비활성 상태로 시작하고, 선택 시
        /// ClassSelectPanelController.UpdateSelectionVisual()이 Show()/Hide()를 호출한다.
        /// </summary>
        private static InkMarkOverlay BuildInkMarkOverlay(RectTransform cardRoot)
        {
            var overlayRT = CreateAnchored("InkMark", cardRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(cardRoot.sizeDelta.x + 16f, cardRoot.sizeDelta.y + 16f), Vector2.zero);
            var ring = overlayRT.gameObject.AddComponent<Image>();
            ring.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            ring.color = UIColors.InkMark;
            ring.type = Image.Type.Filled;
            ring.fillMethod = Image.FillMethod.Radial360;
            ring.fillClockwise = true;
            ring.fillAmount = 0f;
            ring.raycastTarget = false;
            overlayRT.SetAsLastSibling(); // CSS z-index:5와 동일하게 카드 내용물 위에 그려지도록

            var overlay = overlayRT.gameObject.AddComponent<InkMarkOverlay>();
            BindSerialized(overlay, ("ring", ring));
            overlayRT.gameObject.SetActive(false);
            return overlay;
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
