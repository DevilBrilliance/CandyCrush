using CandyCrush.Common;
using CandyCrush.Data;
using UnityEngine;

namespace CandyCrush.View
{
    /// <summary>棋盘表现层：根据 BoardModel 镜像生成 TileView。</summary>
    public class BoardView : MonoBehaviour
    {
        [SerializeField] LevelConfig levelConfig;
        [SerializeField] TileSpriteCatalog catalog;
        [SerializeField] Transform tileRoot;
        [SerializeField] SpriteRenderer boardBg;
        [SerializeField] float cellSize = 0.95f;
        [SerializeField] Vector2 boardOrigin;

        BoardModel _model;
        TileView[,] _views;

        public BoardModel Model => _model;
        public float CellSize => cellSize;
        public int Rows => _model?.Rows ?? levelConfig.rows;
        public int Cols => _model?.Cols ?? levelConfig.cols;

        public void Initialize(LevelConfig config, TileSpriteCatalog sprites)
        {
            levelConfig = config;
            catalog = sprites;
            _model = new BoardModel(config.rows, config.cols);
            var layout = config.BuildInitialLayout();
            bool empty = true;
            for (int r = 0; r < config.rows && empty; r++)
            for (int c = 0; c < config.cols && empty; c++)
                if (layout[r, c] != TileType.Empty) empty = false;

            if (empty)
                layout = DemoLayouts.BuildVideoStyleBoard(config.rows, config.cols);

            _model.Fill(layout);
            RebuildViews();
            RefreshBoardBg();
        }

        public void RebuildViews()
        {
            if (tileRoot == null) tileRoot = transform;
            ClearChildren(tileRoot);
            _views = new TileView[_model.Rows, _model.Cols];

            for (int r = 0; r < _model.Rows; r++)
            for (int c = 0; c < _model.Cols; c++)
            {
                var type = _model.Get(r, c);
                if (type == TileType.Empty) continue;
                var sprite = catalog.GetSprite(type);
                if (sprite == null) continue;

                var go = new GameObject($"Tile_{r}_{c}");
                go.transform.SetParent(tileRoot, false);
                go.transform.localPosition = GridUtil.CellToLocal(r, c, _model.Rows, _model.Cols, cellSize, boardOrigin);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                var view = go.AddComponent<TileView>();
                view.Setup(type, sprite, r, c, cellSize);
                _views[r, c] = view;
            }
        }

        void RefreshBoardBg()
        {
            if (boardBg == null) return;
            if (catalog.boardCellSprite != null)
                boardBg.sprite = catalog.boardCellSprite;

            float w = _model.Cols * cellSize + 0.35f;
            float h = _model.Rows * cellSize + 0.35f;
            if (boardBg.sprite != null)
            {
                var size = boardBg.sprite.bounds.size;
                boardBg.transform.localScale = new Vector3(w / size.x, h / size.y, 1f);
            }
            boardBg.color = new Color(0.35f, 0.55f, 0.85f, 0.45f);
            boardBg.sortingOrder = 0;
        }

        public bool TryGetCell(Vector3 worldPos, out int row, out int col)
        {
            var local = transform.InverseTransformPoint(worldPos);
            return GridUtil.TryWorldToCell(local, _model.Rows, _model.Cols, cellSize, boardOrigin, out row, out col);
        }

        public TileView GetView(int row, int col) =>
            _model != null && _model.InBounds(row, col) ? _views[row, col] : null;

        static void ClearChildren(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i);
                if (Application.isPlaying) Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);
            }
        }
    }

    /// <summary>参考视频风格的初始布局：上半区四色混排，下半区行李箱。</summary>
    public static class DemoLayouts
    {
        public static TileType[,] BuildVideoStyleBoard(int rows, int cols)
        {
            var layout = new TileType[rows, cols];
            var normals = new[] { TileType.Red, TileType.Yellow, TileType.Blue, TileType.Green };
            int suitcaseStartRow = Mathf.Max(1, rows / 2);

            // 确定性伪随机，避免开局三连（取正数再取模，避免溢出产生负下标）
            int seed = 20260525;
            for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
            {
                if (r >= suitcaseStartRow)
                {
                    layout[r, c] = TileType.Suitcase;
                    continue;
                }

                seed = Next(seed);
                TileType pick = normals[PositiveMod(seed, normals.Length)];
                // 避免横向三连
                if (c >= 2 && layout[r, c - 1] == pick && layout[r, c - 2] == pick)
                    pick = normals[(ArrayIndex(normals, pick) + 1) % normals.Length];
                // 避免纵向三连
                if (r >= 2 && layout[r - 1, c] == pick && layout[r - 2, c] == pick)
                    pick = normals[(ArrayIndex(normals, pick) + 1) % normals.Length];
                layout[r, c] = pick;
            }

            // 在分界行混入若干箱子（贴近视频）
            if (suitcaseStartRow > 0)
            {
                int mid = suitcaseStartRow - 1;
                for (int c = 0; c < cols; c++)
                {
                    if (c % 2 == 0) layout[mid, c] = TileType.Suitcase;
                }
            }

            return layout;
        }

        static int Next(int s) => unchecked(s * 1103515245 + 12345);

        static int PositiveMod(int value, int modulo)
        {
            int m = value % modulo;
            return m < 0 ? m + modulo : m;
        }

        static int ArrayIndex(TileType[] arr, TileType v)
        {
            for (int i = 0; i < arr.Length; i++)
                if (arr[i] == v) return i;
            return 0;
        }
    }
}
