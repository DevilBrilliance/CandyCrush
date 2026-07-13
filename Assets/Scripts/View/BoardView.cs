using System;
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

    /// <summary>参考视频/图一的初始布局：上 4 行四色，第 5 行两侧箱+中间三色，底 3 行全箱（共 33 箱）。</summary>
    public static class DemoLayouts
    {
        // R=红帽 Y=黄铃 B=蓝枕 G=绿叶 S=行李箱 — 严格对齐参考截图
        static readonly TileType[,] VideoBoard8x9 =
        {
            // row 0: 叶 枕 帽 帽 铃 叶 铃 枕 帽
            { TileType.Green, TileType.Blue, TileType.Red, TileType.Red, TileType.Yellow, TileType.Green, TileType.Yellow, TileType.Blue, TileType.Red },
            // row 1: 叶 铃 帽 铃 枕 铃 铃 帽 枕
            { TileType.Green, TileType.Yellow, TileType.Red, TileType.Yellow, TileType.Blue, TileType.Yellow, TileType.Yellow, TileType.Red, TileType.Blue },
            // row 2: 帽 枕 铃 叶 帽 叶 叶 铃 叶
            { TileType.Red, TileType.Blue, TileType.Yellow, TileType.Green, TileType.Red, TileType.Green, TileType.Green, TileType.Yellow, TileType.Green },
            // row 3: 枕 叶 叶 枕 叶 枕 铃 帽 铃
            { TileType.Blue, TileType.Green, TileType.Green, TileType.Blue, TileType.Green, TileType.Blue, TileType.Yellow, TileType.Red, TileType.Yellow },
            // row 4: 箱箱箱 叶铃叶 箱箱箱
            { TileType.Suitcase, TileType.Suitcase, TileType.Suitcase, TileType.Green, TileType.Yellow, TileType.Green, TileType.Suitcase, TileType.Suitcase, TileType.Suitcase },
            // row 5-7: 全箱
            { TileType.Suitcase, TileType.Suitcase, TileType.Suitcase, TileType.Suitcase, TileType.Suitcase, TileType.Suitcase, TileType.Suitcase, TileType.Suitcase, TileType.Suitcase },
            { TileType.Suitcase, TileType.Suitcase, TileType.Suitcase, TileType.Suitcase, TileType.Suitcase, TileType.Suitcase, TileType.Suitcase, TileType.Suitcase, TileType.Suitcase },
            { TileType.Suitcase, TileType.Suitcase, TileType.Suitcase, TileType.Suitcase, TileType.Suitcase, TileType.Suitcase, TileType.Suitcase, TileType.Suitcase, TileType.Suitcase },
        };

        public static TileType[,] BuildVideoStyleBoard(int rows, int cols)
        {
            var layout = new TileType[rows, cols];
            int copyRows = Math.Min(rows, VideoBoard8x9.GetLength(0));
            int copyCols = Math.Min(cols, VideoBoard8x9.GetLength(1));

            for (int r = 0; r < copyRows; r++)
            for (int c = 0; c < copyCols; c++)
                layout[r, c] = VideoBoard8x9[r, c];

            // 尺寸不一致时：多出的行填行李箱，多出的列用四色补齐（避免三连）
            var normals = new[] { TileType.Red, TileType.Yellow, TileType.Blue, TileType.Green };
            for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
            {
                if (r < copyRows && c < copyCols) continue;
                if (r >= 4)
                {
                    layout[r, c] = TileType.Suitcase;
                    continue;
                }
                layout[r, c] = normals[(r + c) % normals.Length];
            }

            return layout;
        }
    }
}
