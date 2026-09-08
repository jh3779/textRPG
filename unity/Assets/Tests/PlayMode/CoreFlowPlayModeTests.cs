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
 * 탐색→전투)과 대표적인 버튼 클릭 1개 이상씩만 확인한다(테스트 A~F).
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
    }
}
