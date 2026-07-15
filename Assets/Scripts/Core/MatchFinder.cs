using System.Collections.Generic;
using CandyCrush.Data;

namespace CandyCrush.Core
{
    /// <summary>横/竖扫描匹配；识别 4 连、5 连、L/T，输出 MatchGroup + 道具生成信息。</summary>
    public static class MatchFinder
    {
        static bool[,] _visited;
        static int _visitedRows;
        static int _visitedCols;
        static readonly List<MatchGroup> _groups = new List<MatchGroup>(16);
        static readonly Stack<MatchGroup> _pool = new Stack<MatchGroup>(16);

        public static List<MatchGroup> FindMatches(BoardModel board, bool enableColorBall = false)
        {
            int rows = board.Rows, cols = board.Cols;
            EnsureVisited(rows, cols);
            RecycleGroups();

            // 横向
            for (int r = 0; r < rows; r++)
            {
                int c = 0;
                while (c < cols)
                {
                    var t = board.Get(r, c);
                    if (!TileTypeUtil.IsNormal(t)) { c++; continue; }
                    int start = c;
                    while (c < cols && board.Get(r, c) == t) c++;
                    int len = c - start;
                    if (len >= 3)
                    {
                        var g = RentGroup();
                        for (int i = start; i < c; i++)
                        {
                            g.Cells.Add(new GridPos(r, i));
                            _visited[r, i] = true;
                        }
                        AnnotateBooster(g, len, true, enableColorBall);
                        _groups.Add(g);
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
                    if (!TileTypeUtil.IsNormal(t)) { r++; continue; }
                    int start = r;
                    while (r < rows && board.Get(r, c) == t) r++;
                    int len = r - start;
                    if (len >= 3)
                    {
                        MatchGroup target = null;
                        for (int i = start; i < r; i++)
                        {
                            if (!_visited[i, c]) continue;
                            target = FindGroupContaining(_groups, i, c);
                            if (target != null) break;
                        }

                        if (target == null)
                        {
                            target = RentGroup();
                            _groups.Add(target);
                        }

                        for (int i = start; i < r; i++)
                        {
                            var pos = new GridPos(i, c);
                            if (!Contains(target.Cells, pos))
                                target.Cells.Add(pos);
                            _visited[i, c] = true;
                        }

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
                            target.SpawnBooster = enableColorBall && len >= 5
                                ? BoosterType.ColorBall
                                : BoosterType.RocketV;
                            target.SpawnAt = new GridPos(start + len / 2, c);
                        }
                    }
                }
            }

            FindSquares(board, _groups);

            for (int gi = 0; gi < _groups.Count; gi++)
            {
                var g = _groups[gi];
                if (g.SpawnBooster != BoosterType.None &&
                    (g.SpawnAt.Row == 0 && g.SpawnAt.Col == 0 && !Contains(g.Cells, g.SpawnAt)))
                    g.SpawnAt = g.Cells[g.Cells.Count / 2];
            }

            return _groups;
        }

        static void EnsureVisited(int rows, int cols)
        {
            if (_visited == null || _visitedRows != rows || _visitedCols != cols)
            {
                _visited = new bool[rows, cols];
                _visitedRows = rows;
                _visitedCols = cols;
                return;
            }

            for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                _visited[r, c] = false;
        }

        static void RecycleGroups()
        {
            for (int i = 0; i < _groups.Count; i++)
            {
                var g = _groups[i];
                g.Cells.Clear();
                g.SpawnBooster = BoosterType.None;
                g.SpawnAt = default;
                _pool.Push(g);
            }
            _groups.Clear();
        }

        static MatchGroup RentGroup()
        {
            if (_pool.Count > 0)
                return _pool.Pop();
            return new MatchGroup();
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

        static void FindSquares(BoardModel board, List<MatchGroup> groups)
        {
            for (int r = 0; r < board.Rows - 1; r++)
            for (int c = 0; c < board.Cols - 1; c++)
            {
                var t = board.Get(r, c);
                if (!TileTypeUtil.IsNormal(t)) continue;
                if (board.Get(r, c + 1) != t || board.Get(r + 1, c) != t || board.Get(r + 1, c + 1) != t)
                    continue;

                bool allIn = true;
                for (int dr = 0; dr < 2 && allIn; dr++)
                for (int dc = 0; dc < 2 && allIn; dc++)
                    if (FindGroupContaining(groups, r + dr, c + dc) == null) allIn = false;
                if (allIn) continue;

                var g = RentGroup();
                g.SpawnBooster = BoosterType.Propeller;
                g.SpawnAt = new GridPos(r, c);
                g.Cells.Add(new GridPos(r, c));
                g.Cells.Add(new GridPos(r, c + 1));
                g.Cells.Add(new GridPos(r + 1, c));
                g.Cells.Add(new GridPos(r + 1, c + 1));
                groups.Add(g);
            }
        }

        static MatchGroup FindGroupContaining(List<MatchGroup> groups, int r, int c)
        {
            for (int i = 0; i < groups.Count; i++)
            {
                var cells = groups[i].Cells;
                for (int j = 0; j < cells.Count; j++)
                    if (cells[j].Row == r && cells[j].Col == c) return groups[i];
            }
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
            for (int i = 0; i < g.Cells.Count; i++)
            {
                int row = g.Cells[i].Row;
                if (row < min) min = row;
                if (row > max) max = row;
            }
            return max > min;
        }

        static bool HasMultiCol(MatchGroup g)
        {
            int min = int.MaxValue, max = int.MinValue;
            for (int i = 0; i < g.Cells.Count; i++)
            {
                int col = g.Cells[i].Col;
                if (col < min) min = col;
                if (col > max) max = col;
            }
            return max > min;
        }
    }
}
