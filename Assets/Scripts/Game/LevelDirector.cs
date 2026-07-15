using CandyCrush.Common;
using CandyCrush.Data;
using CandyCrush.View;
using CandyCrush.Vfx;
using UnityEngine;

namespace CandyCrush.Game
{
    /// <summary>关卡导演：加载配置、竖屏分辨率适配、动态 UI、初始化棋盘与流程。</summary>
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
        [Tooltip("棋盘适配失败时的兜底 ortho")]
        [SerializeField] float portraitOrthoSize = 8.2f;
        [Tooltip("棋盘相对可视区左右留白比例")]
        [SerializeField] [Range(0.02f, 0.12f)] float boardSidePad = 0.045f;
        [Tooltip("棋盘上方额外留给目标 HUD 的比例（叠加 SafeArea）")]
        [SerializeField] [Range(0.05f, 0.25f)] float boardTopHudPad = 0.12f;
        [Tooltip("棋盘下方留白比例（叠加 SafeArea）")]
        [SerializeField] [Range(0.01f, 0.12f)] float boardBottomPad = 0.04f;

        GoalHUD _goalHud;
        WinPanel _winPanel;
        Camera _cam;
        int _fitScreenW = -1;
        int _fitScreenH = -1;
        float _fitAspect = -1f;
        Rect _fitSafeArea;
        Vector3 _camRestPos;
        bool _camRestCached;

        void Awake()
        {
            ApplyPortraitLock();
            if (flow == null) flow = GetComponent<GameFlowController>() ?? gameObject.AddComponent<GameFlowController>();
            if (input == null) input = FindObjectOfType<InputController>();
        }

        void Start() => Boot();

        void LateUpdate()
        {
            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return;
            if (Screen.width == _fitScreenW && Screen.height == _fitScreenH &&
                Mathf.Approximately(_cam.aspect, _fitAspect) &&
                Screen.safeArea == _fitSafeArea)
                return;
            FitPortraitLayout();
        }

        void ApplyPortraitLock()
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
            _cam.backgroundColor = new Color(0.04f, 0.06f, 0.10f, 1f);
            if (!_camRestCached)
            {
                _camRestPos = _cam.transform.position;
                _camRestCached = true;
            }
        }

        /// <summary>竖屏：相机框住棋盘 + 背景铺满 + 记录分辨率。</summary>
        void FitPortraitLayout()
        {
            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return;

            _fitScreenW = Screen.width;
            _fitScreenH = Screen.height;
            _fitAspect = _cam.aspect;
            _fitSafeArea = Screen.safeArea;

            FitBoardCamera();
            FitBackground();
        }

        void FitBoardCamera()
        {
            if (_cam == null || boardView == null || boardView.Model == null)
            {
                if (_cam != null) _cam.orthographicSize = portraitOrthoSize;
                return;
            }

            if (!_camRestCached)
            {
                _camRestPos = _cam.transform.position;
                _camRestCached = true;
            }

            var bounds = boardView.GetBoardWorldBounds();
            float boardW = Mathf.Max(0.5f, bounds.size.x);
            float boardH = Mathf.Max(0.5f, bounds.size.y);

            float screenH = Mathf.Max(1f, Screen.height);
            var sa = Screen.safeArea;
            float safeTopN = Mathf.Clamp01((screenH - sa.yMax) / screenH);
            float safeBotN = Mathf.Clamp01(sa.yMin / screenH);

            float topPad = Mathf.Clamp(safeTopN + boardTopHudPad, 0.12f, 0.28f);
            float botPad = Mathf.Clamp(safeBotN + boardBottomPad, 0.03f, 0.16f);
            float sidePad = Mathf.Clamp(boardSidePad, 0.02f, 0.12f);
            float usableV = Mathf.Max(0.45f, 1f - topPad - botPad);
            float usableH = Mathf.Max(0.7f, 1f - sidePad * 2f);

            float aspect = Mathf.Max(0.01f, _cam.aspect);
            float orthoFromW = (boardW / usableH) * 0.5f / aspect;
            float orthoFromH = (boardH / usableV) * 0.5f;
            float ortho = Mathf.Max(orthoFromW, orthoFromH);
            ortho = Mathf.Clamp(ortho, 5.2f, 11.5f);
            if (ortho < 0.1f) ortho = portraitOrthoSize;
            _cam.orthographicSize = ortho;

            // 相机略上移，把棋盘放到去掉顶栏后的可视中心
            float worldH = ortho * 2f;
            float midShift = (botPad - topPad) * 0.5f * worldH;
            var boardCenter = bounds.center;
            _cam.transform.position = new Vector3(
                boardCenter.x,
                boardCenter.y - midShift,
                _camRestPos.z);
        }

        void FitBackground()
        {
            if (_cam == null) _cam = Camera.main;
            if (background == null || background.sprite == null || _cam == null) return;

            float worldH = _cam.orthographicSize * 2f;
            float worldW = worldH * _cam.aspect;
            var size = background.sprite.bounds.size;
            if (size.x < 0.0001f || size.y < 0.0001f) return;
            float scale = Mathf.Max(worldW / size.x, worldH / size.y) * 1.02f;
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

            ApplyPortraitLock();
            boardView.Initialize(levelConfig, catalog);
            SpawnUi();
            FitPortraitLayout();

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
            var safe = GameUiFactory.EnsureSafeArea(canvas);
            // uiRoot 若是 Canvas 外节点则仍用；否则挂 SafeArea
            Transform parent = safe != null ? safe : canvas.transform;
            if (uiRoot != null && uiRoot != canvas.transform && uiRoot.GetComponentInParent<Canvas>() == null)
                parent = uiRoot;

            _goalHud = SpawnGoalHud(parent);
            _winPanel = SpawnWinPanel(parent);
        }

        void ClearBakedUi()
        {
            foreach (var hud in FindObjectsOfType<GoalHUD>())
            {
                if (hud == null) continue;
                hud.gameObject.SetActive(false);
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
