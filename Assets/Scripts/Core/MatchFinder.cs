using System.Collections.Generic;
using CandyCrush.Data;

namespace CandyCrush.Core
{
    /// <summary>横/竖扫描匹配；识别 4 连、5 连、L/T，输出 MatchGroup + 道具生成信息。</summary>
    public static class MatchFinder
    {
        public static List<MatchGroup> FindMatches(BoardModel board, bool enableColorBall = false)
        {
            int rows = board.Rows, cols = board.Cols;
            var visited = new bool[rows, cols];
            var groups = new List<MatchGroup>();

            // 横向
            for (int r = 0; r < rows; r++)
            {
                int c = 0;
                while (c < cols)
                {
                    var t = board.Get(r, c);
                    if (!TileTypeUtil.ParticipatesInColorMatch(t)) { c++; continue; }
                    int start = c;
                    while (c < cols && board.Get(r, c) == t) c++;
                    int len = c - start;
                    if (len >= 3)
                    {
                        var g = new MatchGroup { Color = t };
                        for (int i = start; i < c; i++)
                        {
                            g.Cells.Add(new GridPos(r, i));
                            visited[r, i] = true;
                        }
                        AnnotateBooster(g, len, true, enableColorBall);
                        groups.Add(g);
                    }
                }
            }

            // 纵向
            for (int c = 0; c < cols; c++)
            {
                int r = 0;
                while (r < rows)
                {
                    var t = board.Get(r, c);
                    if (!TileTypeUtil.ParticipatesInColorMatch(t)) { r++; continue; }
                    int start = r;
                    while (r < rows && board.Get(r, c) == t) r++;
                    int len = r - start;
                    if (len >= 3)
                    {
                        // 合并已有横向重叠组
                        MatchGroup target = null;
                        for (int i = start; i < r; i++)
                        {
                            if (!visited[i, c]) continue;
                            target = FindGroupContaining(groups, i, c);
                            if (target != null) break;
                        }

                        if (target == null)
                        {
                            target = new MatchGroup { Color = t };
                            groups.Add(target);
                        }

                        for (int i = start; i < r; i++)
                        {
                            var pos = new GridPos(i, c);
                            if (!Contains(target.Cells, pos))
                                target.Cells.Add(pos);
                            visited[i, c] = true;
                        }

                        // L/T：同时有横竖
                        if (target.Cells.Count >= 5 && target.SpawnBooster == BoosterType.None)
                        {
                            bool hasRowSpan = HasMultiCol(target);
                            bool hasColSpan = HasMultiRow(target);
                            if (hasRowSpan && hasColSpan)
                            {
                                target.SpawnBooster = BoosterType.Bomb;
                                target.SpawnAt = target.Cells[target.Cells.Count / 2];
                            }
                            else if (len >= 5 && enableColorBall)
                            {
                                target.SpawnBooster = BoosterType.ColorBall;
                                target.SpawnAt = new GridPos(start + len / 2, c);
                            }
                            else if (len >= 4)
                            {
                                target.SpawnBooster = BoosterType.RocketV;
                                target.SpawnAt = new GridPos(start + len / 2, c);
                            }
                        }
                        else if (len >= 4 && target.SpawnBooster == BoosterType.None)
                        {
                            target.SpawnBooster = BoosterType.RocketV;
                            target.SpawnAt = new GridPos(start + len / 2, c);
                        }
                        else if (len >= 5 && enableColorBall && target.SpawnBooster == BoosterType.None)
                        {
                            target.SpawnBooster = BoosterType.ColorBall;
                            target.SpawnAt = new GridPos(start + len / 2, c);
                        }
                    }
                }
            }

            // 2x2 方块 → 螺旋桨
            FindSquares(board, groups, enableColorBall);

            // 确保每个 group 有 SpawnAt
            foreach (var g in groups)
            {
                if (g.SpawnBooster != BoosterType.None && (g.SpawnAt.Row == 0 && g.SpawnAt.Col == 0 && !Contains(g.Cells, g.SpawnAt)))
                    g.SpawnAt = g.Cells[g.Cells.Count / 2];
            }

            return groups;
        }

        static void AnnotateBooster(MatchGroup g, int len, bool horizontal, bool enableColorBall)
        {
            if (len >= 5 && enableColorBall)
            {
                g.SpawnBooster = BoosterType.ColorBall;
                g.SpawnAt = g.Cells[g.Cells.Count / 2];
            }
            else if (len >= 4)
            {
                g.SpawnBooster = horizontal ? BoosterType.RocketH : BoosterType.RocketV;
                g.SpawnAt = g.Cells[g.Cells.Count / 2];
            }
        }

        static void FindSquares(BoardModel board, List<MatchGroup> groups, bool enableColorBall)
        {
            for (int r = 0; r < board.Rows - 1; r++)
            for (int c = 0; c < board.Cols - 1; c++)
            {
                var t = board.Get(r, c);
                if (!TileTypeUtil.ParticipatesInColorMatch(t)) continue;
                if (board.Get(r, c + 1) != t || board.Get(r + 1, c) != t || board.Get(r + 1, c + 1) != t)
                    continue;

                // 若四格都已在某组中则跳过；否则新建螺旋桨组
                bool allIn = true;
                for (int dr = 0; dr < 2 && allIn; dr++)
                for (int dc = 0; dc < 2 && allIn; dc++)
                    if (FindGroupContaining(groups, r + dr, c + dc) == null) allIn = false;
                if (allIn) continue;

                var g = new MatchGroup
                {
                    Color = t,
                    SpawnBooster = BoosterType.Propeller,
                    SpawnAt = new GridPos(r, c)
                };
                g.Cells.Add(new GridPos(r, c));
                g.Cells.Add(new GridPos(r, c + 1));
                g.Cells.Add(new GridPos(r + 1, c));
                g.Cells.Add(new GridPos(r + 1, c + 1));
                groups.Add(g);
            }
        }

        static MatchGroup FindGroupContaining(List<MatchGroup> groups, int r, int c)
        {
            var p = new GridPos(r, c);
            for (int i = 0; i < groups.Count; i++)
                if (Contains(groups[i].Cells, p)) return groups[i];
            return null;
        }

        static bool Contains(List<GridPos> list, GridPos p)
        {
            for (int i = 0; i < list.Count; i++)
                if (list[i].Equals(p)) return true;
            return false;
        }

        static bool HasMultiRow(MatchGroup g)
        {
            int min = int.MaxValue, max = int.MinValue;
            foreach (var p in g.Cells) { if (p.Row < min) min = p.Row; if (p.Row > max) max = p.Row; }
            return max > min;
        }

        static bool HasMultiCol(MatchGroup g)
        {
            int min = int.MaxValue, max = int.MinValue;
            foreach (var p in g.Cells) { if (p.Col < min) min = p.Col; if (p.Col > max) max = p.Col; }
            return max > min;
        }
    }
}
