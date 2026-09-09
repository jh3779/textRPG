/*
 * CoreFlowPlayModeTests.cs
 *
 * 📝 역할: DEC-126. RegressionSmokeTest.cs(EditMode, GameSession/Player/BattleSystem 등
 * 순수 C# 로직을 직접 호출해서 값만 확인하는 방식)로는 절대 잡을 수 없는 종류의 결함
 * (버튼 참조 누락으로 클릭 자체가 안 됨, 이벤트 리스너 미배선, 씬 재생성 중 바인딩 손상 등)을
 * 잡기 위해 Unity Test Framework의 PlayMode 테스트로 Main.unity를 실제로 로드하고
 * Button.onClick.Invoke()로 클릭을 시뮬레이션한 뒤, 그 결과 UI 상태(GameObject.activeSelf,
 * TMP_Text 내용, Image.sprite 등)가 기대한 대로 바뀌는지 assert한다 — "사람이 Play 버튼을
 * 눌러 클릭해 보는 것"에 가장 가까운 자동화 대체재다.
 *
 * 과설계 금지: 모든 화면·모든 버튼을 다 검증하지 않는다. 핵심 화면 전환(타이틀→직업선택→
 * 탐색→전투)과 대표적인 버튼 클릭 1개 이상씩만 확인한다(테스트 A~F, 이후 발견된 버그·신규 기능에
 * 대응해 G~M까지 추가됨 — DEC-132: K~M은 전투 플레이어 초상화·"가방" 아이템 사용).
 *
 * 실행: Unity -batchmode -nographics -runTests -testPlatform PlayMode
 *   -testResults <결과경로>.xml -projectPath unity
 *
 * 씬 재로드로 테스트 간 격리를 확보한다 — 매 테스트가 SceneManager.LoadScene("Main")으로
 * 시작하므로 GameBootstrap.Awake()가 매번 새 GameSession을 만든다(정적 가변 상태 없음).
 * 유일한 외부 부작용은 디스크의 세이브 파일(Application.persistentDataPath/save1.json)이라
 * RegressionSmokeTest.TestSaveExistsDetection과 동일한 백업/복원 패턴을 그대로 재사용한다.
 */

using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TextRPG.GameLogic;
using TextRPG.Persistence;
using TextRPG.UI;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TextRPG.Tests.PlayMode
{
    public class CoreFlowPlayModeTests
    {
        private const string MainSceneName = "Main";

        private static IEnumerator LoadMainScene()
        {
            SceneManager.LoadScene(MainSceneName, LoadSceneMode.Single);
            yield return null;
            yield return null;
        }

        private static GameBootstrap FindBootstrap()
        {
            return Object.FindFirstObjectByType<GameBootstrap>(FindObjectsInactive.Include);
        }

        private static T FindController<T>() where T : Object
        {
            return Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
        }

        /// <summary>
        /// ButtonRow 하위의 동적으로 생성된 버튼 중, 라벨(TMP_Text)이 prefix로 시작하는 첫 버튼을 찾는다.
        /// ExplorePanelController가 매 화면마다 버튼을 전부 파괴하고 새로 Instantiate하므로 이름이 아니라
        /// 라벨 텍스트로 찾아야 한다(모두 "ButtonTemplate(Clone)"이라는 동일한 이름을 가짐).
        /// </summary>
        private static Button FindButtonByLabelPrefix(Transform container, string prefix)
        {
            foreach (var button in container.GetComponentsInChildren<Button>(false))
            {
                var label = button.GetComponentInChildren<TMP_Text>(true);
                if (label != null && label.text.StartsWith(prefix))
                {
                    return button;
                }
            }
            return null;
        }

        // ----------------------------------------------------------------
        // 알려진 무관한 에디터 노이즈 로그만 정확히 소비(review-verify-agent Critical 반영)
        // ----------------------------------------------------------------

        /// <summary>
        /// 이 프로젝트가 아직 TMP Essential Resources를 임포트하지 않아(범위 밖 인프라 변경 —
        /// 최종 보고 참조) Unity Editor가 세션마다 한 번 TMP 패키지 임포터 창을 -nographics에서
        /// 띄우려다 실패하며 남기는 무관한 에러 로그(예: "No graphic device is available to show
        /// the window."/"...to initialize the view.")가 있다. WaitForSeconds로 실제 시간을 오래
        /// 기다리는 테스트(G/H)에서 이 로그와 우연히 겹칠 수 있다.
        ///
        /// review-verify-agent Critical 지적: 최초 구현은 이 구간에서 <see cref="LogAssert.ignoreFailingMessages"/>를
        /// 통째로 켜서 무관한 로그를 걸렀는데, 이 API는 특정 메시지만이 아니라 그 구간의 모든
        /// Error/Assert/Exception 자동실패 체크를 꺼버려 정작 이번에 새로 추가한 위험도 높은 코드
        /// (InkMarkOverlay/QuillRevealText)의 진짜 에러도 못 잡는 문제가 실증됐다(InkMarkOverlay.Show()에
        /// Debug.LogError를 임시 주입해도 테스트가 Passed로 통과하는 것으로 재현됨).
        ///
        /// 수정: <see cref="LogAssert.Expect(LogType, string)"/>는 "그 메시지가 반드시 나타나야
        /// 테스트가 통과한다"는 정반대 제약이 있어(끝까지 안 나타나면 오히려 실패), 타이밍이
        /// 비결정적인 이 노이즈에 그대로 쓸 수 없다. 대신 <see cref="Application.logMessageReceived"/>를
        /// 직접 구독해 알려진 노이즈 패턴과 실제로 일치하는 로그가 찍히는 순간에만(즉 이미 발생을
        /// 확인한 로그에 대해서만) 그 정확한 메시지 문자열로 <see cref="LogAssert.Expect(LogType, string)"/>를
        /// 호출해 그 로그 하나만 소비한다 — 노이즈가 몇 번 찍히든, 심지어 한 번도 안 찍히든(이 경우
        /// Expect 자체를 호출하지 않으므로 "기대했는데 안 나타남" 실패도 없다) 항상 안전하다.
        /// 패턴에 해당하지 않는 다른 모든 Error/Assert/Exception 로그(우리 코드가 남긴 진짜 버그
        /// 포함)는 이 핸들러가 손대지 않으므로 여전히 테스트를 실패시킨다.
        /// </summary>
        private static readonly Regex KnownNoisyEditorLogPattern =
            new Regex(@"No graphic device is available to (show the window|initialize the view)\.");

        private static void ConsumeKnownEditorNoiseIfMatched(string message, string stackTrace, LogType type)
        {
            if (type == LogType.Error && KnownNoisyEditorLogPattern.IsMatch(message))
            {
                LogAssert.Expect(LogType.Error, message);
            }
        }

        // ----------------------------------------------------------------
        // 세이브 파일 백업/복원 (RegressionSmokeTest.TestSaveExistsDetection과 동일 패턴 재사용)
        // ----------------------------------------------------------------
        private static string BackupSaveFileIfExists(out bool hadExisting)
        {
            string path = SaveSystem.GetSavePath();
            hadExisting = File.Exists(path);
            return hadExisting ? File.ReadAllText(path) : null;
        }

        private static void RestoreSaveFile(bool hadExisting, string backup)
        {
            string path = SaveSystem.GetSavePath();
            if (hadExisting)
            {
                File.WriteAllText(path, backup);
            }
            else if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private static void DeleteSaveFileIfExists()
        {
            string path = SaveSystem.GetSavePath();
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private static void WriteDummySaveFile()
        {
            string path = SaveSystem.GetSavePath();
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            // OnNewGameClicked()은 SaveSystem.SaveExists()(File.Exists 여부)만 확인하므로
            // 실제 로드 가능한 내용일 필요는 없다 — 모달 표시 조건 검증만이 목적.
            File.WriteAllText(path, "{\"version\":2}");
        }

        // ----------------------------------------------------------------
        // 테스트 A: Main.unity 로드 → 타이틀 화면이 실제로 뜨는지
        // ----------------------------------------------------------------
        [UnityTest]
        public IEnumerator A_TitleScreen_ShowsOnLoad()
        {
            yield return LoadMainScene();

            var bootstrap = FindBootstrap();
            Assert.IsNotNull(bootstrap, "GameBootstrap을 씬에서 찾을 수 없습니다.");
            Assert.AreEqual(GameState.TITLE, bootstrap.Session.CurrentState);

            var titleController = FindController<TitlePanelController>();
            Assert.IsNotNull(titleController, "TitlePanelController를 씬에서 찾을 수 없습니다.");
            Assert.IsTrue(titleController.gameObject.activeSelf, "타이틀 패널이 로드 직후 활성 상태여야 합니다.");
        }

        // ----------------------------------------------------------------
        // 테스트 B: 세이브 없음 → "새 게임" 클릭 → 직업 선택 화면으로 전환되는지
        // ----------------------------------------------------------------
        [UnityTest]
        public IEnumerator B_NewGame_WithoutSave_GoesToClassSelect()
        {
            bool hadExisting = false;
            string backup = null;
            try
            {
                backup = BackupSaveFileIfExists(out hadExisting);
                DeleteSaveFileIfExists();

                yield return LoadMainScene();

                var bootstrap = FindBootstrap();
                var titleController = FindController<TitlePanelController>();
                var classSelectController = FindController<ClassSelectPanelController>();

                var newGameButton = titleController.transform.Find("ParchmentCard/NewGameButton").GetComponent<Button>();
                Assert.IsNotNull(newGameButton, "NewGameButton을 찾을 수 없습니다.");

                newGameButton.onClick.Invoke();
                yield return null;

                Assert.AreEqual(GameState.CLASS_SELECT, bootstrap.Session.CurrentState);
                Assert.IsTrue(classSelectController.gameObject.activeSelf, "직업 선택 패널로 전환되어야 합니다.");
                Assert.IsFalse(titleController.gameObject.activeSelf, "타이틀 패널은 비활성화되어야 합니다.");
            }
            finally
            {
                RestoreSaveFile(hadExisting, backup);
            }
        }

        // ----------------------------------------------------------------
        // 테스트 C: 세이브 있음 → "새 게임" 클릭 → 덮어쓰기 확인 모달(DEC-122 실제 검증)
        // ----------------------------------------------------------------
        [UnityTest]
        public IEnumerator C_NewGame_WithSave_ShowsOverwriteConfirmModal()
        {
            bool hadExisting = false;
            string backup = null;
            try
            {
                backup = BackupSaveFileIfExists(out hadExisting);
                WriteDummySaveFile();

                yield return LoadMainScene();

                var bootstrap = FindBootstrap();
                var titleController = FindController<TitlePanelController>();
                var classSelectController = FindController<ClassSelectPanelController>();

                var newGameButton = titleController.transform.Find("ParchmentCard/NewGameButton").GetComponent<Button>();
                var overlay = titleController.transform.Find("OverwriteConfirmOverlay").gameObject;
                var yesButton = titleController.transform.Find("OverwriteConfirmOverlay/ConfirmCard/YesButton").GetComponent<Button>();
                var noButton = titleController.transform.Find("OverwriteConfirmOverlay/ConfirmCard/NoButton").GetComponent<Button>();

                Assert.IsFalse(overlay.activeSelf, "모달은 처음엔 숨겨져 있어야 합니다.");

                // 1) "새 게임" 클릭 → 모달이 떠야 함
                newGameButton.onClick.Invoke();
                yield return null;

                Assert.IsTrue(overlay.activeSelf, "세이브가 있는 상태에서 '새 게임'을 누르면 확인 모달이 떠야 합니다(DEC-122).");
                Assert.AreEqual(GameState.TITLE, bootstrap.Session.CurrentState, "모달이 뜬 시점엔 아직 새 게임이 진행되면 안 됩니다.");
                Assert.IsFalse(classSelectController.gameObject.activeSelf);

                // 2) "아니오" 클릭 → 모달만 닫히고 여전히 타이틀
                noButton.onClick.Invoke();
                yield return null;

                Assert.IsFalse(overlay.activeSelf, "'아니오'를 누르면 모달이 닫혀야 합니다.");
                Assert.AreEqual(GameState.TITLE, bootstrap.Session.CurrentState);
                Assert.IsTrue(titleController.gameObject.activeSelf, "'아니오' 이후에도 타이틀 화면에 남아있어야 합니다.");

                // 3) 다시 "새 게임" → "예" 클릭 → 직업 선택으로 진행
                newGameButton.onClick.Invoke();
                yield return null;
                Assert.IsTrue(overlay.activeSelf, "다시 '새 게임'을 누르면 모달이 다시 떠야 합니다.");

                yesButton.onClick.Invoke();
                yield return null;

                Assert.IsFalse(overlay.activeSelf, "'예'를 누르면 모달이 닫혀야 합니다.");
                Assert.AreEqual(GameState.CLASS_SELECT, bootstrap.Session.CurrentState);
                Assert.IsTrue(classSelectController.gameObject.activeSelf, "'예'를 누르면 직업 선택 화면으로 전환되어야 합니다.");
            }
            finally
            {
                RestoreSaveFile(hadExisting, backup);
            }
        }

        // ----------------------------------------------------------------
        // 테스트 D: 직업 선택(전사) → 탐색 화면 전환 + 상태 표시(HP/마나 등) 정확성
        // ----------------------------------------------------------------
        [UnityTest]
        public IEnumerator D_SelectWarrior_ConfirmsAndShowsExploreWithCorrectStats()
        {
            bool hadExisting = false;
            string backup = null;
            try
            {
                backup = BackupSaveFileIfExists(out hadExisting);
                DeleteSaveFileIfExists();

                yield return LoadMainScene();

                var bootstrap = FindBootstrap();
                var titleController = FindController<TitlePanelController>();
                var classSelectController = FindController<ClassSelectPanelController>();
                var exploreController = FindController<ExplorePanelController>();

                titleController.transform.Find("ParchmentCard/NewGameButton").GetComponent<Button>().onClick.Invoke();
                yield return null;

                var warriorCardButton = classSelectController.transform.Find("Card_warrior").GetComponent<Button>();
                Assert.IsNotNull(warriorCardButton, "전사 카드 버튼을 찾을 수 없습니다.");
                warriorCardButton.onClick.Invoke();
                yield return null;

                var confirmButton = classSelectController.transform.Find("ConfirmButton").GetComponent<Button>();
                Assert.IsTrue(confirmButton.interactable, "직업을 선택하면 확정 버튼이 활성화되어야 합니다(INV-01).");

                confirmButton.onClick.Invoke();
                yield return null;

                Assert.AreEqual(GameState.PLAYING, bootstrap.Session.CurrentState);
                Assert.IsTrue(exploreController.gameObject.activeSelf, "탐색 화면으로 전환되어야 합니다.");
                Assert.IsFalse(classSelectController.gameObject.activeSelf);

                var statusLine = exploreController.transform.Find("StatusLine").GetComponent<TMP_Text>();
                // CharacterClassDatabase 최종 스탯(DEC-123): 전사 HP114/ATK13/Mana10
                Assert.IsTrue(statusLine.text.Contains("HP 114/114"), $"전사 HP 표시가 잘못됐습니다: {statusLine.text}");
                Assert.IsTrue(statusLine.text.Contains("Mana 10/10"), $"전사 마나 표시가 잘못됐습니다: {statusLine.text}");
                Assert.IsTrue(statusLine.text.Contains("ATK 13"), $"전사 공격력 표시가 잘못됐습니다: {statusLine.text}");
            }
            finally
            {
                RestoreSaveFile(hadExisting, backup);
            }
        }

        // ----------------------------------------------------------------
        // 테스트 E: 탐색 화면 동적 생성 버튼(이동) 클릭 → 실제로 지역 이동
        // ----------------------------------------------------------------
        [UnityTest]
        public IEnumerator E_ExploreScreen_MoveButtonClick_ActuallyMovesLocation()
        {
            bool hadExisting = false;
            string backup = null;
            try
            {
                backup = BackupSaveFileIfExists(out hadExisting);
                DeleteSaveFileIfExists();

                yield return LoadMainScene();
                yield return SelectWarriorAndConfirm();

                var bootstrap = FindBootstrap();
                var exploreController = FindController<ExplorePanelController>();
                var buttonRow = exploreController.transform.Find("ButtonRow");

                Assert.AreEqual(0, bootstrap.Session.Map.GetCurrentLocationIndex());

                var moveButton = FindButtonByLabelPrefix(buttonRow, "1.");
                Assert.IsNotNull(moveButton, "지역 0(던전 입구)의 '1. 던전에 들어간다' 버튼을 찾을 수 없습니다.");

                moveButton.onClick.Invoke();
                yield return null;

                Assert.AreEqual(1, bootstrap.Session.Map.GetCurrentLocationIndex(),
                    "이동 버튼 클릭 후 실제로 지역 1(갈림길)로 이동해야 합니다.");

                var titleText = exploreController.transform.Find("TextBar/LocationTitle").GetComponent<TMP_Text>();
                Assert.AreEqual("갈림길", titleText.text);
            }
            finally
            {
                RestoreSaveFile(hadExisting, backup);
            }
        }

        // ----------------------------------------------------------------
        // 테스트 F: 고블린 전투 진입 → 포트레이트 렌더링 + 공격/마나스킬 버튼 실동작 검증
        // ----------------------------------------------------------------
        [UnityTest]
        public IEnumerator F_GoblinBattle_PortraitRendersAndAttackButtonsWork()
        {
            bool hadExisting = false;
            string backup = null;
            try
            {
                backup = BackupSaveFileIfExists(out hadExisting);
                DeleteSaveFileIfExists();

                yield return LoadMainScene();
                yield return SelectWarriorAndConfirm();

                var bootstrap = FindBootstrap();
                var exploreController = FindController<ExplorePanelController>();
                var buttonRow = exploreController.transform.Find("ButtonRow");

                // 던전 입구(0) → "1. 던전에 들어간다" → 갈림길(1)
                FindButtonByLabelPrefix(buttonRow, "1.").onClick.Invoke();
                yield return null;
                Assert.AreEqual(1, bootstrap.Session.Map.GetCurrentLocationIndex());

                // 갈림길(1) → "2. 오른쪽 통로로 간다" → 어두운 통로(3), 고블린 즉시 조우(ProcessLocationEntry)
                var rightPathButton = FindButtonByLabelPrefix(buttonRow, "2.");
                Assert.IsNotNull(rightPathButton, "갈림길의 '2. 오른쪽 통로로 간다' 버튼을 찾을 수 없습니다.");
                rightPathButton.onClick.Invoke();
                yield return null;

                Assert.AreEqual(GameState.BATTLE, bootstrap.Session.CurrentState, "고블린과 즉시 전투가 시작되어야 합니다.");
                var enemy = bootstrap.Session.CurrentEnemy;
                Assert.IsNotNull(enemy, "전투 상대(고블린)가 설정되어 있어야 합니다.");
                Assert.AreEqual("고블린", enemy.GetName());

                // DEC-124: 적 포트레이트가 실제로 화면에 그려지는지(Image가 활성화되고 sprite != null)
                var enemyPortraitImage = exploreController.transform.Find("EnemyPortrait").GetComponent<Image>();
                Assert.IsTrue(enemyPortraitImage.enabled, "전투 중에는 적 포트레이트 Image가 활성화되어야 합니다.");
                Assert.IsNotNull(enemyPortraitImage.sprite,
                    $"적 포트레이트 스프라이트가 null입니다(변종 파일: {(enemy.PortraitVariants != null && enemy.PortraitVariants.Length > 0 ? enemy.PortraitVariants[0] : "없음")}).");

                // DEC-131 후속 수정: 사용자가 전투 화면 스크린샷으로 적 포트레이트가 화면 하단에
                // 잘리고 액션 버튼(ButtonRow)과 겹쳐 보인다고 실제로 보고했다 — ButtonRow 배경은
                // 완전 투명(CreateButton의 배경색 alpha=0)이라 겹치면 몬스터 그림이 버튼 뒤로 그대로
                // 비쳐 보인다. 좌표를 하드코딩 비교하지 않고, 두 RectTransform의 세로 범위(pivot
                // 0.5 기준 anchoredPosition.y ± sizeDelta.y/2)가 서로 겹치지 않는지로 검증한다.
                var enemyPortraitRT = enemyPortraitImage.GetComponent<RectTransform>();
                var buttonRowRT = buttonRow.GetComponent<RectTransform>();
                Assert.IsFalse(VerticalRangesOverlap(enemyPortraitRT, buttonRowRT),
                    $"적 포트레이트가 액션 버튼 행(ButtonRow)과 겹칩니다 " +
                    $"(EnemyPortrait {DescribeVerticalRange(enemyPortraitRT)}, ButtonRow {DescribeVerticalRange(buttonRowRT)}).");

                // DEC-123: 마나가 충분(전사 마나10 >= 강타 비용5)하면 마나 스킬 버튼("3. 강타 (마나 5)")이 실제로 나타나야 함
                var manaSkillButton = FindButtonByLabelPrefix(buttonRow, "3.");
                Assert.IsNotNull(manaSkillButton, "마나가 충분한데도 마나 스킬 버튼('3. 강타')이 보이지 않습니다.");

                int manaBefore = bootstrap.Session.Player.GetMana();
                manaSkillButton.onClick.Invoke();
                yield return null;

                Assert.Less(bootstrap.Session.Player.GetMana(), manaBefore, "마나 스킬 버튼 클릭 후 실제로 마나가 소모되어야 합니다.");

                // 전투가 아직 계속되고 있다면(고블린이 강타 한 방에 죽지 않은 경우) 일반 공격도 실제로
                // 적 HP를 깎는지(또는 전투를 끝내는지) 확인한다.
                if (bootstrap.Session.CurrentState == GameState.BATTLE)
                {
                    int enemyHpBefore = bootstrap.Session.CurrentEnemy.GetHp();
                    var attackButton = FindButtonByLabelPrefix(buttonRow, "1.");
                    Assert.IsNotNull(attackButton, "'1. 일반 공격' 버튼을 찾을 수 없습니다.");

                    attackButton.onClick.Invoke();
                    yield return null;

                    bool battleEnded = bootstrap.Session.CurrentState != GameState.BATTLE;
                    bool enemyDamaged = !battleEnded && bootstrap.Session.CurrentEnemy.GetHp() < enemyHpBefore;
                    Assert.IsTrue(battleEnded || enemyDamaged,
                        "일반 공격 버튼 클릭 후 전투 상태가 실제로 진행(적 HP 감소 또는 전투 종료)되어야 합니다.");
                }
            }
            finally
            {
                RestoreSaveFile(hadExisting, backup);
            }
        }

        // ----------------------------------------------------------------
        // 테스트 G: 직업 카드 선택 시 잉크마크(InkMarkOverlay) activeSelf/fillAmount 실제 변화(DEC-127)
        // ----------------------------------------------------------------
        [UnityTest]
        public IEnumerator G_ClassSelect_CardSelection_TogglesInkMarkOverlay()
        {
            bool hadExisting = false;
            string backup = null;
            // 이 테스트는 WaitForSeconds로 실제 시간을 기다린다 — ConsumeKnownEditorNoiseIfMatched
            // 참조(알려진 무관한 TMP 임포터 창 에러 로그만 정확히 소비, 그 외 진짜 에러는 그대로 실패).
            Application.logMessageReceived += ConsumeKnownEditorNoiseIfMatched;
            try
            {
                backup = BackupSaveFileIfExists(out hadExisting);
                DeleteSaveFileIfExists();

                yield return LoadMainScene();

                var titleController = FindController<TitlePanelController>();
                var classSelectController = FindController<ClassSelectPanelController>();

                titleController.transform.Find("ParchmentCard/NewGameButton").GetComponent<Button>().onClick.Invoke();
                yield return null;

                var warriorCard = classSelectController.transform.Find("Card_warrior");
                var rogueCard = classSelectController.transform.Find("Card_rogue");
                var warriorInk = warriorCard.GetComponentInChildren<InkMarkOverlay>(true);
                var rogueInk = rogueCard.GetComponentInChildren<InkMarkOverlay>(true);
                Assert.IsNotNull(warriorInk, "전사 카드에 InkMarkOverlay가 연결되어 있어야 합니다.");
                Assert.IsNotNull(rogueInk, "도적 카드에 InkMarkOverlay가 연결되어 있어야 합니다.");

                Assert.IsFalse(warriorInk.gameObject.activeSelf, "선택 전에는 잉크마크가 숨겨져 있어야 합니다.");
                Assert.IsFalse(rogueInk.gameObject.activeSelf);

                warriorCard.GetComponent<Button>().onClick.Invoke();
                yield return null;

                Assert.IsTrue(warriorInk.gameObject.activeSelf, "전사 카드를 선택하면 잉크마크가 표시되어야 합니다(DEC-118/127).");
                Assert.IsFalse(rogueInk.gameObject.activeSelf, "선택하지 않은 카드는 잉크마크가 표시되면 안 됩니다.");

                var warriorRing = warriorInk.GetComponent<Image>();
                float fillEarly = warriorRing.fillAmount;
                Assert.Less(fillEarly, 1f, "선택 직후에는 원이 아직 다 그려지지 않은 상태(fillAmount < 1)여야 합니다.");

                yield return new WaitForSeconds(0.25f);
                Assert.Greater(warriorRing.fillAmount, fillEarly,
                    "시간이 지나면 잉크마크 fillAmount가 더 커져야 합니다(원이 그려지는 중, DEC-118/127).");

                // 다른 카드로 선택을 바꾸면 이전 카드의 잉크마크는 사라지고 새 카드에 나타나야 함
                rogueCard.GetComponent<Button>().onClick.Invoke();
                yield return null;

                Assert.IsFalse(warriorInk.gameObject.activeSelf, "다른 카드를 선택하면 이전 카드의 잉크마크는 사라져야 합니다.");
                Assert.IsTrue(rogueInk.gameObject.activeSelf, "새로 선택한 카드에 잉크마크가 표시되어야 합니다.");
            }
            finally
            {
                RestoreSaveFile(hadExisting, backup);
                Application.logMessageReceived -= ConsumeKnownEditorNoiseIfMatched;
            }
        }

        // ----------------------------------------------------------------
        // 테스트 H: 결과 화면 깃펜 필기 연출(QuillRevealText)의 maxVisibleCharacters/진행률이
        // 시간에 따라 실제로 증가하는지(DEC-121/127)
        // ----------------------------------------------------------------
        [UnityTest]
        public IEnumerator H_ResultPanel_QuillReveal_ProgressesOverTimeAndCompletes()
        {
            // 테스트 G와 동일한 이유로 ConsumeKnownEditorNoiseIfMatched를 구독한다(알려진 무관한
            // 로그만 정확히 소비, 그 외 진짜 에러는 그대로 실패).
            Application.logMessageReceived += ConsumeKnownEditorNoiseIfMatched;
            try
            {
                yield return LoadMainScene();

                var resultController = FindController<ResultPanelController>();
                var quill = resultController.GetComponentInChildren<QuillRevealText>(true);
                Assert.IsNotNull(quill, "ResultPanel의 서술 텍스트에 QuillRevealText가 연결되어 있어야 합니다.");

                var text = quill.GetComponent<TMP_Text>();
                Assert.IsNotNull(text, "QuillRevealText와 같은 오브젝트에 TMP_Text가 있어야 합니다.");

                // 코루틴은 비활성 GameObject에서 시작할 수 없다 — 실제 흐름(GameBootstrap.ShowResult()가
                // SetActivePanel로 먼저 켠 뒤 Refresh()를 호출)과 동일하게 패널을 먼저 활성화한다.
                resultController.gameObject.SetActive(true);

                const string sample = "던전의 주인을 물리치고 던전을 클리어했습니다.";
                quill.Play(sample);
                // StartCoroutine 호출 시점에 첫 yield 전까지는 동기 실행되므로(Unity 코루틴 특성),
                // Play() 직후에도 이미 한 프레임 분량만큼 진행되어 있을 수 있다 — "정확히 0"이 아니라
                // "아직 완료 전(1 미만)"만 확인하고, 이후 진행률이 실제로 더 커지는지로 검증한다.
                float progressEarly = quill.Progress;
                int visibleEarly = text.maxVisibleCharacters;
                Assert.Less(progressEarly, 1f, "재생 시작 직후에는 아직 완료 전이어야 합니다.");

                yield return new WaitForSeconds(0.3f);
                float progressMid = quill.Progress;
                int visibleMid = text.maxVisibleCharacters;
                Assert.Greater(progressMid, progressEarly, "시간이 지나면 진행률이 더 커져야 합니다.");
                Assert.GreaterOrEqual(visibleMid, visibleEarly, "시간이 지나면 maxVisibleCharacters가 줄어들면 안 됩니다.");

                yield return new WaitForSeconds(1.5f);
                Assert.AreEqual(1f, quill.Progress, 0.001f, "충분한 시간이 지나면 진행률이 1(완료)이 되어야 합니다.");
                Assert.AreEqual(sample, text.text, "전체 문장이 그대로 설정되어 있어야 합니다(clip-path 근사이므로 텍스트 자체는 항상 전체가 들어있음).");
            }
            finally
            {
                Application.logMessageReceived -= ConsumeKnownEditorNoiseIfMatched;
            }
        }

        // ----------------------------------------------------------------
        // 테스트 I: 씬의 실제 한글 TMP 텍스트가 "깨지지 않고 렌더링 가능한 폰트"를
        // 실제로 갖는지(DEC-131 후속 — 사용자가 실제 빌드에서 "글씨가 다 깨짐"으로
        // 보고한 문제, LiberationSans SDF는 라틴 전용이라 한글이 tofu(□)로 대체됨)
        // ----------------------------------------------------------------
        [UnityTest]
        public IEnumerator I_KoreanText_ResolvesToFontsThatActuallyHaveHangulGlyphs()
        {
            bool hadExisting = false;
            string backup = null;
            try
            {
                backup = BackupSaveFileIfExists(out hadExisting);
                DeleteSaveFileIfExists();

                int checkedTextCount = 0;
                int checkedCharCount = 0;
                var missing = new System.Collections.Generic.List<string>();

                yield return LoadMainScene();

                // 타이틀 화면(로드 직후 활성 상태)
                CheckActiveKoreanText(missing, ref checkedTextCount, ref checkedCharCount);

                yield return SelectWarriorAndConfirm();

                // 직업 선택 화면 클릭 도중 잠깐 활성화됐던 화면은 SelectWarriorAndConfirm 내부에서
                // 이미 지나갔으므로, 여기서는 그 결과로 지금 활성화된 탐색 화면을 확인한다.
                CheckActiveKoreanText(missing, ref checkedTextCount, ref checkedCharCount);

                // 던전 입구(0)→갈림길(1)→오른쪽 통로(고블린 전투)까지 진행해 탐색·전투 화면의
                // 한글 TMP_Text(동적 생성 버튼 라벨 포함)까지 커버한다.
                var exploreController = FindController<ExplorePanelController>();
                var buttonRow = exploreController.transform.Find("ButtonRow");
                FindButtonByLabelPrefix(buttonRow, "1.")?.onClick.Invoke();
                yield return null;
                CheckActiveKoreanText(missing, ref checkedTextCount, ref checkedCharCount);

                FindButtonByLabelPrefix(buttonRow, "2.")?.onClick.Invoke();
                yield return null;
                CheckActiveKoreanText(missing, ref checkedTextCount, ref checkedCharCount);

                // ResultPanel(승리/패배 화면)은 이번 시나리오에서 자연스럽게 도달하지 않으므로,
                // 테스트 H와 동일한 방식(패널을 직접 SetActive(true))으로 강제 활성화해 Awake/OnEnable을
                // 트리거한 뒤 그 안의 한글 텍스트도 함께 검증한다.
                var resultController = FindController<ResultPanelController>();
                bool resultWasActive = resultController.gameObject.activeSelf;
                resultController.gameObject.SetActive(true);
                yield return null;
                CheckActiveKoreanText(missing, ref checkedTextCount, ref checkedCharCount);
                resultController.gameObject.SetActive(resultWasActive);

                Assert.Greater(checkedTextCount, 0,
                    "이번에 진행한 화면들에서 비ASCII(한글 등) 텍스트를 가진 TMP_Text를 하나도 못 찾았습니다 — " +
                    "테스트가 실제로 아무것도 검증하지 못한 것일 수 있습니다.");
                Assert.Greater(checkedCharCount, 0);

                Assert.IsEmpty(missing,
                    $"다음 문자들이 실제 렌더링에 쓰이는 폰트(폴백 포함)에 글리프가 없어 깨져 보일 것입니다: " +
                    string.Join(", ", missing));
            }
            finally
            {
                RestoreSaveFile(hadExisting, backup);
            }
        }

        // ----------------------------------------------------------------
        // 테스트 J: 직업 선택 카드의 이름(Name)/스탯(Stats)/아이템(Items) 텍스트가 서로 겹치지
        // 않는지(DEC-131 후속 — 사용자가 실제 스크린샷으로 "전사"와 "HP114..."가 뒤엉켜 보이는
        // 것을 확인해 보고한 레이아웃 버그. ProjectSetupTool.BuildClassSelectPanel의
        // anchoredPosition 겹침이 원인이었다). 씬을 다시 생성해도 이 겹침이 재발하면 이 테스트가
        // 잡아내도록, 실제 좌표값을 하드코딩해 비교하는 대신 RectTransform의 계산된 세로 범위를
        // 서로 비교한다.
        // ----------------------------------------------------------------
        [UnityTest]
        public IEnumerator J_ClassSelectCards_NameStatsItemsText_DoNotOverlapVertically()
        {
            yield return LoadMainScene();

            var classSelectController = FindController<ClassSelectPanelController>();
            Assert.IsNotNull(classSelectController, "ClassSelectPanelController를 찾을 수 없습니다.");

            string[] ids = { "warrior", "rogue", "mage" };
            int checkedCards = 0;

            foreach (var id in ids)
            {
                var card = classSelectController.transform.Find($"Card_{id}");
                if (card == null)
                {
                    continue;
                }

                var nameRT = card.Find("Name").GetComponent<RectTransform>();
                var statsRT = card.Find("Stats").GetComponent<RectTransform>();
                var itemsRT = card.Find("Items").GetComponent<RectTransform>();

                Assert.IsFalse(VerticalRangesOverlap(nameRT, statsRT),
                    $"'{id}' 카드에서 이름(Name) 텍스트와 스탯(Stats) 텍스트가 세로로 겹칩니다 " +
                    $"(Name {DescribeVerticalRange(nameRT)}, Stats {DescribeVerticalRange(statsRT)}).");

                Assert.IsFalse(VerticalRangesOverlap(statsRT, itemsRT),
                    $"'{id}' 카드에서 스탯(Stats) 텍스트와 아이템(Items) 텍스트가 세로로 겹칩니다 " +
                    $"(Stats {DescribeVerticalRange(statsRT)}, Items {DescribeVerticalRange(itemsRT)}).");

                checkedCards++;
            }

            Assert.AreEqual(3, checkedCards, "직업 카드 3개(전사/도적/마법사) 전부 확인했어야 합니다.");
        }

        /// <summary>
        /// pivot이 (0.5, 0.5)인 RectTransform 기준, anchoredPosition.y와 sizeDelta.y로부터
        /// [하단, 상단] 세로 범위를 계산한다. ProjectSetupTool의 카드 자식 텍스트들은 전부
        /// 기본 pivot(0.5,0.5)으로 생성되므로 이 가정이 유효하다.
        /// </summary>
        private static (float bottom, float top) GetVerticalRange(RectTransform rt)
        {
            float half = rt.sizeDelta.y / 2f;
            float centerY = rt.anchoredPosition.y;
            return (centerY - half, centerY + half);
        }

        private static bool VerticalRangesOverlap(RectTransform a, RectTransform b)
        {
            var (bottomA, topA) = GetVerticalRange(a);
            var (bottomB, topB) = GetVerticalRange(b);
            return topA > bottomB && topB > bottomA;
        }

        private static string DescribeVerticalRange(RectTransform rt)
        {
            var (bottom, top) = GetVerticalRange(rt);
            return $"[{bottom:F0}, {top:F0}]";
        }

        /// <summary>
        /// 테스트 I 전용 헬퍼: "지금 실제로 화면에 활성 상태인" TMP_Text만 검사 대상으로 삼는다
        /// (아직 한 번도 활성화된 적 없는 GameObject는 TMP_Text.Awake/OnEnable이 아직 실행되지
        /// 않아 font가 원래 null인 게 정상 — 이건 DEC-131이 고친 버그가 아니라 Unity 생명주기
        /// 특성이라, 여기 포함시키면 실제 화면에 보이지도 않는 텍스트 때문에 오탐이 난다).
        /// </summary>
        private static void CheckActiveKoreanText(System.Collections.Generic.List<string> missing, ref int checkedTextCount, ref int checkedCharCount)
        {
            var allTexts = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            foreach (var t in allTexts)
            {
                if (string.IsNullOrEmpty(t.text))
                {
                    continue;
                }

                // DEC-131 1차 수정(NullReferenceException) 재확인: 실제로 화면에 활성화된
                // 컴포넌트는 자신에게 폰트가 배정 안 돼 있어도(Main.unity의 29개 컴포넌트 전부
                // m_fontAsset: {fileID: 0}) TMP_Settings.defaultFontAsset 폴백으로 null이 아니어야 한다.
                Assert.IsNotNull(t.font, $"'{t.name}'의 font가 null입니다 — TMP_Settings.defaultFontAsset 폴백이 깨졌습니다.");

                bool textHasNonAscii = false;
                foreach (char c in t.text)
                {
                    if (c <= 0x7E)
                    {
                        continue; // ASCII(라틴/숫자/기본 문장부호)는 LiberationSans SDF로 충분 — 검증 대상 아님.
                    }

                    textHasNonAscii = true;
                    checkedCharCount++;

                    bool hasGlyph = t.font.HasCharacter(c, searchFallbacks: true, tryAddCharacter: true);
                    if (!hasGlyph)
                    {
                        string entry = $"'{t.name}' 텍스트의 '{c}'(U+{(int)c:X4})";
                        if (!missing.Contains(entry))
                        {
                            missing.Add(entry);
                        }
                    }
                }

                if (textHasNonAscii)
                {
                    checkedTextCount++;
                }
            }
        }

        /// <summary>테스트 E/F 공용 준비 단계: 새 게임 → 전사 선택 → 확정까지 실제 버튼 클릭으로 진행한다.</summary>
        private static IEnumerator SelectWarriorAndConfirm()
        {
            var titleController = FindController<TitlePanelController>();
            var classSelectController = FindController<ClassSelectPanelController>();

            titleController.transform.Find("ParchmentCard/NewGameButton").GetComponent<Button>().onClick.Invoke();
            yield return null;

            classSelectController.transform.Find("Card_warrior").GetComponent<Button>().onClick.Invoke();
            yield return null;

            classSelectController.transform.Find("ConfirmButton").GetComponent<Button>().onClick.Invoke();
            yield return null;
        }

        /// <summary>테스트 K/L/M 공용 준비 단계: 던전 입구(0) → 갈림길(1) → 오른쪽 통로(고블린 즉시 조우)까지 진행한다.</summary>
        private static IEnumerator EnterGoblinBattle(ExplorePanelController exploreController)
        {
            var buttonRow = exploreController.transform.Find("ButtonRow");
            FindButtonByLabelPrefix(buttonRow, "1.").onClick.Invoke();
            yield return null;
            FindButtonByLabelPrefix(buttonRow, "2.").onClick.Invoke();
            yield return null;
        }

        // ----------------------------------------------------------------
        // 테스트 K: 전투 진입 시 플레이어 캐릭터 전신 이미지가 실제로 표시되고(대립 구도, DEC-116/132),
        // 탐색 화면에서는 표시되지 않는지 검증
        // ----------------------------------------------------------------
        [UnityTest]
        public IEnumerator K_GoblinBattle_PlayerPortraitRendersWithCorrectClassSprite()
        {
            bool hadExisting = false;
            string backup = null;
            try
            {
                backup = BackupSaveFileIfExists(out hadExisting);
                DeleteSaveFileIfExists();

                yield return LoadMainScene();
                yield return SelectWarriorAndConfirm();

                var bootstrap = FindBootstrap();
                var exploreController = FindController<ExplorePanelController>();

                // 탐색 화면(전투 진입 전)에는 플레이어 전투 비주얼이 보이면 안 된다.
                var playerPortraitImage = exploreController.transform.Find("PlayerPortrait").GetComponent<Image>();
                Assert.IsFalse(playerPortraitImage.enabled, "탐색 화면에서는 PlayerPortrait이 비활성 상태여야 합니다.");

                yield return EnterGoblinBattle(exploreController);
                Assert.AreEqual(GameState.BATTLE, bootstrap.Session.CurrentState, "고블린과 즉시 전투가 시작되어야 합니다.");

                Assert.IsTrue(playerPortraitImage.enabled, "전투 중에는 PlayerPortrait Image가 활성화되어야 합니다(DEC-132 대립 구도).");
                Assert.IsNotNull(playerPortraitImage.sprite,
                    $"플레이어 클래스({bootstrap.Session.Player.ClassId})에 대응하는 스프라이트가 할당되어 있어야 합니다.");

                // 대립 구도: 몬스터(EnemyPortrait, +x)와 플레이어(PlayerPortrait, -x)가 서로 반대편에 있어야 한다.
                var enemyPortraitRT = exploreController.transform.Find("EnemyPortrait").GetComponent<RectTransform>();
                var playerPortraitRT = playerPortraitImage.GetComponent<RectTransform>();
                Assert.Greater(enemyPortraitRT.anchoredPosition.x, 0f, "몬스터 포트레이트는 화면 오른쪽(양수 x)에 있어야 합니다.");
                Assert.Less(playerPortraitRT.anchoredPosition.x, 0f, "플레이어 포트레이트는 화면 왼쪽(음수 x, 몬스터의 반대편)에 있어야 합니다.");
            }
            finally
            {
                RestoreSaveFile(hadExisting, backup);
            }
        }

        // ----------------------------------------------------------------
        // 테스트 L: 전투 중 "가방"에서 포션을 사용하면 실제로 HP가 회복되고, 아이템이 소모되며,
        // 턴이 적에게 넘어가는지(밸런스 유지, DEC-132) 버튼 클릭으로 검증
        // ----------------------------------------------------------------
        [UnityTest]
        public IEnumerator L_BattleBag_UsePotion_RestoresHpAndConsumesTurn()
        {
            bool hadExisting = false;
            string backup = null;
            try
            {
                backup = BackupSaveFileIfExists(out hadExisting);
                DeleteSaveFileIfExists();

                yield return LoadMainScene();
                yield return SelectWarriorAndConfirm();

                var bootstrap = FindBootstrap();
                var exploreController = FindController<ExplorePanelController>();
                yield return EnterGoblinBattle(exploreController);
                Assert.AreEqual(GameState.BATTLE, bootstrap.Session.CurrentState);

                // 전사 DEF=5, 고블린 4변종 중 최댓값(ATK10+Random(0,2)=최대12, 12-5=7)이 나와도 회복량(30)보다
                // 훨씬 작으므로, HP를 미리 5로 낮춰두면 결과 HP가 (5+30-7)=28 이상 (5+30-1)=34 이하 범위에
                // 항상 들어와 랜덤 변종과 무관하게 결정적으로 검증 가능하다(RegressionSmokeTest의 동일 근거 재사용).
                bootstrap.Session.Player.SetHp(5);
                int itemCountBefore = bootstrap.Session.Inventory.GetItemCount();
                int roundBefore = bootstrap.Session.CurrentBattle.Round;

                var buttonRow = exploreController.transform.Find("ButtonRow");
                var bagButton = FindButtonByLabelPrefix(buttonRow, "5. 가방");
                Assert.IsNotNull(bagButton, "'5. 가방' 버튼을 찾을 수 없습니다.");
                Assert.IsTrue(bagButton.interactable, "회복 물약을 갖고 있으므로 가방 버튼은 활성화되어 있어야 합니다.");

                bagButton.onClick.Invoke();
                yield return null;

                var potionButton = FindButtonByLabelPrefix(buttonRow, "회복 물약");
                Assert.IsNotNull(potionButton, "가방을 열면 '회복 물약' 사용 버튼이 보여야 합니다.");

                potionButton.onClick.Invoke();
                yield return null;

                Assert.AreEqual(itemCountBefore - 1, bootstrap.Session.Inventory.GetItemCount(),
                    "사용한 포션은 인벤토리에서 소모되어야 합니다.");
                Assert.AreEqual(GameState.BATTLE, bootstrap.Session.CurrentState, "이 시나리오에서는 전투가 계속되어야 합니다.");
                Assert.AreEqual(roundBefore + 1, bootstrap.Session.CurrentBattle.Round,
                    "포션 사용도 한 턴으로 취급되어 Round가 1 증가해야 합니다(밸런스 유지 — 턴 소모).");
                Assert.GreaterOrEqual(bootstrap.Session.Player.GetHp(), 28);
                Assert.LessOrEqual(bootstrap.Session.Player.GetHp(), 34);

                // 아이템 사용 후에는 가방 서브 메뉴가 아니라 원래 전투 행동 선택지로 돌아와 있어야 한다.
                Assert.IsNotNull(FindButtonByLabelPrefix(buttonRow, "1. 일반 공격"),
                    "포션 사용 후에는 다시 전투 행동 선택지('1. 일반 공격')로 돌아와야 합니다.");
                Assert.IsNull(FindButtonByLabelPrefix(buttonRow, "뒤로"),
                    "포션 사용 후에는 가방 서브 메뉴('뒤로' 버튼)가 닫혀 있어야 합니다.");
            }
            finally
            {
                RestoreSaveFile(hadExisting, backup);
            }
        }

        // ----------------------------------------------------------------
        // 테스트 M: 사용 가능한 포션이 없으면 "가방" 선택지가 비활성화되는지(DEC-132) 검증
        // ----------------------------------------------------------------
        [UnityTest]
        public IEnumerator M_BattleBag_NoPotions_DisablesBagOption()
        {
            bool hadExisting = false;
            string backup = null;
            try
            {
                backup = BackupSaveFileIfExists(out hadExisting);
                DeleteSaveFileIfExists();

                yield return LoadMainScene();
                yield return SelectWarriorAndConfirm();

                var bootstrap = FindBootstrap();
                var exploreController = FindController<ExplorePanelController>();

                // 전투 진입 전(탐색 화면)에 시작 포션("회복 물약")을 직접 소모시켜 가방을 비운다.
                var inventory = bootstrap.Session.Inventory;
                int potionIndex = -1;
                for (int i = 0; i < inventory.GetItemCount(); i++)
                {
                    if (inventory.GetItem(i).GetItemType() == ItemType.POTION)
                    {
                        potionIndex = i;
                        break;
                    }
                }
                Assert.GreaterOrEqual(potionIndex, 0, "시작 인벤토리에 포션(회복 물약)이 있어야 합니다.");
                bootstrap.Session.UseItem(potionIndex);
                Assert.IsFalse(bootstrap.Session.HasUsablePotion(), "유일한 포션을 사용한 뒤에는 사용 가능한 포션이 없어야 합니다.");

                yield return EnterGoblinBattle(exploreController);
                Assert.AreEqual(GameState.BATTLE, bootstrap.Session.CurrentState);

                var buttonRow = exploreController.transform.Find("ButtonRow");
                var bagButton = FindButtonByLabelPrefix(buttonRow, "5. 가방");
                Assert.IsNotNull(bagButton, "포션이 없어도 '5. 가방' 버튼 자체는 보여야 합니다(비활성화 방식).");
                Assert.IsFalse(bagButton.interactable, "사용 가능한 포션이 없으면 가방 버튼은 비활성화되어야 합니다.");
            }
            finally
            {
                RestoreSaveFile(hadExisting, backup);
            }
        }

        // ----------------------------------------------------------------
        // 테스트 N: 전투 중 "1. 일반 공격" 클릭 시 실제로 피격 플래시(HitFlashEffect)와
        // 데미지 숫자 팝업(DamagePopupText)이 양쪽(적/플레이어) 모두 트리거되고, 표시된
        // 숫자가 실제 HP 변화량과 정확히 일치하는지 검증(DEC-133 신규).
        //
        // 고블린 4변종 모두 HP가 최소 20이고 전사의 일반 공격(ATK13+Random(0,3))으로는
        // 한 방에 죽지 않으며(변종별 def 고려해도 최종 피해 10~16, HP20~45), 전사 HP(114)도
        // 고블린 반격 한 방으로는 죽지 않으므로 "양쪽 다 이번 턴에 피해를 입는다"가
        // 변종·랜덤 롤과 무관하게 항상 성립한다(테스트 L과 동일한 결정성 확보 방식).
        // ----------------------------------------------------------------
        [UnityTest]
        public IEnumerator N_GoblinBattle_AttackTriggersHitFlashAndDamagePopupsOnBothSides()
        {
            bool hadExisting = false;
            string backup = null;
            Application.logMessageReceived += ConsumeKnownEditorNoiseIfMatched;
            try
            {
                backup = BackupSaveFileIfExists(out hadExisting);
                DeleteSaveFileIfExists();

                yield return LoadMainScene();
                yield return SelectWarriorAndConfirm();

                var bootstrap = FindBootstrap();
                var exploreController = FindController<ExplorePanelController>();
                yield return EnterGoblinBattle(exploreController);
                Assert.AreEqual(GameState.BATTLE, bootstrap.Session.CurrentState);

                var enemyPortrait = exploreController.transform.Find("EnemyPortrait");
                var playerPortrait = exploreController.transform.Find("PlayerPortrait");
                var enemyImage = enemyPortrait.GetComponent<Image>();
                var playerImage = playerPortrait.GetComponent<Image>();
                var enemyHitFlash = enemyPortrait.GetComponent<HitFlashEffect>();
                var playerHitFlash = playerPortrait.GetComponent<HitFlashEffect>();
                var enemyDamagePopup = enemyPortrait.GetComponentInChildren<DamagePopupText>(true);
                var playerDamagePopup = playerPortrait.GetComponentInChildren<DamagePopupText>(true);

                Assert.IsNotNull(enemyHitFlash, "EnemyPortrait에 HitFlashEffect가 연결되어 있어야 합니다.");
                Assert.IsNotNull(playerHitFlash, "PlayerPortrait에 HitFlashEffect가 연결되어 있어야 합니다.");
                Assert.IsNotNull(enemyDamagePopup, "EnemyPortrait 하위에 DamagePopupText가 연결되어 있어야 합니다.");
                Assert.IsNotNull(playerDamagePopup, "PlayerPortrait 하위에 DamagePopupText가 연결되어 있어야 합니다.");

                Color enemyOriginalColor = enemyImage.color;
                Color playerOriginalColor = playerImage.color;
                Assert.IsFalse(enemyHitFlash.IsPlaying, "공격 전에는 피격 플래시가 재생 중이면 안 됩니다.");
                Assert.IsFalse(enemyDamagePopup.gameObject.activeSelf, "공격 전에는 데미지 팝업이 비활성 상태여야 합니다.");

                int enemyHpBefore = bootstrap.Session.CurrentEnemy.GetHp();
                int playerHpBefore = bootstrap.Session.Player.GetHp();

                var buttonRow = exploreController.transform.Find("ButtonRow");
                var attackButton = FindButtonByLabelPrefix(buttonRow, "1.");
                Assert.IsNotNull(attackButton, "'1. 일반 공격' 버튼을 찾을 수 없습니다.");

                attackButton.onClick.Invoke();

                Assert.AreEqual(GameState.BATTLE, bootstrap.Session.CurrentState,
                    "이 테스트가 유효하려면 한 번의 공격 교환으로 전투가 끝나지 않아야 합니다(설계상 항상 성립).");

                int enemyHpAfter = bootstrap.Session.CurrentEnemy.GetHp();
                int playerHpAfter = bootstrap.Session.Player.GetHp();
                Assert.Less(enemyHpAfter, enemyHpBefore, "적이 이번 턴에 실제로 피해를 입어야 합니다.");
                Assert.Less(playerHpAfter, playerHpBefore, "플레이어도 적의 반격으로 실제로 피해를 입어야 합니다.");

                // 클릭 처리(코루틴 StartCoroutine)는 onClick.Invoke() 안에서 첫 yield 전까지 동기
                // 실행되므로, 이 시점에 이미 플래시 색이 바뀌어 있고 코루틴이 진행 중이어야 한다.
                Assert.IsTrue(enemyHitFlash.IsPlaying, "적이 피해를 입으면 EnemyHitFlash 코루틴이 즉시 실행 중이어야 합니다.");
                Assert.IsTrue(playerHitFlash.IsPlaying, "플레이어가 피해를 입으면 PlayerHitFlash 코루틴이 즉시 실행 중이어야 합니다.");
                Assert.AreEqual((Color)UIColors.HitFlash, enemyImage.color, "피격 직후 EnemyPortrait 색이 HitFlash 색이어야 합니다.");
                Assert.AreEqual((Color)UIColors.HitFlash, playerImage.color, "피격 직후 PlayerPortrait 색이 HitFlash 색이어야 합니다.");

                Assert.IsTrue(enemyDamagePopup.gameObject.activeSelf, "적 데미지 팝업이 활성화되어 있어야 합니다.");
                Assert.IsTrue(playerDamagePopup.gameObject.activeSelf, "플레이어 데미지 팝업이 활성화되어 있어야 합니다.");
                Assert.AreEqual($"-{enemyHpBefore - enemyHpAfter}", enemyDamagePopup.CurrentText,
                    "적 데미지 팝업 숫자가 실제 HP 감소량과 정확히 일치해야 합니다.");
                Assert.AreEqual($"-{playerHpBefore - playerHpAfter}", playerDamagePopup.CurrentText,
                    "플레이어 데미지 팝업 숫자가 실제 HP 감소량과 정확히 일치해야 합니다.");

                // 지속시간(HitFlashEffect 0.18초, DamagePopupText 0.7초)이 모두 지나면 원상 복귀해야 한다.
                yield return new WaitForSeconds(0.9f);

                Assert.IsFalse(enemyHitFlash.IsPlaying, "충분한 시간이 지나면 피격 플래시 코루틴이 끝나 있어야 합니다.");
                Assert.IsFalse(playerHitFlash.IsPlaying);
                Assert.AreEqual(enemyOriginalColor, enemyImage.color, "플래시가 끝나면 EnemyPortrait 색이 원래대로 복원돼야 합니다.");
                Assert.AreEqual(playerOriginalColor, playerImage.color, "플래시가 끝나면 PlayerPortrait 색이 원래대로 복원돼야 합니다.");
                Assert.IsFalse(enemyDamagePopup.gameObject.activeSelf, "충분한 시간이 지나면 데미지 팝업이 다시 비활성화돼야 합니다.");
                Assert.IsFalse(playerDamagePopup.gameObject.activeSelf);
            }
            finally
            {
                RestoreSaveFile(hadExisting, backup);
                Application.logMessageReceived -= ConsumeKnownEditorNoiseIfMatched;
            }
        }

        // ----------------------------------------------------------------
        // 테스트 O: 짧은 시간 안에 Flash()/Show()를 연속 호출해도 이전 이펙트의 잔상이
        // 남지 않고 깨끗하게 새로 시작되는지(DEC-133 신규 — "연속 공격 시 이전 이펙트가
        // 남아있지 않고 새로 시작"). 실제 BattleSystem RNG에 의존하지 않도록, 씬에 이미
        // 배치된 컴포넌트를 직접 호출해 결정적으로 검증한다.
        // ----------------------------------------------------------------
        [UnityTest]
        public IEnumerator O_HitFlashAndDamagePopup_RepeatedCalls_RestartCleanlyWithoutResidue()
        {
            bool hadExisting = false;
            string backup = null;
            Application.logMessageReceived += ConsumeKnownEditorNoiseIfMatched;
            try
            {
                backup = BackupSaveFileIfExists(out hadExisting);
                DeleteSaveFileIfExists();

                yield return LoadMainScene();
                yield return SelectWarriorAndConfirm();

                var exploreController = FindController<ExplorePanelController>();
                yield return EnterGoblinBattle(exploreController);

                var enemyPortrait = exploreController.transform.Find("EnemyPortrait");
                var enemyImage = enemyPortrait.GetComponent<Image>();
                var enemyHitFlash = enemyPortrait.GetComponent<HitFlashEffect>();
                var enemyDamagePopup = enemyPortrait.GetComponentInChildren<DamagePopupText>(true);
                var popupRect = enemyDamagePopup.GetComponent<RectTransform>();

                Color originalColor = enemyImage.color;

                enemyHitFlash.Flash();
                Assert.IsTrue(enemyHitFlash.IsPlaying);
                Assert.AreEqual((Color)UIColors.HitFlash, enemyImage.color);

                // 코루틴(flashDuration=0.18초)이 끝나기 전에 다시 Flash()를 호출한다 — 이전
                // 이펙트가 중단되고 잔상 없이 새로 시작되어야 한다(원본 색이 오염되면 안 됨).
                yield return null;
                enemyHitFlash.Flash();
                Assert.IsTrue(enemyHitFlash.IsPlaying, "연속 Flash() 호출 후에도 코루틴이 계속 실행 중이어야 합니다.");
                Assert.AreEqual((Color)UIColors.HitFlash, enemyImage.color, "연속 Flash() 호출 직후에도 HitFlash 색이어야 합니다.");

                yield return new WaitForSeconds(0.3f);
                Assert.IsFalse(enemyHitFlash.IsPlaying, "충분한 시간이 지나면 코루틴이 끝나 있어야 합니다.");
                Assert.AreEqual(originalColor, enemyImage.color,
                    "연속 호출 이후에도 최종적으로는 최초 원본 색으로 정확히 복원되어야 합니다(잔상/오염 없음).");

                enemyDamagePopup.Show(10, isHeal: false);
                Assert.AreEqual("-10", enemyDamagePopup.CurrentText);
                Assert.IsTrue(enemyDamagePopup.gameObject.activeSelf);
                Vector2 startPosition = popupRect.anchoredPosition;

                // 애니메이션이 진행되도록 몇 프레임 흘려보낸 뒤(팝업이 위로 떠오르는 중), 완료되기
                // 전에 새 값으로 다시 Show()를 호출한다 — 이전 애니메이션 잔여 위치/텍스트가 아니라
                // 새 값·시작 위치로 즉시 리셋되어야 한다. Vector2를 엄격한 값 동등 비교(==)로 검증하면
                // RectTransform 내부 계산(anchoredPosition은 rect/anchor/pivot으로부터 매 프레임
                // 다시 계산되는 파생값)에서 생기는 부동소수점 오차(표시상 "(0.00, 40.00)"으로 동일해
                // 보여도 실제로는 미세하게 다른 값)로 false-positive 실패가 날 수 있어(2026-09-08
                // 실측: `Assert.AreEqual`이 "Expected: (0.00, 40.00) But was: (0.00, 40.00)"로 실패하는
                // 것을 재현·확인함), Vector2.Distance 기반 근사 비교로 바꿨다.
                yield return new WaitForSeconds(0.2f);
                Assert.Greater(Vector2.Distance(startPosition, popupRect.anchoredPosition), 0.01f,
                    "이 어서션이 유효하려면 리셋 전에 팝업이 이미 위로 떠오르는 중이어야 합니다.");

                enemyDamagePopup.Show(20, isHeal: true);
                Assert.AreEqual("+20", enemyDamagePopup.CurrentText, "새 Show() 호출은 이전 값이 아니라 새 값으로 즉시 갱신되어야 합니다.");
                Assert.LessOrEqual(Vector2.Distance(startPosition, popupRect.anchoredPosition), 0.01f,
                    "새 Show() 호출 시 위치가 시작 지점으로 리셋되어야 합니다(이전 애니메이션 잔여 위치가 남으면 안 됨).");

                yield return new WaitForSeconds(0.9f);
                Assert.IsFalse(enemyDamagePopup.IsPlaying);
                Assert.IsFalse(enemyDamagePopup.gameObject.activeSelf, "충분한 시간이 지나면 팝업이 다시 비활성화되어야 합니다.");
            }
            finally
            {
                RestoreSaveFile(hadExisting, backup);
                Application.logMessageReceived -= ConsumeKnownEditorNoiseIfMatched;
            }
        }

        // ----------------------------------------------------------------
        // 테스트 P: 전투 중 PlayerPortrait/EnemyPortrait에 DEC-137 가장자리 마스킹 셰이더/머티리얼이
        // 실제로 붙어있는지(사각형 이미지가 그대로 얹혀있던 DEC-116 공백이 실제로 메워졌는지) 검증.
        // ----------------------------------------------------------------
        [UnityTest]
        public IEnumerator P_BattlePortraits_HaveEdgeMaskMaterialApplied()
        {
            bool hadExisting = false;
            string backup = null;
            try
            {
                backup = BackupSaveFileIfExists(out hadExisting);
                DeleteSaveFileIfExists();

                yield return LoadMainScene();
                yield return SelectWarriorAndConfirm();

                var exploreController = FindController<ExplorePanelController>();
                yield return EnterGoblinBattle(exploreController);

                var enemyPortraitImage = exploreController.transform.Find("EnemyPortrait").GetComponent<Image>();
                var playerPortraitImage = exploreController.transform.Find("PlayerPortrait").GetComponent<Image>();

                Assert.IsNotNull(enemyPortraitImage.material, "EnemyPortrait에 머티리얼이 할당되어 있어야 합니다(DEC-137).");
                Assert.AreEqual("TextRPG/UI/PortraitEdgeMask", enemyPortraitImage.material.shader.name,
                    "EnemyPortrait 머티리얼이 가장자리 마스킹 셰이더를 써야 합니다.");
                var enemyMaskTex = enemyPortraitImage.material.GetTexture("_MaskTex");
                Assert.IsNotNull(enemyMaskTex, "EnemyPortrait 머티리얼의 _MaskTex가 비어있으면 안 됩니다.");
                Assert.AreEqual("mask_enemy_softedge", enemyMaskTex.name,
                    $"EnemyPortrait은 mask_enemy_softedge를 써야 하는데 {enemyMaskTex.name}이 할당되어 있습니다.");

                Assert.IsNotNull(playerPortraitImage.material, "PlayerPortrait에 머티리얼이 할당되어 있어야 합니다(DEC-137).");
                Assert.AreEqual("TextRPG/UI/PortraitEdgeMask", playerPortraitImage.material.shader.name,
                    "PlayerPortrait 머티리얼이 가장자리 마스킹 셰이더를 써야 합니다.");
                var playerMaskTex = playerPortraitImage.material.GetTexture("_MaskTex");
                Assert.IsNotNull(playerMaskTex, "PlayerPortrait 머티리얼의 _MaskTex가 비어있으면 안 됩니다.");
                Assert.AreEqual("mask_character_softedge", playerMaskTex.name,
                    $"PlayerPortrait은 mask_character_softedge를 써야 하는데 {playerMaskTex.name}이 할당되어 있습니다.");
            }
            finally
            {
                RestoreSaveFile(hadExisting, backup);
            }
        }

        // ----------------------------------------------------------------
        // 테스트 Q: 인벤토리(아이템 사용/장착)·상점(무기고 구매) 버튼에 아이템 아이콘이 실제로
        // 표시되는지(DEC-137), 그리고 아이콘이 없는 일반 버튼(선택지 이동)은 여전히 기존과 동일하게
        // 아이콘 없이 나오는지(회귀 방지) 검증.
        // ----------------------------------------------------------------
        [UnityTest]
        public IEnumerator Q_InventoryAndShopButtons_ShowItemIcons()
        {
            bool hadExisting = false;
            string backup = null;
            try
            {
                backup = BackupSaveFileIfExists(out hadExisting);
                DeleteSaveFileIfExists();

                yield return LoadMainScene();
                yield return SelectWarriorAndConfirm();

                var exploreController = FindController<ExplorePanelController>();
                var buttonRow = exploreController.transform.Find("ButtonRow");

                // 던전 입구: 시작 포션("회복 물약") 사용 버튼에 아이콘이 있어야 한다.
                var usePotionButton = FindButtonByLabelPrefix(buttonRow, "아이템 사용: 회복 물약");
                Assert.IsNotNull(usePotionButton, "'아이템 사용: 회복 물약' 버튼을 찾을 수 없습니다.");
                var potionIcon = usePotionButton.transform.Find("Icon").GetComponent<Image>();
                Assert.IsTrue(potionIcon.enabled, "회복 물약 사용 버튼의 아이콘이 활성화되어 있어야 합니다.");
                Assert.IsNotNull(potionIcon.sprite, "회복 물약 사용 버튼의 아이콘 스프라이트가 null이면 안 됩니다.");
                Assert.AreEqual("item_회복물약", potionIcon.sprite.name,
                    $"회복 물약 아이콘은 item_회복물약이어야 하는데 {potionIcon.sprite.name}이 할당되어 있습니다.");

                // 전사 시작 무기("장검") 장착 버튼("장착됨: 장검" — 이미 장착 상태로 시작)에도 아이콘이 있어야 한다.
                var equippedWeaponButton = FindButtonByLabelPrefix(buttonRow, "장착됨: 장검");
                Assert.IsNotNull(equippedWeaponButton, "'장착됨: 장검' 버튼을 찾을 수 없습니다.");
                var weaponIcon = equippedWeaponButton.transform.Find("Icon").GetComponent<Image>();
                Assert.IsTrue(weaponIcon.enabled, "장검 장착 버튼의 아이콘이 활성화되어 있어야 합니다.");
                Assert.AreEqual("item_전사장검", weaponIcon.sprite.name);

                // 아이콘이 없는 일반 선택지 버튼("1. 던전에 들어간다")은 여전히 아이콘이 비활성이어야 한다(회귀 방지).
                var moveButton = FindButtonByLabelPrefix(buttonRow, "1.");
                Assert.IsNotNull(moveButton);
                var moveIcon = moveButton.transform.Find("Icon").GetComponent<Image>();
                Assert.IsFalse(moveIcon.enabled, "아이템과 무관한 이동 버튼은 아이콘이 비활성 상태여야 합니다.");

                // 던전 입구 -> 갈림길 -> 낡은 무기고(지역 2)로 이동해 구매 버튼 아이콘을 확인한다.
                moveButton.onClick.Invoke();
                yield return null;
                Assert.AreEqual(1, FindBootstrap().Session.Map.GetCurrentLocationIndex(), "갈림길(지역 1)로 이동해야 합니다.");

                var toArmoryButton = FindButtonByLabelPrefix(buttonRow, "1.");
                Assert.IsNotNull(toArmoryButton, "갈림길의 '1. 왼쪽 빛을 따라간다' 버튼을 찾을 수 없습니다.");
                toArmoryButton.onClick.Invoke();
                yield return null;

                Assert.AreEqual(2, FindBootstrap().Session.Map.GetCurrentLocationIndex(), "낡은 무기고(지역 2)로 이동해야 합니다.");

                var buyGreatswordButton = FindButtonByLabelPrefix(buttonRow, "구매: 대검");
                Assert.IsNotNull(buyGreatswordButton, "전사 전용 신규 무기(대검) 구매 버튼을 찾을 수 없습니다.");
                var greatswordIcon = buyGreatswordButton.transform.Find("Icon").GetComponent<Image>();
                Assert.IsTrue(greatswordIcon.enabled, "대검 구매 버튼의 아이콘이 활성화되어 있어야 합니다.");
                Assert.AreEqual("item_전사대검", greatswordIcon.sprite.name);

                var buyLeatherArmorButton = FindButtonByLabelPrefix(buttonRow, "구매: 가죽 갑옷");
                Assert.IsNotNull(buyLeatherArmorButton, "가죽 갑옷 구매 버튼을 찾을 수 없습니다.");
                var leatherArmorIcon = buyLeatherArmorButton.transform.Find("Icon").GetComponent<Image>();
                Assert.IsTrue(leatherArmorIcon.enabled, "가죽 갑옷 구매 버튼의 아이콘이 활성화되어 있어야 합니다.");
                Assert.AreEqual("item_가죽갑옷", leatherArmorIcon.sprite.name);
            }
            finally
            {
                RestoreSaveFile(hadExisting, backup);
            }
        }

        // ----------------------------------------------------------------
        // 테스트 R: 큰 양피지 패널(타이틀/직업 카드 3장/결과지) 네 모서리에 필리그리 장식
        // Image가 실제로 배치돼 있는지(DEC-117/120 설계, DEC-138에서 실제 연결). 탐색/전투
        // fullbleed 화면은 대상이 아니므로(DEC-116) 여기서 검증하지 않는다.
        // ----------------------------------------------------------------
        [UnityTest]
        public IEnumerator R_LargeParchmentPanels_HaveFiligreeCornersApplied()
        {
            yield return LoadMainScene();

            var titleController = FindController<TitlePanelController>();
            var classSelectController = FindController<ClassSelectPanelController>();
            var resultController = FindController<ResultPanelController>();

            string[] cornerNames =
            {
                "ui_filigree_top_left", "ui_filigree_top_right",
                "ui_filigree_bottom_left", "ui_filigree_bottom_right",
            };

            AssertFiligreeCornersPresent(titleController.transform.Find("ParchmentCard"), "타이틀 패널 카드", cornerNames);
            AssertFiligreeCornersPresent(resultController.transform.Find("ParchmentCard"), "결과 패널 카드", cornerNames);

            int checkedCards = 0;
            foreach (var id in new[] { "warrior", "rogue", "mage" })
            {
                var card = classSelectController.transform.Find($"Card_{id}");
                Assert.IsNotNull(card, $"'{id}' 직업 카드를 찾을 수 없습니다.");
                AssertFiligreeCornersPresent(card, $"'{id}' 직업 카드", cornerNames);
                checkedCards++;
            }
            Assert.AreEqual(3, checkedCards, "직업 카드 3개(전사/도적/마법사) 전부 확인했어야 합니다.");
        }

        private static void AssertFiligreeCornersPresent(Transform panelRoot, string label, string[] cornerNames)
        {
            Assert.IsNotNull(panelRoot, $"{label}을(를) 찾을 수 없습니다.");
            foreach (var name in cornerNames)
            {
                var cornerT = panelRoot.Find(name);
                Assert.IsNotNull(cornerT, $"{label}에 필리그리 모서리 '{name}'가 없습니다.");
                var img = cornerT.GetComponent<Image>();
                Assert.IsNotNull(img, $"{label}의 '{name}'에 Image 컴포넌트가 없습니다.");
                Assert.IsNotNull(img.sprite, $"{label}의 '{name}' 이미지 스프라이트가 null입니다.");
            }
        }

        // ----------------------------------------------------------------
        // 테스트 S: 결과 화면 깃펜(QuillRevealText) 펜 아이콘이 실제 깃펜 이미지(art-assets
        // vfx_quill_*)를 쓰고, 진행률에 따라 다른 프레임 스프라이트로 바뀌는지(DEC-138) 검증.
        // 이동 로직(테스트 H)과 별개로 이번엔 "그림이 실제로 바뀌는지"만 확인한다.
        // ----------------------------------------------------------------
        [UnityTest]
        public IEnumerator S_ResultPanel_QuillReveal_PenSpriteChangesAcrossProgress()
        {
            Application.logMessageReceived += ConsumeKnownEditorNoiseIfMatched;
            try
            {
                yield return LoadMainScene();

                var resultController = FindController<ResultPanelController>();
                var quill = resultController.GetComponentInChildren<QuillRevealText>(true);
                Assert.IsNotNull(quill, "ResultPanel의 서술 텍스트에 QuillRevealText가 연결되어 있어야 합니다.");

                resultController.gameObject.SetActive(true);

                var penTransform = quill.transform.Find("QuillPen");
                Assert.IsNotNull(penTransform, "QuillPen 오브젝트를 찾을 수 없습니다.");
                var penImage = penTransform.GetComponent<Image>();
                Assert.IsNotNull(penImage, "펜 아이콘에 Image 컴포넌트가 있어야 합니다.");
                Assert.IsNotNull(penImage.sprite,
                    "펜 아이콘 스프라이트가 null이면 안 됩니다(DEC-138: 실제 깃펜 이미지 연결).");

                quill.Play("깃펜 프레임 전환 테스트용 문장입니다.");
                var spriteAtStart = penImage.sprite;
                Assert.AreEqual("vfx_quill_idle", spriteAtStart.name,
                    $"재생 시작 직후(progress<0.2) 펜 스프라이트는 vfx_quill_idle이어야 하는데 {spriteAtStart.name}입니다.");

                yield return new WaitForSeconds(1.7f);
                Assert.AreEqual(1f, quill.Progress, 0.001f, "충분한 시간이 지나면 진행률이 1(완료)이 되어야 합니다.");
                var spriteAtEnd = penImage.sprite;

                Assert.AreNotEqual(spriteAtStart, spriteAtEnd,
                    "진행률 0 부근과 1.0(완료) 사이에 펜 스프라이트가 실제로 바뀌어야 합니다(DEC-138: idle→writing→end 프레임 전환).");
                Assert.AreEqual("vfx_quill_writing_end", spriteAtEnd.name,
                    $"완료 시점 펜 스프라이트는 vfx_quill_writing_end여야 하는데 {spriteAtEnd.name}입니다.");
            }
            finally
            {
                Application.logMessageReceived -= ConsumeKnownEditorNoiseIfMatched;
            }
        }

        // ----------------------------------------------------------------
        // 테스트 T: 던전 보드(DEC-139, OQ-110 해결) — 게임 시작 시 토큰이 첫 번째 노드(던전 입구)
        // 위에 있고 모든 미래 노드가 locked 상태인지, 지역을 실제로 이동했을 때(ChooseLocationAction을
        // 거치는 실제 버튼 클릭) 토큰이 두 번째 노드(갈림길)로 옮겨가고 첫 번째 노드가 cleared로
        // 바뀌며 그 사이 연결선(Route_0)만 완료 색으로 바뀌는지 검증.
        // ----------------------------------------------------------------
        [UnityTest]
        public IEnumerator T_DungeonBoard_TokenAndOverlaysReflectCurrentLocation()
        {
            bool hadExisting = false;
            string backup = null;
            try
            {
                backup = BackupSaveFileIfExists(out hadExisting);
                DeleteSaveFileIfExists();

                yield return LoadMainScene();
                yield return SelectWarriorAndConfirm();

                var bootstrap = FindBootstrap();
                var exploreController = FindController<ExplorePanelController>();
                var board = exploreController.transform.Find("DungeonBoard");
                Assert.IsNotNull(board, "탐색 화면에 DungeonBoard가 배치되어 있어야 합니다(DEC-139).");

                string[] nodeNames =
                {
                    "Node_0_node_entrance", "Node_1_node_fork", "Node_2_node_armory",
                    "Node_3_node_corridor", "Node_4_node_boss",
                };
                var nodeTransforms = new Transform[nodeNames.Length];
                for (int i = 0; i < nodeNames.Length; i++)
                {
                    nodeTransforms[i] = board.Find(nodeNames[i]);
                    Assert.IsNotNull(nodeTransforms[i], $"보드 노드 '{nodeNames[i]}'를 찾을 수 없습니다.");
                }

                var token = board.Find("CurrentLocationToken").GetComponent<RectTransform>();
                Assert.IsNotNull(token, "CurrentLocationToken을 찾을 수 없습니다.");

                Assert.AreEqual(0, bootstrap.Session.Map.GetCurrentLocationIndex());
                var node0RT = nodeTransforms[0].GetComponent<RectTransform>();
                Assert.AreEqual(node0RT.anchoredPosition.x, token.anchoredPosition.x, 0.01f,
                    "게임 시작 시 토큰은 첫 번째 노드(던전 입구) 위에 있어야 합니다.");

                for (int i = 0; i < nodeTransforms.Length; i++)
                {
                    bool cleared = nodeTransforms[i].Find("ClearedOverlay").gameObject.activeSelf;
                    bool locked = nodeTransforms[i].Find("LockedOverlay").gameObject.activeSelf;
                    Assert.IsFalse(cleared, $"노드 {i}는 아직 지나온 지역이 아니므로 cleared 오버레이가 꺼져 있어야 합니다.");
                    Assert.AreEqual(i > 0, locked,
                        $"노드 {i}의 locked 상태가 기대와 다릅니다(현재 위치 0 기준).");
                }

                // 던전 입구(0) -> "1. 던전에 들어간다" 버튼을 실제로 클릭해 갈림길(1)로 이동한다.
                var buttonRow = exploreController.transform.Find("ButtonRow");
                var moveButton = FindButtonByLabelPrefix(buttonRow, "1.");
                Assert.IsNotNull(moveButton, "던전 입구의 '1. 던전에 들어간다' 버튼을 찾을 수 없습니다.");
                moveButton.onClick.Invoke();
                yield return null;

                Assert.AreEqual(1, bootstrap.Session.Map.GetCurrentLocationIndex());

                var node1RT = nodeTransforms[1].GetComponent<RectTransform>();
                Assert.AreEqual(node1RT.anchoredPosition.x, token.anchoredPosition.x, 0.01f,
                    "이동 후 토큰은 두 번째 노드(갈림길) 위로 옮겨가야 합니다.");

                Assert.IsTrue(nodeTransforms[0].Find("ClearedOverlay").gameObject.activeSelf,
                    "이동 후 첫 번째 노드(던전 입구)는 cleared 상태로 바뀌어야 합니다.");
                Assert.IsFalse(nodeTransforms[0].Find("LockedOverlay").gameObject.activeSelf);
                Assert.IsFalse(nodeTransforms[1].Find("ClearedOverlay").gameObject.activeSelf,
                    "현재 위치 노드(갈림길)는 cleared 오버레이가 켜지면 안 됩니다.");
                Assert.IsFalse(nodeTransforms[1].Find("LockedOverlay").gameObject.activeSelf,
                    "현재 위치 노드(갈림길)는 locked 오버레이도 꺼져 있어야 합니다.");
                for (int i = 2; i < nodeTransforms.Length; i++)
                {
                    Assert.IsTrue(nodeTransforms[i].Find("LockedOverlay").gameObject.activeSelf,
                        $"노드 {i}는 여전히 locked 상태여야 합니다.");
                }

                var route0 = board.Find("Route_0").GetComponent<Image>();
                Assert.AreEqual((Color)UIColors.Primary, route0.color,
                    "이미 지나온 구간(Route_0)은 완료 색(UIColors.Primary)으로 바뀌어야 합니다.");
                var route1 = board.Find("Route_1").GetComponent<Image>();
                Assert.AreEqual((Color)UIColors.OutlineVariant, route1.color,
                    "아직 안 지나온 구간(Route_1)은 기본(OutlineVariant) 색 그대로여야 합니다.");
            }
            finally
            {
                RestoreSaveFile(hadExisting, backup);
            }
        }
    }
}
