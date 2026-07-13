using CandyCrush.Data;
using CandyCrush.View;
using CandyCrush.Vfx;
using UnityEngine;

namespace CandyCrush.Game
{
    /// <summary>关卡导演：加载配置、初始化棋盘/目标、启动氛围特效。</summary>
    public class LevelDirector : MonoBehaviour
    {
        [SerializeField] LevelConfig levelConfig;
        [SerializeField] TileSpriteCatalog catalog;
        [SerializeField] BoardView boardView;
        [SerializeField] GoalHUD goalHud;
        [SerializeField] WinPanel winPanel;
        [SerializeField] Transform atmosphereRoot;
        [SerializeField] SpriteRenderer background;

        AtmosphereFx _atmosphere;
        int _remaining;

        public int Remaining => _remaining;
        public BoardView Board => boardView;

        void Awake()
        {
            EnsurePortraitSetup();
        }

        void Start() => Boot();

        void EnsurePortraitSetup()
        {
            var setup = FindObjectOfType<PortraitSetup>();
            if (setup == null)
                setup = gameObject.AddComponent<PortraitSetup>();
            setup.Bind(Camera.main, background);
        }

        public void Boot()
        {
            if (levelConfig == null || catalog == null || boardView == null)
            {
                Debug.LogError("[LevelDirector] Missing references.");
                return;
            }

            boardView.Initialize(levelConfig, catalog);
            _remaining = CountSuitcases(boardView.Model);
            if (levelConfig.objectiveCount > 0)
                _remaining = Mathf.Min(_remaining, levelConfig.objectiveCount);

            if (goalHud != null)
            {
                goalHud.SetIcon(catalog.GetSprite(TileType.Suitcase));
                goalHud.SetRemaining(_remaining);
            }

            if (winPanel != null) winPanel.Hide();

            if (atmosphereRoot == null) atmosphereRoot = transform;
            _atmosphere = AtmosphereFx.CreateDefault(atmosphereRoot);
        }

        public void NotifySuitcaseCollected(int amount = 1)
        {
            _remaining = Mathf.Max(0, _remaining - amount);
            if (goalHud != null) goalHud.SetRemaining(_remaining);
            if (_remaining <= 0)
                OnWin();
        }

        void OnWin()
        {
            if (winPanel != null) winPanel.Show();
        }

        static int CountSuitcases(BoardModel model)
        {
            int n = 0;
            for (int r = 0; r < model.Rows; r++)
            for (int c = 0; c < model.Cols; c++)
                if (model.Get(r, c) == TileType.Suitcase) n++;
            return n;
        }
    }
}
