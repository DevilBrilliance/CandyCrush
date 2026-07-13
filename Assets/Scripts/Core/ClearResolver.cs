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
        public static void Resolve(
            BoardModel board,
            IEnumerable<GridPos> clearCells,
            ObjectiveTracker objective,
            out List<GridPos> cleared,
            out List<TileType> clearedTypes,
            out List<GridPos> collectedSuitcases)
        {
            cleared = new List<GridPos>();
            clearedTypes = new List<TileType>();
            collectedSuitcases = new List<GridPos>();
            var set = new HashSet<GridPos>();

            foreach (var p in clearCells)
            {
                if (!board.InBounds(p.Row, p.Col)) continue;
                if (!set.Add(p)) continue;

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
            var extra = new List<GridPos>();
            var existing = new HashSet<GridPos>(cells);
            int[] dr = { -1, 1, 0, 0 };
            int[] dc = { 0, 0, -1, 1 };

            foreach (var p in cells)
            {
                for (int i = 0; i < 4; i++)
                {
                    int nr = p.Row + dr[i], nc = p.Col + dc[i];
                    if (!board.InBounds(nr, nc)) continue;
                    var np = new GridPos(nr, nc);
                    if (existing.Contains(np)) continue;
                    if (board.Get(nr, nc) == TileType.Suitcase)
                    {
                        extra.Add(np);
                        existing.Add(np);
                    }
                }
            }
            cells.AddRange(extra);
        }
    }
}
