using CandyCrush.Common;
using CandyCrush.Data;
using CandyCrush.View;
using CandyCrush.Vfx;
using UnityEngine;

namespace CandyCrush.Game
{
    /// <summary>关卡导演：加载配置、动态加载目标/胜利 UI 预制体、初始化棋盘与流程。</summary>
    public class LevelDirector : MonoBehaviour
    {
        [SerializeField] LevelConfig levelConfig;
        [SerializeField] TileSpriteCatalog catalog;
        [SerializeField] BoardView boardView;
        [Header("UI Prefabs (Resources fallback if empty)")]
        [SerializeField] GoalHUD goalHudPrefab;
        [SerializeField] WinPanel winPanelPrefab;
        [SerializeField] Transform uiRoot;
        [SerializeField] Transform atmosphereRoot;
        [SerializeField] SpriteRenderer background;
        [SerializeField] GameFlowController flow;
        [SerializeField] InputController input;
        [SerializeField] float portraitOrthoSize = 8.2f;

        GoalHUD _goalHud;
        WinPanel _winPanel;
        Camera _cam;
        int _fitScreenW = -1;
        int _fitScreenH = -1;
        float _fitAspect = -1f;

        void Awake()
        {
            ApplyPortrait();
            if (flow == null) flow = GetComponent<GameFlowController>() ?? gameObject.AddComponent<GameFlowController>();
            if (input == null) input = FindObjectOfType<InputController>();
        }

        void Start() => Boot();

        void LateUpdate()
        {
            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return;
            if (Screen.width == _fitScreenW && Screen.height == _fitScreenH &&
                Mathf.Approximately(_cam.aspect, _fitAspect))
                return;
            FitBackground();
        }

        void ApplyPortrait()
        {
            Screen.orientation = ScreenOrientation.Portrait;
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;

            _cam = Camera.main;
            if (_cam == null) return;
            _cam.orthographic = true;
            _cam.orthographicSize = portraitOrthoSize;
            // 无夜景贴图：用相机清屏色作深色底
            _cam.backgroundColor = new Color(0.04f, 0.06f, 0.10f, 1f);
            FitBackground();
        }

        void FitBackground()
        {
            if (_cam == null) _cam = Camera.main;
            if (background == null || background.sprite == null || _cam == null) return;

            _fitScreenW = Screen.width;
            _fitScreenH = Screen.height;
            _fitAspect = _cam.aspect;

            float worldH = _cam.orthographicSize * 2f;
            float worldW = worldH * _cam.aspect;
            var size = background.sprite.bounds.size;
            float scale = Mathf.Max(worldW / size.x, worldH / size.y);
            background.transform.localScale = new Vector3(scale, scale, 1f);
            background.transform.position = new Vector3(
                _cam.transform.position.x,
                _cam.transform.position.y,
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
            SpawnUi();

            int suitcases = CountSuitcases(boardView.Model);
            if (_goalHud != null)
                _goalHud.SetIcon(catalog.GetSprite(TileType.Suitcase));

            EventBus.Publish(new ObjectiveChangedEvent(Mathf.Min(levelConfig.objectiveCount, suitcases)));
            if (_winPanel != null)
                _winPanel.Hide();

            if (atmosphereRoot == null) atmosphereRoot = transform;
            AtmosphereFx.CreateDefault(atmosphereRoot);

            flow.Bind(boardView, _goalHud, _winPanel);
            if (input != null) input.Bind(boardView, flow);

            flow.BeginLevel(levelConfig, suitcases);
        }

        void SpawnUi()
        {
            ClearBakedUi();
            GameUiFactory.EnsureEventSystem();
            var canvas = GameUiFactory.EnsureOverlayCanvas();
            var parent = uiRoot != null ? uiRoot : canvas.transform;

            _goalHud = SpawnGoalHud(parent);
            _winPanel = SpawnWinPanel(parent);
        }

        /// <summary>清掉场景里旧的烘焙 Goal/Win，避免与动态实例重复。</summary>
        void ClearBakedUi()
        {
            foreach (var hud in FindObjectsOfType<GoalHUD>())
            {
                if (hud == null) continue;
                hud.gameObject.SetActive(false); // 立刻退订 EventBus
                Destroy(hud.gameObject);
            }
            foreach (var win in FindObjectsOfType<WinPanel>())
            {
                if (win == null) continue;
                win.gameObject.SetActive(false);
                Destroy(win.gameObject);
            }
        }

        GoalHUD SpawnGoalHud(Transform parent)
        {
            var prefab = goalHudPrefab;
            if (prefab == null)
                prefab = Resources.Load<GoalHUD>(GameUiFactory.GoalHudResourcePath);

            if (prefab != null)
                return Instantiate(prefab, parent);

            Debug.LogWarning("[LevelDirector] GoalHUD prefab missing, building at runtime. Run CandyCrush/Rebuild UI Prefabs.");
            return GameUiFactory.CreateGoalHud(
                parent,
                null,
                catalog != null ? catalog.GetSprite(TileType.Suitcase) : null,
                null);
        }

        WinPanel SpawnWinPanel(Transform parent)
        {
            var prefab = winPanelPrefab;
            if (prefab == null)
                prefab = Resources.Load<WinPanel>(GameUiFactory.WinPanelResourcePath);

            if (prefab != null)
            {
                var instance = Instantiate(prefab, parent);
                EnsureSettleFx(instance);
                return instance;
            }

            Debug.LogWarning("[LevelDirector] WinPanel prefab missing, building at runtime. Run CandyCrush/Rebuild UI Prefabs.");
            return GameUiFactory.CreateWinPanel(
                parent,
                catalog != null ? catalog.GetSprite(TileType.Suitcase) : null,
                null);
        }

        static void EnsureSettleFx(WinPanel win)
        {
            if (win == null) return;
            var settle = win.GetComponent<SettleFx>();
            if (settle == null) settle = win.gameObject.AddComponent<SettleFx>();
            var great = win.transform.Find("Visual/GreatText") as RectTransform
                        ?? win.transform.Find("GreatText") as RectTransform;
            if (great != null) settle.BindGreat(great);
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
