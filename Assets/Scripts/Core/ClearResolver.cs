using System.Collections.Generic;
using CandyCrush.Data;

namespace CandyCrush.Core
{
    public sealed class ObjectiveTracker
    {
        public ObjectiveType Type { get; }
        public int Remaining { get; private set; }
        public bool IsComplete => Remaining <= 0;

        public ObjectiveTracker(ObjectiveType type, int count)
        {
            Type = type;
            Remaining = count < 0 ? 0 : count;
        }

        public int Collect(int amount = 1)
        {
            if (amount <= 0 || Remaining <= 0) return 0;
            int applied = amount > Remaining ? Remaining : amount;
            Remaining -= applied;
            return applied;
        }
    }

    /// <summary>清格；行李箱进入清除集则收集。</summary>
    public static class ClearResolver
    {
        static readonly HashSet<GridPos> _set = new HashSet<GridPos>();
        static readonly HashSet<GridPos> _existing = new HashSet<GridPos>();
        static readonly List<GridPos> _extra = new List<GridPos>(16);
        static readonly int[] Dr = { -1, 1, 0, 0 };
        static readonly int[] Dc = { 0, 0, -1, 1 };

        public static void Resolve(
            BoardModel board,
            List<GridPos> clearCells,
            ObjectiveTracker objective,
            List<GridPos> cleared,
            List<TileType> clearedTypes,
            List<GridPos> collectedSuitcases)
        {
            cleared.Clear();
            clearedTypes.Clear();
            collectedSuitcases.Clear();
            _set.Clear();

            for (int i = 0; i < clearCells.Count; i++)
            {
                var p = clearCells[i];
                if (!board.InBounds(p.Row, p.Col)) continue;
                if (!_set.Add(p)) continue;

                var t = board.Get(p.Row, p.Col);
                if (t == TileType.Empty) continue;

                if (t == TileType.Suitcase)
                {
                    collectedSuitcases.Add(p);
                    if (objective != null && objective.Type == ObjectiveType.CollectSuitcase)
                        objective.Collect(1);
                }

                board.Set(p.Row, p.Col, TileType.Empty);
                cleared.Add(p);
                clearedTypes.Add(t);
            }
        }

        /// <summary>匹配消除时，把贴邻匹配格的行李箱一并波及。</summary>
        public static void ExpandWithAdjacentSuitcases(BoardModel board, List<GridPos> cells)
        {
            _extra.Clear();
            _existing.Clear();
            for (int i = 0; i < cells.Count; i++)
                _existing.Add(cells[i]);

            for (int ci = 0; ci < cells.Count; ci++)
            {
                var p = cells[ci];
                for (int i = 0; i < 4; i++)
                {
                    int nr = p.Row + Dr[i], nc = p.Col + Dc[i];
                    if (!board.InBounds(nr, nc)) continue;
                    var np = new GridPos(nr, nc);
                    if (_existing.Contains(np)) continue;
                    if (board.Get(nr, nc) == TileType.Suitcase)
                    {
                        _extra.Add(np);
                        _existing.Add(np);
                    }
                }
            }

            for (int i = 0; i < _extra.Count; i++)
                cells.Add(_extra[i]);
        }
    }
}
