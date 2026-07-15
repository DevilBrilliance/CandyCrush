using System.Collections.Generic;
using CandyCrush.Data;

namespace CandyCrush.Core
{
    public static class BoosterUtil
    {
        public static TileType ToTile(BoosterType b) => b switch
        {
            BoosterType.RocketH => TileType.RocketH,
            BoosterType.RocketV => TileType.RocketV,
            BoosterType.Propeller => TileType.Propeller,
            BoosterType.Bomb => TileType.Bomb,
            BoosterType.ColorBall => TileType.ColorBall,
            _ => TileType.Empty
        };
    }

    /// <summary>道具效果：火箭清行/列、炸弹 5×5、螺旋桨十字清格后追箱、彩球同色。</summary>
    public static class BoosterExecutor
    {
        public static void GetClearCells(BoardModel board, int row, int col, TileType booster, TileType partnerColor,
            List<GridPos> cells, GridPos? propellerTarget = null)
        {
            cells.Clear();
            if (!board.InBounds(row, col)) return;

            switch (booster)
            {
                case TileType.RocketH:
                    for (int c = 0; c < board.Cols; c++)
                        cells.Add(new GridPos(row, c));
                    break;
                case TileType.RocketV:
                    for (int r = 0; r < board.Rows; r++)
                        cells.Add(new GridPos(r, col));
                    break;
                case TileType.Bomb:
                    for (int r = row - 2; r <= row + 2; r++)
                    for (int c = col - 2; c <= col + 2; c++)
                        if (board.InBounds(r, c)) cells.Add(new GridPos(r, c));
                    break;
                case TileType.Propeller:
                    AddUnique(cells, new GridPos(row, col));
                    TryAdd(board, cells, row - 1, col);
                    TryAdd(board, cells, row + 1, col);
                    TryAdd(board, cells, row, col - 1);
                    TryAdd(board, cells, row, col + 1);
                    var target = propellerTarget.HasValue
                        ? propellerTarget
                        : FindPropellerTarget(board, row, col);
                    if (target.HasValue)
                        AddUnique(cells, target.Value);
                    break;
                case TileType.ColorBall:
                    var color = TileTypeUtil.IsNormal(partnerColor) ? partnerColor : FindAnyNormal(board);
                    cells.Add(new GridPos(row, col));
                    if (color != TileType.Empty)
                    {
                        for (int r = 0; r < board.Rows; r++)
                        for (int c = 0; c < board.Cols; c++)
                            if (board.Get(r, c) == color)
                                cells.Add(new GridPos(r, c));
                    }
                    break;
            }
        }

        public static List<GridPos> GetClearCells(BoardModel board, int row, int col, TileType booster, TileType partnerColor,
            GridPos? propellerTarget = null)
        {
            var cells = new List<GridPos>(16);
            GetClearCells(board, row, col, booster, partnerColor, cells, propellerTarget);
            return cells;
        }

        /// <param name="exclude">已被其它螺旋桨占用的目标，避免多桨追同一格。</param>
        public static GridPos? FindPropellerTarget(BoardModel board, int selfR, int selfC, ICollection<GridPos> exclude = null)
        {
            // 十字花（自身+上下左右）已清掉，追击不能再选这些格（邻格箱子不算追击目标）
            bool Skip(int r, int c)
            {
                if (r == selfR && c == selfC) return true;
                int d = Abs(r - selfR) + Abs(c - selfC);
                if (d <= 1) return true;
                if (exclude == null || exclude.Count == 0) return false;
                var p = new GridPos(r, c);
                foreach (var e in exclude)
                    if (e.Equals(p)) return true;
                return false;
            }

            // 优先行李箱（不含十字格内）
            GridPos? bestSuit = null;
            int bestDist = int.MaxValue;
            for (int r = 0; r < board.Rows; r++)
            for (int c = 0; c < board.Cols; c++)
            {
                if (Skip(r, c)) continue;
                if (board.Get(r, c) != TileType.Suitcase) continue;
                int d = Abs(r - selfR) + Abs(c - selfC);
                if (d < bestDist) { bestDist = d; bestSuit = new GridPos(r, c); }
            }
            if (bestSuit.HasValue) return bestSuit;

            // 其次任意非空且在十字外
            GridPos? bestOther = null;
            bestDist = int.MaxValue;
            for (int r = 0; r < board.Rows; r++)
            for (int c = 0; c < board.Cols; c++)
            {
                if (Skip(r, c)) continue;
                if (board.Get(r, c) == TileType.Empty) continue;
                int d = Abs(r - selfR) + Abs(c - selfC);
                if (d < bestDist) { bestDist = d; bestOther = new GridPos(r, c); }
            }
            return bestOther;
        }

        static void AddUnique(List<GridPos> cells, GridPos p)
        {
            for (int i = 0; i < cells.Count; i++)
                if (cells[i].Equals(p)) return;
            cells.Add(p);
        }

        static void TryAdd(BoardModel board, List<GridPos> cells, int r, int c)
        {
            if (board.InBounds(r, c)) AddUnique(cells, new GridPos(r, c));
        }

        static TileType FindAnyNormal(BoardModel board)
        {
            for (int r = 0; r < board.Rows; r++)
            for (int c = 0; c < board.Cols; c++)
            {
                var t = board.Get(r, c);
                if (TileTypeUtil.IsNormal(t)) return t;
            }
            return TileType.Empty;
        }

        static int Abs(int v) => v < 0 ? -v : v;
    }
}
