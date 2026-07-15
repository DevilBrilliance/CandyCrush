using System;
using System.Collections.Generic;
using CandyCrush.Data;

namespace CandyCrush.Core
{
    public static class GravitySystem
    {
        /// <summary>列压实：非空块下落。结果写入 moves（会 Clear）。</summary>
        public static void ApplyGravity(BoardModel board, List<FallMove> moves)
        {
            moves.Clear();
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
        }

        /// <summary>兼容旧调用：分配新列表。</summary>
        public static List<FallMove> ApplyGravity(BoardModel board)
        {
            var moves = new List<FallMove>(16);
            ApplyGravity(board, moves);
            return moves;
        }
    }

    public static class TileSpawner
    {
        static readonly TileType[] Normals =
        {
            TileType.Red, TileType.Yellow, TileType.Blue, TileType.Green
        };
        static readonly int[] _weights = { 1, 1, 1, 1 };

        /// <summary>顶行空位补普通块（永不刷行李箱）。结果写入 spawns（会 Clear）。</summary>
        public static void FillEmpties(BoardModel board, int[] spawnWeights, Random rng, List<SpawnMove> spawns)
        {
            if (rng == null) rng = new Random();
            FillWeights(spawnWeights);
            spawns.Clear();

            for (int c = 0; c < board.Cols; c++)
            {
                for (int r = 0; r < board.Rows; r++)
                {
                    if (board.Get(r, c) != TileType.Empty) continue;
                    var type = Pick(_weights, rng);
                    type = AvoidImmediateMatch(board, r, c, type, rng);
                    board.Set(r, c, type);
                    spawns.Add(new SpawnMove(new GridPos(r, c), type));
                }
            }
        }

        public static List<SpawnMove> FillEmpties(BoardModel board, int[] spawnWeights, Random rng)
        {
            var spawns = new List<SpawnMove>(16);
            FillEmpties(board, spawnWeights, rng, spawns);
            return spawns;
        }

        static void FillWeights(int[] spawnWeights)
        {
            for (int i = 0; i < 4; i++)
                _weights[i] = (spawnWeights != null && i < spawnWeights.Length && spawnWeights[i] > 0)
                    ? spawnWeights[i]
                    : 1;
        }

        static TileType AvoidImmediateMatch(BoardModel board, int r, int c, TileType type, Random rng)
        {
            for (int attempt = 0; attempt < 6; attempt++)
            {
                bool bad = false;
                if (c >= 2 && board.Get(r, c - 1) == type && board.Get(r, c - 2) == type) bad = true;
                if (r >= 2 && board.Get(r - 1, c) == type && board.Get(r - 2, c) == type) bad = true;
                if (!bad) return type;
                type = Pick(_weights, rng);
            }
            return type;
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
