using CandyCrush.Data;
using CandyCrush.View;
using CandyCrush.Vfx;
using UnityEngine;

namespace CandyCrush.Game
{
    /// <summary>关卡导演：加载配置、初始化棋盘/目标/流程、启动氛围特效。</summary>
    public class LevelDirector : MonoBehaviour
    {
        [SerializeField] LevelConfig levelConfig;
        [SerializeField] TileSpriteCatalog catalog;
        [SerializeField] BoardView boardView;
        [SerializeField] GoalHUD goalHud;
        [SerializeField] WinPanel winPanel;
        [SerializeField] Transform atmosphereRoot;
        [SerializeField] SpriteRenderer background;
        [SerializeField] GameFlowController flow;
        [SerializeField] InputController input;
        [SerializeField] float portraitOrthoSize = 8.2f;

        AtmosphereFx _atmosphere;

        public BoardView Board => boardView;
        public GameFlowController Flow => flow;

        void Awake()
        {
            ApplyPortrait();
            if (flow == null) flow = GetComponent<GameFlowController>() ?? gameObject.AddComponent<GameFlowController>();
            if (input == null) input = FindObjectOfType<InputController>();
        }

        void Start() => Boot();

        void LateUpdate() => FitBackground();

        void ApplyPortrait()
        {
            Screen.orientation = ScreenOrientation.Portrait;
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;

            var cam = Camera.main;
            if (cam == null) return;
            cam.orthographic = true;
            cam.orthographicSize = portraitOrthoSize;
            FitBackground();
        }

        void FitBackground()
        {
            var cam = Camera.main;
            if (background == null || background.sprite == null || cam == null) return;
            float worldH = cam.orthographicSize * 2f;
            float worldW = worldH * cam.aspect;
            var size = background.sprite.bounds.size;
            float scale = Mathf.Max(worldW / size.x, worldH / size.y);
            background.transform.localScale = new Vector3(scale, scale, 1f);
            background.transform.position = new Vector3(
                cam.transform.position.x,
                cam.transform.position.y,
                background.transform.position.z);
        }

        public void Boot()
        {
            if (levelConfig == null || catalog == null || boardView == null)
            {
                Debug.LogError("[LevelDirector] Missing references.");
                return;
            }

            boardView.Initialize(levelConfig, catalog);

            int suitcases = CountSuitcases(boardView.Model);
            if (goalHud != null)
            {
                goalHud.SetIcon(catalog.GetSprite(TileType.Suitcase));
                goalHud.SetRemaining(Mathf.Min(levelConfig.objectiveCount, suitcases));
            }

            if (winPanel != null) winPanel.Hide();

            if (atmosphereRoot == null) atmosphereRoot = transform;
            _atmosphere = AtmosphereFx.CreateDefault(atmosphereRoot);

            flow.Bind(boardView, goalHud, winPanel);
            if (input != null) input.Bind(boardView, flow);

            flow.BeginLevel(levelConfig, suitcases);
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
