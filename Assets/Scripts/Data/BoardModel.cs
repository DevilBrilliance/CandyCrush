using System;

namespace CandyCrush.Data
{
    /// <summary>棋盘唯一真值。纯 C#，View 只读镜像。</summary>
    public sealed class BoardModel
    {
        public int Rows { get; }
        public int Cols { get; }

        readonly TileType[,] _cells;

        public BoardModel(int rows, int cols)
        {
            if (rows <= 0 || cols <= 0) throw new ArgumentOutOfRangeException();
            Rows = rows;
            Cols = cols;
            _cells = new TileType[rows, cols];
        }

        public TileType Get(int row, int col)
        {
            EnsureInBounds(row, col);
            return _cells[row, col];
        }

        public void Set(int row, int col, TileType type)
        {
            EnsureInBounds(row, col);
            _cells[row, col] = type;
        }

        public void Swap(int r0, int c0, int r1, int c1)
        {
            EnsureInBounds(r0, c0);
            EnsureInBounds(r1, c1);
            (_cells[r0, c0], _cells[r1, c1]) = (_cells[r1, c1], _cells[r0, c0]);
        }

        public bool InBounds(int row, int col) =>
            row >= 0 && row < Rows && col >= 0 && col < Cols;

        public void Fill(TileType[,] layout)
        {
            if (layout == null) throw new ArgumentNullException(nameof(layout));
            if (layout.GetLength(0) != Rows || layout.GetLength(1) != Cols)
                throw new ArgumentException("Layout size mismatch.");
            for (int r = 0; r < Rows; r++)
            for (int c = 0; c < Cols; c++)
                _cells[r, c] = layout[r, c];
        }

        void EnsureInBounds(int row, int col)
        {
            if (!InBounds(row, col))
                throw new ArgumentOutOfRangeException($"({row},{col}) out of {Rows}x{Cols}");
        }
    }
}
