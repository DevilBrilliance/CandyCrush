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
        [Tooltip("消除前缩到 clearShrinkScale 的时长")]
        [SerializeField] float clearShrinkDuration = 0.1f;
        [Tooltip("消除前缩到的相对基准缩放（0.1 = 缩小到原尺寸 10%）")]
        [SerializeField] float clearShrinkScale = 0.1f;
        [Tooltip("新块落到格位后的微震动时长")]
        [SerializeField] float landShakeDuration = 0.3f;
        [Tooltip("落地震动强度（相对基准缩放）")]
        [SerializeField] float landShakeStrength = 0.07f;

        BoardModel _model;
        TileView[,] _views;
        ClearBurstFx _clearFx;
        BoosterFx _boosterFx;
        Transform _cellBgRoot;

        public BoardModel Model => _model;
        public TileSpriteCatalog Catalog => catalog;
        public LevelConfig Config => levelConfig;
        public float CellSizeSafe() => cellSize > 0.01f ? cellSize : 0.95f;

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
            _boosterFx = BoosterFx.Ensure(transform);
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

            if (_boosterFx == null)
                _boosterFx = BoosterFx.Ensure(transform);

            // --- 道具轨迹（火箭/螺旋桨/炸弹）---
            if (step.ActivatedBoosters.Count > 0)
                yield return _boosterFx.PlayActivations(step.ActivatedBoosters, this);

            // --- 消除前：匹配棋子先缩到 0.1，期间其他块不动 ---
            var shrinking = new List<(TileView view, Vector3 fromScale, Vector3 toScale)>();
            for (int i = 0; i < step.Cleared.Count; i++)
            {
                var p = step.Cleared[i];
                var v = _views[p.Row, p.Col];
                if (v == null) continue;
                float baseS = v.BaseScale;
                var from = Vector3.one * baseS;
                var to = Vector3.one * (baseS * Mathf.Clamp(clearShrinkScale, 0.01f, 1f));
                shrinking.Add((v, from, to));
                v.transform.localScale = from; // 若被选中放大，先回到基准再缩
            }

            float shrinkDur = clearShrinkDuration > 0.01f ? clearShrinkDuration : 0.3f;
            float t = 0f;
            while (t < shrinkDur)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / shrinkDur);
                u = u * u * (3f - 2f * u);
                foreach (var s in shrinking)
                    if (s.view != null) s.view.transform.localScale = Vector3.Lerp(s.fromScale, s.toScale, u);
                yield return null;
            }
            foreach (var s in shrinking)
                if (s.view != null) s.view.transform.localScale = s.toScale;

            // --- 消除碎裂 + 同步下落（缩放完后才开始）---
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

            if (step.SpawnedBoosters.Count > 0 && _boosterFx != null)
                StartCoroutine(_boosterFx.PlaySpawnPops(step.SpawnedBoosters, this));

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

            // 碎裂与下落同时进行；至少等 clearDur，保证碎裂有可见首帧
            float animDur = Mathf.Max(fallDur, clearDur);
            t = 0f;
            while (t < animDur)
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

            foreach (var view in toDestroy)
                DestroyTile(view);

            // --- 落地微震动（下落/新生成的块）---
            var landed = new List<(TileView view, Vector3 restPos)>(falling.Count + spawning.Count);
            foreach (var f in falling)
                if (f.view != null) landed.Add((f.view, f.to));
            foreach (var s in spawning)
                if (s.view != null) landed.Add((s.view, s.to));
            if (landed.Count > 0)
                yield return PlayLandShake(landed);

            ReconcileViews();
        }

        IEnumerator PlayLandShake(List<(TileView view, Vector3 restPos)> tiles)
        {
            float dur = landShakeDuration > 0.01f ? landShakeDuration : 0.3f;
            float strength = Mathf.Max(0.01f, landShakeStrength);
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / dur);
                // 衰减正弦：落地压扁→回弹→迅速归稳
                float wobble = Mathf.Sin(u * Mathf.PI * 5f) * (1f - u) * (1f - u) * strength;
                foreach (var item in tiles)
                {
                    if (item.view == null) continue;
                    float baseS = item.view.BaseScale;
                    float sx = baseS * (1f + wobble);
                    float sy = baseS * (1f - wobble * 0.9f);
                    item.view.transform.localScale = new Vector3(sx, sy, 1f);
                    item.view.transform.localPosition =
                        item.restPos + Vector3.up * (wobble * cellSize * 0.12f);
                }
                yield return null;
            }

            foreach (var item in tiles)
            {
                if (item.view == null) continue;
                item.view.RestoreVisual();
                item.view.transform.localPosition = item.restPos;
            }
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
            if (_model == null) return;

            float pad = cellSize * 0.1f;
            float innerW = _model.Cols * cellSize + pad;
            float innerH = _model.Rows * cellSize + pad;
            float border = cellSize * 0.07f;
            var center = new Vector3(boardOrigin.x, boardOrigin.y, 0f);

            // 棋盘底板保持不动（外框 + 内填充），只另铺 tileB 格子
            if (boardBg != null)
            {
                var frame = catalog != null ? catalog.boardPanelSprite : null;
                if (frame == null) frame = Resources.Load<Sprite>("Board/UIpanel_outside");
                if (frame == null) frame = Resources.Load<Sprite>("Board/candy_bg_01");
                if (frame == null && catalog != null) frame = catalog.boardCellSprite;
                if (frame == null) frame = Resources.Load<Sprite>("Board/tileA");

                var fill = Resources.Load<Sprite>("Board/UIpanel_inside");
                if (fill == null) fill = frame;

                if (frame != null)
                {
                    FitSlicedBoard(boardBg, frame, innerW + border * 2f, innerH + border * 2f);
                    boardBg.enabled = true;
                    boardBg.transform.localPosition = center;
                    boardBg.color = new Color(0.55f, 0.78f, 1f, 0.95f);
                    boardBg.sortingOrder = 0;

                    if (_cellBgRoot == null)
                    {
                        var go = new GameObject("BoardFill");
                        go.transform.SetParent(transform, false);
                        _cellBgRoot = go.transform;
                    }

                    var fillGo = _cellBgRoot.Find("Inner");
                    SpriteRenderer fillSr;
                    if (fillGo == null)
                    {
                        var go = new GameObject("Inner");
                        go.transform.SetParent(_cellBgRoot, false);
                        fillSr = go.AddComponent<SpriteRenderer>();
                    }
                    else fillSr = fillGo.GetComponent<SpriteRenderer>();

                    FitSlicedBoard(fillSr, fill, innerW, innerH);
                    fillSr.transform.localPosition = center;
                    fillSr.color = new Color(0.28f, 0.48f, 0.78f, 0.62f);
                    fillSr.sortingOrder = 1;
                }
            }

            // 只用 tileB 按棋子格位铺一层
            var cell = catalog != null ? catalog.boardCellAltSprite : null;
            if (cell == null) cell = Resources.Load<Sprite>("Board/tileB");
            if (cell == null && catalog != null) cell = catalog.boardCellSprite;
            if (cell == null)
            {
                Debug.LogWarning("[BoardView] Missing Board/tileB.");
                return;
            }

            Transform cellRoot = transform.Find("BoardCells");
            if (cellRoot == null)
            {
                var go = new GameObject("BoardCells");
                go.transform.SetParent(transform, false);
                cellRoot = go.transform;
            }

            for (int i = cellRoot.childCount - 1; i >= 0; i--)
            {
                var child = cellRoot.GetChild(i).gameObject;
                child.transform.SetParent(null, false);
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }

            float fit = cellSize;
            for (int r = 0; r < _model.Rows; r++)
            for (int c = 0; c < _model.Cols; c++)
            {
                var cellGo = new GameObject($"Cell_{r}_{c}");
                cellGo.transform.SetParent(cellRoot, false);
                cellGo.transform.localPosition = CellLocal(r, c);

                var sr = cellGo.AddComponent<SpriteRenderer>();
                sr.sprite = cell;
                sr.color = new Color(1f, 1f, 1f, 0.45f);
                sr.sortingOrder = 2;
                sr.drawMode = SpriteDrawMode.Simple;

                var b = cell.bounds.size;
                if (b.x > 0.0001f && b.y > 0.0001f)
                    cellGo.transform.localScale = new Vector3(fit / b.x, fit / b.y, 1f);
            }
        }

        static void FitSlicedBoard(SpriteRenderer sr, Sprite sprite, float worldW, float worldH)
        {
            if (sr == null || sprite == null) return;
            sr.sprite = sprite;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = new Vector2(Mathf.Max(0.1f, worldW), Mathf.Max(0.1f, worldH));
            sr.transform.localScale = Vector3.one;
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
