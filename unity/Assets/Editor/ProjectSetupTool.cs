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

                var nameText = CreateText("Name", cardRoot, ids[i], 22, new Color32(0x3B, 0x33, 0x20, 0xFF),
                    TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(280, 34), new Vector2(0, -290));

                var statsText = CreateText("Stats", cardRoot, "", 16, new Color32(0x3B, 0x33, 0x20, 0xFF),
                    TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(280, 90), new Vector2(0, -330));

                var itemsText = CreateText("Items", cardRoot, "", 14, new Color32(0x5B, 0x4E, 0x33, 0xFF),
                    TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(280, 40), new Vector2(0, -420));

                var selectBtn = cardRoot.gameObject.AddComponent<Button>();
                var targetGraphic = cardBg;
                selectBtn.targetGraphic = targetGraphic;

                cards[i] = new ClassSelectPanelController.ClassCard
                {
                    classId = ids[i],
                    selectButton = selectBtn,
                    cardBackground = cardBg,
                    portraitImage = portraitImg,
                    nameText = nameText,
                    statsText = statsText,
                    itemsText = itemsText
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
                ("statusOverlayCloseButton", closeBtn));

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

            var headline = CreateText("Headline", card, "GAME OVER", 30, new Color32(0x8B, 0x2E, 0x2E, 0xFF),
                TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(420, 50), new Vector2(0, -30));

            var description = CreateText("Description", card, "", 16, new Color32(0x5B, 0x4E, 0x33, 0xFF),
                TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(420, 40), new Vector2(0, -90));

            var stats = CreateText("Stats", card, "", 14, new Color32(0x5B, 0x4E, 0x33, 0xFF),
                TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(420, 30), new Vector2(0, -140));

            var backBtn = CreateButton("BackToTitleButton", card, "타이틀로 돌아가기", UIColors.Primary, Color.black,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(280, 52), new Vector2(0, 30));

            var controller = root.gameObject.AddComponent<ResultPanelController>();
            BindSerialized(controller,
                ("bootstrap", bootstrap),
                ("headlineText", headline),
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
