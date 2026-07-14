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

    /// <summary>道具效果：火箭清行/列、炸弹 5×5、螺旋桨追箱、彩球同色。</summary>
    public static class BoosterExecutor
    {
        public static List<GridPos> GetClearCells(BoardModel board, int row, int col, TileType booster, TileType partnerColor)
        {
            var cells = new List<GridPos>();
            if (!board.InBounds(row, col)) return cells;

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
                    // 以炸弹为中心 5×5
                    for (int r = row - 2; r <= row + 2; r++)
                    for (int c = col - 2; c <= col + 2; c++)
                        if (board.InBounds(r, c)) cells.Add(new GridPos(r, c));
                    break;
                case TileType.Propeller:
                    cells.Add(new GridPos(row, col));
                    var target = FindPropellerTarget(board, row, col);
                    if (target.HasValue) cells.Add(target.Value);
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

            return cells;
        }

        static GridPos? FindPropellerTarget(BoardModel board, int selfR, int selfC)
        {
            // 优先行李箱
            GridPos? bestSuit = null;
            int bestDist = int.MaxValue;
            for (int r = 0; r < board.Rows; r++)
            for (int c = 0; c < board.Cols; c++)
            {
                if (r == selfR && c == selfC) continue;
                if (board.Get(r, c) != TileType.Suitcase) continue;
                int d = Abs(r - selfR) + Abs(c - selfC);
                if (d < bestDist) { bestDist = d; bestSuit = new GridPos(r, c); }
            }
            if (bestSuit.HasValue) return bestSuit;

            // 其次任意非空非自身
            for (int r = 0; r < board.Rows; r++)
            for (int c = 0; c < board.Cols; c++)
            {
                if (r == selfR && c == selfC) continue;
                if (board.Get(r, c) != TileType.Empty)
                    return new GridPos(r, c);
            }
            return null;
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
