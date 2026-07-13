using System;
using CandyCrush.Data;

namespace CandyCrush.View
{
    /// <summary>参考视频/图一的初始布局：上 4 行四色，第 5 行两侧箱+中间三色，底 3 行全箱（共 33 箱）。</summary>
    public static class DemoLayouts
    {
        static readonly TileType[,] VideoBoard8x9 =
        {
            { TileType.Green, TileType.Blue, TileType.Red, TileType.Red, TileType.Yellow, TileType.Green, TileType.Yellow, TileType.Blue, TileType.Red },
            { TileType.Green, TileType.Yellow, TileType.Red, TileType.Yellow, TileType.Blue, TileType.Yellow, TileType.Yellow, TileType.Red, TileType.Blue },
            { TileType.Red, TileType.Blue, TileType.Yellow, TileType.Green, TileType.Red, TileType.Green, TileType.Green, TileType.Yellow, TileType.Green },
            { TileType.Blue, TileType.Green, TileType.Green, TileType.Blue, TileType.Green, TileType.Blue, TileType.Yellow, TileType.Red, TileType.Yellow },
            { TileType.Suitcase, TileType.Suitcase, TileType.Suitcase, TileType.Green, TileType.Yellow, TileType.Green, TileType.Suitcase, TileType.Suitcase, TileType.Suitcase },
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
