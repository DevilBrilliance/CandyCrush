using System.Collections;
using System.Collections.Generic;
using CandyCrush.Common;
using CandyCrush.Core;
using CandyCrush.Data;
using CandyCrush.Vfx;
using UnityEngine;

namespace CandyCrush.View
{
    /// <summary>棋盘表现：镜像 BoardModel，播放交换/消除/下落/生成。</summary>
    public class BoardView : MonoBehaviour
    {
        [SerializeField] LevelConfig levelConfig;
        [SerializeField] TileSpriteCatalog catalog;
        [SerializeField] Transform tileRoot;
        [SerializeField] SpriteRenderer boardBg;
        [SerializeField] float cellSize = 0.95f;
        [SerializeField] Vector2 boardOrigin;
        [SerializeField] float swapDuration = 0.18f;
        [SerializeField] float fallDuration = 0.22f;
        [SerializeField] float clearDuration = 0.15f;

        BoardModel _model;
        TileView[,] _views;
        ClearBurstFx _clearFx;

        public BoardModel Model => _model;
        public TileSpriteCatalog Catalog => catalog;
        public LevelConfig Config => levelConfig;

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
            _clearFx = ClearBurstFx.Ensure(transform, catalog);
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
                _views[r, c] = CreateTileView(type, r, c, CellLocal(r, c));
            }
        }

        /// <summary>增量对齐 Model：只补洞/纠错/清残留，避免全盘 Destroy+新建。</summary>
        public void SyncFromModel() => ReconcileViews();

        public void ReconcileViews()
        {
            if (_model == null) return;
            if (tileRoot == null) tileRoot = transform;
            if (_views == null || _views.GetLength(0) != _model.Rows || _views.GetLength(1) != _model.Cols)
                _views = new TileView[_model.Rows, _model.Cols];

            for (int r = 0; r < _model.Rows; r++)
            for (int c = 0; c < _model.Cols; c++)
            {
                var type = _model.Get(r, c);
                var view = _views[r, c];

                if (type == TileType.Empty)
                {
                    if (view != null)
                    {
                        DestroyTile(view);
                        _views[r, c] = null;
                    }
                    continue;
                }

                if (view != null && view.Type != type)
                {
                    DestroyTile(view);
                    view = null;
                    _views[r, c] = null;
                }

                if (view == null)
                {
                    _views[r, c] = CreateTileView(type, r, c, CellLocal(r, c));
                    continue;
                }

                view.SetGridPos(r, c);
                view.RestoreVisual();
                view.transform.localPosition = CellLocal(r, c);
            }
        }

        /// <summary>立刻摘掉并销毁格子视图（收集飞行另建副本）。</summary>
        public void ConsumeViews(IReadOnlyList<GridPos> cells)
        {
            if (cells == null || _views == null) return;
            for (int i = 0; i < cells.Count; i++)
            {
                var p = cells[i];
                if (!_model.InBounds(p.Row, p.Col)) continue;
                var v = _views[p.Row, p.Col];
                if (v == null) continue;
                _views[p.Row, p.Col] = null;
                DestroyTile(v);
            }
        }

        public TileView GetView(int row, int col) =>
            _model != null && _model.InBounds(row, col) ? _views[row, col] : null;

        public bool TryGetCell(Vector3 worldPos, out int row, out int col)
        {
            var local = transform.InverseTransformPoint(worldPos);
            return GridUtil.TryWorldToCell(local, _model.Rows, _model.Cols, cellSize, boardOrigin, out row, out col);
        }

        public Vector3 CellLocal(int row, int col) =>
            GridUtil.CellToLocal(row, col, _model.Rows, _model.Cols, cellSize, boardOrigin);

        public IEnumerator AnimateSwap(int r0, int c0, int r1, int c1)
        {
            var a = _views[r0, c0];
            var b = _views[r1, c1];
            var p0 = CellLocal(r0, c0);
            var p1 = CellLocal(r1, c1);
            float dur = swapDuration > 0.01f ? swapDuration : 0.18f;

            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / dur);
                u = u * u * (3f - 2f * u);
                if (a != null) a.transform.localPosition = Vector3.Lerp(p0, p1, u);
                if (b != null) b.transform.localPosition = Vector3.Lerp(p1, p0, u);
                yield return null;
            }

            _views[r0, c0] = b;
            _views[r1, c1] = a;
            if (a != null) { a.transform.localPosition = p1; a.SetGridPos(r1, c1); }
            if (b != null) { b.transform.localPosition = p0; b.SetGridPos(r0, c0); }
        }

        public IEnumerator AnimateSwapBack(int r0, int c0, int r1, int c1)
        {
            yield return AnimateSwap(r0, c0, r1, c1);
        }

        public IEnumerator PlayCascadeStep(CascadeStepResult step)
        {
            if (step == null || !step.HadWork) yield break;

            float clearDur = clearDuration > 0.01f ? clearDuration : 0.16f;
            float fallDur = fallDuration > 0.01f ? fallDuration : 0.22f;

            if (_clearFx == null)
                _clearFx = ClearBurstFx.Ensure(transform, catalog);
            else
                _clearFx.Configure(catalog);

            // --- 消除：立刻隐藏棋子，碎裂由 ClearBurstFx 承担 ---
            var toDestroy = new List<TileView>();
            for (int i = 0; i < step.Cleared.Count; i++)
            {
                var p = step.Cleared[i];
                var v = _views[p.Row, p.Col];
                var type = i < step.ClearedTypes.Count
                    ? step.ClearedTypes[i]
                    : (v != null ? v.Type : TileType.Empty);

                var world = transform.TransformPoint(CellLocal(p.Row, p.Col));
                if (type != TileType.Empty)
                    _clearFx.Play(type, world, cellSize);

                if (v == null) continue;
                _views[p.Row, p.Col] = null;
                v.gameObject.SetActive(false);
                toDestroy.Add(v);
            }

            float t = 0f;
            while (t < clearDur)
            {
                t += Time.deltaTime;
                yield return null;
            }

            foreach (var view in toDestroy)
                DestroyTile(view);

            // --- 下落 ---
            var falling = new List<(TileView view, Vector3 from, Vector3 to)>();
            var fallList = new List<FallMove>(step.Falls);
            fallList.Sort((a, b) =>
            {
                int cmp = a.To.Col.CompareTo(b.To.Col);
                return cmp != 0 ? cmp : b.To.Row.CompareTo(a.To.Row);
            });

            foreach (var move in fallList)
            {
                var v = _views[move.From.Row, move.From.Col];
                if (v == null)
                    v = CreateTileView(move.Type, move.From.Row, move.From.Col, CellLocal(move.From.Row, move.From.Col));

                var occupant = _views[move.To.Row, move.To.Col];
                if (occupant != null && occupant != v)
                {
                    DestroyTile(occupant);
                    _views[move.To.Row, move.To.Col] = null;
                }

                _views[move.From.Row, move.From.Col] = null;
                _views[move.To.Row, move.To.Col] = v;
                v.SetGridPos(move.To.Row, move.To.Col);
                falling.Add((v, v.transform.localPosition, CellLocal(move.To.Row, move.To.Col)));
            }

            foreach (var (at, type) in step.SpawnedBoosters)
            {
                if (_model.Get(at.Row, at.Col) != type) continue;
                if (_views[at.Row, at.Col] != null) continue;
                _views[at.Row, at.Col] = CreateTileView(type, at.Row, at.Col, CellLocal(at.Row, at.Col));
            }

            var spawning = new List<(TileView view, Vector3 from, Vector3 to)>();
            foreach (var spawn in step.Spawns)
            {
                if (_views[spawn.To.Row, spawn.To.Col] != null) continue;
                if (_model.Get(spawn.To.Row, spawn.To.Col) == TileType.Empty) continue;

                var dest = CellLocal(spawn.To.Row, spawn.To.Col);
                var start = dest + Vector3.up * (cellSize * (spawn.To.Row + 2));
                var v = CreateTileView(spawn.Type, spawn.To.Row, spawn.To.Col, start);
                _views[spawn.To.Row, spawn.To.Col] = v;
                spawning.Add((v, start, dest));
            }

            t = 0f;
            while (t < fallDur)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / fallDur);
                u = u * u * (3f - 2f * u);
                foreach (var f in falling)
                    if (f.view != null) f.view.transform.localPosition = Vector3.Lerp(f.from, f.to, u);
                foreach (var s in spawning)
                    if (s.view != null) s.view.transform.localPosition = Vector3.Lerp(s.from, s.to, u);
                yield return null;
            }

            foreach (var f in falling)
                if (f.view != null) f.view.transform.localPosition = f.to;
            foreach (var s in spawning)
                if (s.view != null) s.view.transform.localPosition = s.to;

            // 轻量对齐即可，不再全盘重建
            ReconcileViews();
        }

        TileView CreateTileView(TileType type, int row, int col, Vector3 localPos)
        {
            var sprite = catalog != null ? catalog.GetSprite(type) : null;
            if (sprite == null)
            {
                Debug.LogWarning($"[BoardView] Missing sprite for {type}, using fallback.");
                sprite = CreateFallbackSprite(type);
            }

            var go = new GameObject($"Tile_{row}_{col}_{type}");
            go.transform.SetParent(tileRoot, false);
            go.transform.localPosition = localPos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 10;
            var view = go.AddComponent<TileView>();
            view.Setup(type, sprite, row, col, cellSize);
            return view;
        }

        void DestroyTile(TileView view)
        {
            if (view == null) return;
            if (Application.isPlaying) Destroy(view.gameObject);
            else DestroyImmediate(view.gameObject);
        }

        static Sprite CreateFallbackSprite(TileType type)
        {
            var tex = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            var color = type switch
            {
                TileType.Propeller => new Color(1f, 0.4f, 0.9f),
                TileType.Bomb => new Color(1f, 0.2f, 0.2f),
                TileType.RocketH => new Color(0.3f, 0.8f, 1f),
                TileType.RocketV => new Color(0.3f, 0.8f, 1f),
                TileType.ColorBall => new Color(1f, 0.85f, 0.2f),
                _ => Color.magenta
            };
            var pixels = new Color[64];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            tex.SetPixels(pixels);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 8f);
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

        void ClearChildren(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i).gameObject;
                // 先摘掉，避免同帧立刻新建时 childCount 仍含待 Destroy 对象
                child.transform.SetParent(null, false);
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }
        }
    }
}
