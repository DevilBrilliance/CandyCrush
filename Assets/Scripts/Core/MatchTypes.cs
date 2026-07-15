using System;
using System.Collections.Generic;
using CandyCrush.Data;

namespace CandyCrush.Core
{
    public readonly struct GridPos : IEquatable<GridPos>
    {
        public readonly int Row;
        public readonly int Col;

        public GridPos(int row, int col) { Row = row; Col = col; }

        public bool Equals(GridPos other) => Row == other.Row && Col == other.Col;
        public override bool Equals(object obj) => obj is GridPos p && Equals(p);
        public override int GetHashCode() => (Row * 397) ^ Col;
        public override string ToString() => $"({Row},{Col})";
    }

    public sealed class MatchGroup
    {
        public readonly List<GridPos> Cells = new List<GridPos>();
        public BoosterType SpawnBooster = BoosterType.None;
        public GridPos SpawnAt;
    }

    public readonly struct FallMove
    {
        public readonly GridPos From;
        public readonly GridPos To;
        public readonly TileType Type;

        public FallMove(GridPos from, GridPos to, TileType type)
        {
            From = from; To = to; Type = type;
        }
    }

    public readonly struct SpawnMove
    {
        public readonly GridPos To;
        public readonly TileType Type;

        public SpawnMove(GridPos to, TileType type) { To = to; Type = type; }
    }

    public sealed class CascadeStepResult
    {
        public readonly List<GridPos> Cleared = new List<GridPos>(32);
        /// <summary>与 Cleared 一一对应的被消棋子类型（用于碎裂特效）。</summary>
        public readonly List<TileType> ClearedTypes = new List<TileType>(32);
        public readonly List<GridPos> CollectedSuitcases = new List<GridPos>(16);
        public readonly List<(GridPos at, TileType booster)> SpawnedBoosters = new List<(GridPos, TileType)>(8);
        /// <summary>本步激活的道具（表现层播 BoosterFx）。</summary>
        public readonly List<ActivatedBooster> ActivatedBoosters = new List<ActivatedBooster>(8);
        public readonly List<FallMove> Falls = new List<FallMove>(32);
        public readonly List<SpawnMove> Spawns = new List<SpawnMove>(32);
        public bool HadWork;

        public void Reset()
        {
            Cleared.Clear();
            ClearedTypes.Clear();
            CollectedSuitcases.Clear();
            SpawnedBoosters.Clear();
            ActivatedBoosters.Clear();
            Falls.Clear();
            Spawns.Clear();
            HadWork = false;
        }
    }

    public struct ActivatedBooster
    {
        public TileType Type;
        public GridPos Origin;
        public GridPos Target;
        public bool HasTarget;
    }
}
