using UnityEngine;

namespace CandyCrush.Data
{
    [CreateAssetMenu(menuName = "CandyCrush/Level Config", fileName = "LevelConfig")]
    public class LevelConfig : ScriptableObject
    {
        [Header("Board")]
        public int rows = 8;
        public int cols = 9;

        [Header("Objective")]
        public ObjectiveType objectiveType = ObjectiveType.CollectSuitcase;
        public int objectiveCount = 15;

        [Header("Spawn")]
        [Tooltip("普通四色相对权重：红/黄/蓝/绿")]
        public int[] spawnWeights = { 1, 1, 1, 1 };

        public bool enableColorBall = false;

        [Header("Initial Board (optional)")]
        [Tooltip("长度 = rows*cols，按行优先。0=空，1红2黄3蓝4绿10箱")]
        public int[] initialBoard;

        public TileType[,] BuildInitialLayout()
        {
            var layout = new TileType[rows, cols];
            if (initialBoard == null || initialBoard.Length != rows * cols)
                return layout;

            for (int i = 0; i < initialBoard.Length; i++)
            {
                int r = i / cols;
                int c = i % cols;
                layout[r, c] = (TileType)initialBoard[i];
            }
            return layout;
        }
    }
}
