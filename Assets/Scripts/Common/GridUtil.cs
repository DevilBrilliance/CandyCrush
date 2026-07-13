using UnityEngine;

namespace CandyCrush.Common
{
    public static class GridUtil
    {
        public static Vector3 CellToLocal(int row, int col, int rows, int cols, float cellSize, Vector2 origin)
        {
            float x = origin.x + (col - (cols - 1) * 0.5f) * cellSize;
            float y = origin.y + ((rows - 1) * 0.5f - row) * cellSize;
            return new Vector3(x, y, 0f);
        }

        public static bool TryWorldToCell(Vector3 local, int rows, int cols, float cellSize, Vector2 origin,
            out int row, out int col)
        {
            float fx = (local.x - origin.x) / cellSize + (cols - 1) * 0.5f;
            float fy = (rows - 1) * 0.5f - (local.y - origin.y) / cellSize;
            col = Mathf.RoundToInt(fx);
            row = Mathf.RoundToInt(fy);
            return row >= 0 && row < rows && col >= 0 && col < cols;
        }
    }
}
