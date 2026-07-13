using System;
using CandyCrush.Data;

namespace CandyCrush.Core
{
    /// <summary>相邻校验；无匹配则视为无效（调用方负责回滚）。</summary>
    public static class SwapValidator
    {
        public static bool AreAdjacent(int r0, int c0, int r1, int c1) =>
            Math.Abs(r0 - r1) + Math.Abs(c0 - c1) == 1;

        public static bool WouldCreateMatch(BoardModel board, int r0, int c0, int r1, int c1, bool enableColorBall = false)
        {
            if (!board.InBounds(r0, c0) || !board.InBounds(r1, c1)) return false;
            if (!AreAdjacent(r0, c0, r1, c1)) return false;

            var a = board.Get(r0, c0);
            var b = board.Get(r1, c1);
            // 空格不可换；道具可以和任意块交换激活
            if (a == TileType.Empty || b == TileType.Empty) return false;

            if (TileTypeUtil.IsBooster(a) || TileTypeUtil.IsBooster(b))
                return true;

            board.Swap(r0, c0, r1, c1);
            var matches = MatchFinder.FindMatches(board, enableColorBall);
            board.Swap(r0, c0, r1, c1); // 回滚探测
            return matches.Count > 0;
        }
    }
}
