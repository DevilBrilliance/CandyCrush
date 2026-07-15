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
        readonly CascadeStepResult _step = new CascadeStepResult();
        readonly List<GridPos> _clearSet = new List<GridPos>(64);
        readonly List<(GridPos at, TileType type)> _boosterSpawns = new List<(GridPos, TileType)>(8);
        readonly HashSet<GridPos> _claimedPropTargets = new HashSet<GridPos>();
        readonly HashSet<GridPos> _seen = new HashSet<GridPos>();
        readonly HashSet<GridPos> _recorded = new HashSet<GridPos>();
        readonly Queue<GridPos> _queue = new Queue<GridPos>(64);
        readonly List<GridPos> _scratchCells = new List<GridPos>(64);

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
            _step.Reset();
            if (Round >= MaxRounds) return _step;

            var matches = MatchFinder.FindMatches(board, _enableColorBall);
            if (matches.Count == 0) return _step;

            Round++;
            _clearSet.Clear();
            _boosterSpawns.Clear();

            for (int mi = 0; mi < matches.Count; mi++)
            {
                var g = matches[mi];
                for (int i = 0; i < g.Cells.Count; i++)
                    _clearSet.Add(g.Cells[i]);
                if (g.SpawnBooster != BoosterType.None)
                {
                    var tile = BoosterUtil.ToTile(g.SpawnBooster);
                    if (tile != TileType.Empty)
                        _boosterSpawns.Add((g.SpawnAt, tile));
                }
            }

            ClearResolver.ExpandWithAdjacentSuitcases(board, _clearSet);
            ApplyClear(board);

            for (int i = 0; i < _boosterSpawns.Count; i++)
            {
                var (at, type) = _boosterSpawns[i];
                if (!board.InBounds(at.Row, at.Col)) continue;
                board.Set(at.Row, at.Col, type);
                _step.SpawnedBoosters.Add((at, type));
            }

            ApplyGravityAndSpawn(board);
            _step.HadWork = true;
            return _step;
        }

        /// <summary>激活单个道具（交换触发）。</summary>
        public CascadeStepResult StepBooster(BoardModel board, int row, int col, TileType partnerColor)
        {
            _step.Reset();
            if (Round >= MaxRounds) return _step;
            if (!board.InBounds(row, col)) return _step;

            var booster = board.Get(row, col);
            if (!TileTypeUtil.IsBooster(booster)) return _step;

            Round++;
            _claimedPropTargets.Clear();
            GridPos? propTarget = null;
            if (booster == TileType.Propeller)
            {
                propTarget = BoosterExecutor.FindPropellerTarget(board, row, col, _claimedPropTargets);
                if (propTarget.HasValue) _claimedPropTargets.Add(propTarget.Value);
            }

            BoosterExecutor.GetClearCells(board, row, col, booster, partnerColor, _clearSet, propTarget);
            RecordActivation(booster, row, col, propTarget);
            ExpandNestedBoosters(board, partnerColor);

            ApplyClear(board);
            ApplyGravityAndSpawn(board);
            _step.HadWork = true;
            return _step;
        }

        /// <summary>交换后入口：若任一侧是道具则激活，否则走匹配。</summary>
        public CascadeStepResult StepAfterSwap(BoardModel board, int r0, int c0, int r1, int c1)
        {
            var a = board.Get(r0, c0);
            var b = board.Get(r1, c1);

            if (a == TileType.Propeller && b == TileType.Propeller)
                return StepDualPropellers(board, r0, c0, r1, c1);

            if (TileTypeUtil.IsBooster(a))
                return StepBooster(board, r0, c0, b);
            if (TileTypeUtil.IsBooster(b))
                return StepBooster(board, r1, c1, a);

            return StepMatches(board);
        }

        CascadeStepResult StepDualPropellers(BoardModel board, int r0, int c0, int r1, int c1)
        {
            _step.Reset();
            if (Round >= MaxRounds) return _step;

            Round++;
            _claimedPropTargets.Clear();
            _clearSet.Clear();

            ActivateOnePropeller(board, r0, c0);
            ActivateOnePropeller(board, r1, c1);

            ExpandNestedBoosters(board, TileType.Empty);
            ApplyClear(board);
            ApplyGravityAndSpawn(board);
            _step.HadWork = true;
            return _step;
        }

        void ActivateOnePropeller(BoardModel board, int row, int col)
        {
            for (int i = 0; i < _clearSet.Count; i++)
                _claimedPropTargets.Add(_clearSet[i]);

            var target = BoosterExecutor.FindPropellerTarget(board, row, col, _claimedPropTargets);
            if (target.HasValue) _claimedPropTargets.Add(target.Value);

            BoosterExecutor.GetClearCells(board, row, col, TileType.Propeller, TileType.Empty, _scratchCells, target);
            for (int i = 0; i < _scratchCells.Count; i++)
            {
                var p = _scratchCells[i];
                _claimedPropTargets.Add(p);
                bool exists = false;
                for (int j = 0; j < _clearSet.Count; j++)
                    if (_clearSet[j].Equals(p)) { exists = true; break; }
                if (!exists) _clearSet.Add(p);
            }
            RecordActivation(TileType.Propeller, row, col, target);
        }

        void ExpandNestedBoosters(BoardModel board, TileType partnerColor)
        {
            _seen.Clear();
            _recorded.Clear();
            _queue.Clear();
            for (int i = 0; i < _clearSet.Count; i++)
            {
                _seen.Add(_clearSet[i]);
                _queue.Enqueue(_clearSet[i]);
            }
            for (int i = 0; i < _step.ActivatedBoosters.Count; i++)
                _recorded.Add(_step.ActivatedBoosters[i].Origin);

            while (_queue.Count > 0)
            {
                var p = _queue.Dequeue();
                var t = board.Get(p.Row, p.Col);
                if (!TileTypeUtil.IsBooster(t)) continue;
                if (!_recorded.Add(p)) continue;

                GridPos? propTarget = null;
                if (t == TileType.Propeller)
                {
                    foreach (var s in _seen)
                        _claimedPropTargets.Add(s);
                    propTarget = BoosterExecutor.FindPropellerTarget(board, p.Row, p.Col, _claimedPropTargets);
                    if (propTarget.HasValue) _claimedPropTargets.Add(propTarget.Value);
                }

                RecordActivation(t, p.Row, p.Col, propTarget);
                BoosterExecutor.GetClearCells(board, p.Row, p.Col, t, partnerColor, _scratchCells, propTarget);
                for (int i = 0; i < _scratchCells.Count; i++)
                {
                    var e = _scratchCells[i];
                    if (_seen.Add(e))
                    {
                        _clearSet.Add(e);
                        _queue.Enqueue(e);
                    }
                }
            }
        }

        void RecordActivation(TileType booster, int row, int col, GridPos? propTarget)
        {
            var act = new ActivatedBooster
            {
                Type = booster,
                Origin = new GridPos(row, col),
                HasTarget = false
            };

            if (booster == TileType.Propeller && propTarget.HasValue)
            {
                act.Target = propTarget.Value;
                act.HasTarget = true;
            }

            _step.ActivatedBoosters.Add(act);
        }

        void ApplyClear(BoardModel board)
        {
            ClearResolver.Resolve(board, _clearSet, _objective, _step.Cleared, _step.ClearedTypes, _step.CollectedSuitcases);
        }

        void ApplyGravityAndSpawn(BoardModel board)
        {
            GravitySystem.ApplyGravity(board, _step.Falls);
            TileSpawner.FillEmpties(board, _spawnWeights, _rng, _step.Spawns);
        }
    }
}
