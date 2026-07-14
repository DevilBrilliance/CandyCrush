using System;
using System.Collections.Generic;
using CandyCrush.Data;

namespace CandyCrush.Core
{
    /// <summary>
    /// 同步结算一回合连锁中的「一步」：匹配/道具清 → 生成道具 → 重力 → 补块。
    /// 表现层按 CascadeStepResult 播动画后再请求下一步。
    /// </summary>
    public sealed class CascadeResolver
    {
        public const int MaxRounds = 64;

        readonly int[] _spawnWeights;
        readonly bool _enableColorBall;
        readonly Random _rng;
        readonly ObjectiveTracker _objective;
        public int Round { get; private set; }

        public CascadeResolver(LevelConfig config, ObjectiveTracker objective)
        {
            _objective = objective;
            _spawnWeights = config.spawnWeights;
            _enableColorBall = config.enableColorBall;
            _rng = new Random();
        }

        /// <summary>每次玩家操作开始时重置，避免整关累计触顶后无法再消。</summary>
        public void BeginResolve() => Round = 0;

        /// <summary>开局或交换后：处理普通匹配连锁的一步。若无匹配返回 HadWork=false。</summary>
        public CascadeStepResult StepMatches(BoardModel board)
        {
            var result = new CascadeStepResult();
            if (Round >= MaxRounds) return result;

            var matches = MatchFinder.FindMatches(board, _enableColorBall);
            if (matches.Count == 0) return result;

            Round++;
            var clearSet = new List<GridPos>();
            var boosterSpawns = new List<(GridPos at, TileType type)>();

            foreach (var g in matches)
            {
                foreach (var p in g.Cells) clearSet.Add(p);
                if (g.SpawnBooster != BoosterType.None)
                {
                    var tile = BoosterUtil.ToTile(g.SpawnBooster);
                    if (tile != TileType.Empty)
                        boosterSpawns.Add((g.SpawnAt, tile));
                }
            }

            ClearResolver.ExpandWithAdjacentSuitcases(board, clearSet);
            ApplyClear(board, clearSet, result);

            // 道具生成在被清空的格子上
            foreach (var (at, type) in boosterSpawns)
            {
                if (!board.InBounds(at.Row, at.Col)) continue;
                board.Set(at.Row, at.Col, type);
                result.SpawnedBoosters.Add((at, type));
                // 若该格在 cleared 里，表现层会先消再生成
            }

            ApplyGravityAndSpawn(board, result);
            result.HadWork = true;
            return result;
        }

        /// <summary>激活单个道具（交换触发）。</summary>
        public CascadeStepResult StepBooster(BoardModel board, int row, int col, TileType partnerColor)
        {
            var result = new CascadeStepResult();
            if (Round >= MaxRounds) return result;
            if (!board.InBounds(row, col)) return result;

            var booster = board.Get(row, col);
            if (!TileTypeUtil.IsBooster(booster)) return result;

            Round++;
            var clearSet = BoosterExecutor.GetClearCells(board, row, col, booster, partnerColor);
            RecordActivation(result, board, row, col, booster);
            ExpandNestedBoosters(board, clearSet, partnerColor, result);

            ApplyClear(board, clearSet, result);
            ApplyGravityAndSpawn(board, result);
            result.HadWork = true;
            return result;
        }

        /// <summary>交换后入口：若任一侧是道具则激活，否则走匹配。</summary>
        public CascadeStepResult StepAfterSwap(BoardModel board, int r0, int c0, int r1, int c1)
        {
            var a = board.Get(r0, c0);
            var b = board.Get(r1, c1);

            if (TileTypeUtil.IsBooster(a))
                return StepBooster(board, r0, c0, b);
            if (TileTypeUtil.IsBooster(b))
                return StepBooster(board, r1, c1, a);

            return StepMatches(board);
        }

        void ExpandNestedBoosters(BoardModel board, List<GridPos> clearSet, TileType partnerColor, CascadeStepResult result)
        {
            var seen = new HashSet<GridPos>(clearSet);
            var queue = new Queue<GridPos>(clearSet);
            var recorded = new HashSet<GridPos>();
            for (int i = 0; i < result.ActivatedBoosters.Count; i++)
                recorded.Add(result.ActivatedBoosters[i].Origin);

            while (queue.Count > 0)
            {
                var p = queue.Dequeue();
                var t = board.Get(p.Row, p.Col);
                if (!TileTypeUtil.IsBooster(t)) continue;

                if (recorded.Add(p))
                {
                    var preview = BoosterExecutor.GetClearCells(board, p.Row, p.Col, t, partnerColor);
                    RecordActivation(result, board, p.Row, p.Col, t);
                }

                var extra = BoosterExecutor.GetClearCells(board, p.Row, p.Col, t, partnerColor);
                foreach (var e in extra)
                {
                    if (seen.Add(e))
                    {
                        clearSet.Add(e);
                        queue.Enqueue(e);
                    }
                }
            }
        }

        static void RecordActivation(CascadeStepResult result, BoardModel board, int row, int col, TileType booster)
        {
            var act = new ActivatedBooster
            {
                Type = booster,
                Origin = new GridPos(row, col),
                HasTarget = false
            };

            if (booster == TileType.Propeller)
            {
                var target = BoosterExecutor.FindPropellerTarget(board, row, col);
                if (target.HasValue)
                {
                    act.Target = target.Value;
                    act.HasTarget = true;
                }
            }

            result.ActivatedBoosters.Add(act);
        }

        void ApplyClear(BoardModel board, List<GridPos> clearSet, CascadeStepResult result)
        {
            ClearResolver.Resolve(board, clearSet, _objective, out var cleared, out var clearedTypes, out var collected);
            result.Cleared.AddRange(cleared);
            result.ClearedTypes.AddRange(clearedTypes);
            result.CollectedSuitcases.AddRange(collected);
        }

        void ApplyGravityAndSpawn(BoardModel board, CascadeStepResult result)
        {
            result.Falls.AddRange(GravitySystem.ApplyGravity(board));
            result.Spawns.AddRange(TileSpawner.FillEmpties(board, _spawnWeights, _rng));
        }
    }
}
