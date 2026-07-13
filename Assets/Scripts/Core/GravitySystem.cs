using System;
using System.Collections.Generic;
using CandyCrush.Data;

namespace CandyCrush.Core
{
    public static class GravitySystem
    {
        /// <summary>列压实：非空块下落。返回下落指令（不含写盘，调用方先应用再播动画，或本方法直接写盘）。</summary>
        public static List<FallMove> ApplyGravity(BoardModel board)
        {
            var moves = new List<FallMove>();
            for (int c = 0; c < board.Cols; c++)
            {
                int write = board.Rows - 1;
                for (int r = board.Rows - 1; r >= 0; r--)
                {
                    var t = board.Get(r, c);
                    if (t == TileType.Empty) continue;
                    if (r != write)
                    {
                        moves.Add(new FallMove(new GridPos(r, c), new GridPos(write, c), t));
                        board.Set(write, c, t);
                        board.Set(r, c, TileType.Empty);
                    }
                    write--;
                }
            }
            return moves;
        }
    }

    public static class TileSpawner
    {
        static readonly TileType[] Normals =
        {
            TileType.Red, TileType.Yellow, TileType.Blue, TileType.Green
        };

        /// <summary>顶行空位补普通块（永不刷行李箱）。</summary>
        public static List<SpawnMove> FillEmpties(BoardModel board, int[] spawnWeights, Random rng)
        {
            if (rng == null) rng = new Random();
            var weights = NormalizeWeights(spawnWeights);
            var spawns = new List<SpawnMove>();

            for (int c = 0; c < board.Cols; c++)
            {
                for (int r = 0; r < board.Rows; r++)
                {
                    if (board.Get(r, c) != TileType.Empty) continue;
                    var type = Pick(weights, rng);
                    // 简单避免生成即三连
                    type = AvoidImmediateMatch(board, r, c, type, weights, rng);
                    board.Set(r, c, type);
                    spawns.Add(new SpawnMove(new GridPos(r, c), type));
                }
            }
            return spawns;
        }

        static TileType AvoidImmediateMatch(BoardModel board, int r, int c, TileType type, int[] weights, Random rng)
        {
            for (int attempt = 0; attempt < 6; attempt++)
            {
                bool bad = false;
                if (c >= 2 && board.Get(r, c - 1) == type && board.Get(r, c - 2) == type) bad = true;
                if (r >= 2 && board.Get(r - 1, c) == type && board.Get(r - 2, c) == type) bad = true;
                if (!bad) return type;
                type = Pick(weights, rng);
            }
            return type;
        }

        static int[] NormalizeWeights(int[] spawnWeights)
        {
            var w = new int[4];
            for (int i = 0; i < 4; i++)
                w[i] = (spawnWeights != null && i < spawnWeights.Length && spawnWeights[i] > 0) ? spawnWeights[i] : 1;
            return w;
        }

        static TileType Pick(int[] weights, Random rng)
        {
            int sum = 0;
            for (int i = 0; i < weights.Length; i++) sum += weights[i];
            int roll = rng.Next(sum);
            for (int i = 0; i < weights.Length; i++)
            {
                roll -= weights[i];
                if (roll < 0) return Normals[i];
            }
            return Normals[0];
        }
    }
}
