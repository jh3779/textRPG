/*
 * GameBootstrap.cs
 *
 * 📝 역할: docs/design-system/unity-mapping.html M-03(화면 → 씬/패널) 구현.
 * 단일 씬 + Canvas 하위 패널 전환 방식(정본 권고). GameSession(순수 로직)을 하나 들고
 * 있다가 각 Panel Controller에 넘겨준다 — View(패널)들은 서로 직접 참조하지 않고
 * 이 클래스를 통해서만 화면을 전환한다.
 */

using TextRPG.GameLogic;
using TextRPG.Persistence;
using UnityEngine;

namespace TextRPG.UI
{
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private TitlePanelController titlePanel;
        [SerializeField] private ClassSelectPanelController classSelectPanel;
        [SerializeField] private ExplorePanelController explorePanel;
        [SerializeField] private ResultPanelController resultPanel;

        public GameSession Session { get; private set; }

        private void Awake()
        {
            Session = new GameSession();
        }

        private void Start()
        {
            ShowTitle();
        }

        public void ShowTitle()
        {
            SetActivePanel(titlePanel.gameObject);
            titlePanel.Refresh(SaveSystem.SaveExists());
        }

        public void ShowClassSelect()
        {
            SetActivePanel(classSelectPanel.gameObject);
            classSelectPanel.Refresh();
        }

        public void ShowExplore()
        {
            SetActivePanel(explorePanel.gameObject);
            explorePanel.Refresh();
        }

        public void ShowResult()
        {
            SetActivePanel(resultPanel.gameObject);
            resultPanel.Refresh();
        }

        private void SetActivePanel(GameObject active)
        {
            titlePanel.gameObject.SetActive(active == titlePanel.gameObject);
            classSelectPanel.gameObject.SetActive(active == classSelectPanel.gameObject);
            explorePanel.gameObject.SetActive(active == explorePanel.gameObject);
            resultPanel.gameObject.SetActive(active == resultPanel.gameObject);
        }
    }
}
