using System.Collections;
using System.Collections.Generic;
using CandyCrush.Core;
using CandyCrush.Data;
using CandyCrush.View;
using UnityEngine;

namespace CandyCrush.Vfx
{
    /// <summary>
    /// 道具表现调度：按类型分发给 Rocket/Propeller/Bomb/ColorBall FX。
    /// 共享贴图与对象池见 <see cref="BoosterFxContext"/>。
    /// </summary>
    public class BoosterFx : MonoBehaviour
    {
        public const int SortingOrder = 920;

        [SerializeField] float rocketDuration = 0.52f;
        [SerializeField] float propellerDuration = 1.05f;
        [SerializeField] float bombDuration = 0.62f;
        [SerializeField] float colorBallDuration = 0.4f;
        [SerializeField] float spawnPopDuration = 0.28f;

        BoosterFxContext _ctx;
        RocketBoosterFx _rocket;
        PropellerBoosterFx _propeller;
        BombBoosterFx _bomb;
        ColorBallBoosterFx _colorBall;

        public static BoosterFx Ensure(Transform parent)
        {
            var existing = parent.GetComponentInChildren<BoosterFx>(true);
            if (existing != null) return existing;
            var go = new GameObject("BoosterFx");
            go.transform.SetParent(parent, false);
            return go.AddComponent<BoosterFx>();
        }

        void Awake() => Init();

        void Init()
        {
            if (_ctx != null) return;
            _ctx = new BoosterFxContext(this);
            SyncDurations();
            _ctx.LoadSprites();
            _rocket = new RocketBoosterFx(_ctx);
            _propeller = new PropellerBoosterFx(_ctx);
            _bomb = new BombBoosterFx(_ctx);
            _colorBall = new ColorBallBoosterFx(_ctx);
        }

        void SyncDurations()
        {
            _ctx.RocketDuration = rocketDuration;
            _ctx.PropellerDuration = propellerDuration;
            _ctx.BombDuration = bombDuration;
            _ctx.ColorBallDuration = colorBallDuration;
            _ctx.SpawnPopDuration = spawnPopDuration;
        }

        public IEnumerator PlayActivations(IReadOnlyList<ActivatedBooster> activations, BoardView board)
        {
            Init();
            SyncDurations();
            if (activations == null || activations.Count == 0 || board == null) yield break;
            _ctx.EnsureSpritesLoaded();

            float wait = 0f;
            for (int i = 0; i < activations.Count; i++)
            {
                var a = activations[i];
                float d = a.Type switch
                {
                    TileType.RocketH => rocketDuration,
                    TileType.RocketV => rocketDuration,
                    TileType.Propeller => propellerDuration,
                    TileType.Bomb => Mathf.Max(0.45f, bombDuration),
                    TileType.ColorBall => colorBallDuration,
                    _ => 0.2f
                };
                StartCoroutine(PlayOne(a, board));
                wait = Mathf.Max(wait, d);
            }

            float t = 0f;
            while (t < wait)
            {
                t += Time.deltaTime;
                yield return null;
            }
        }

        public IEnumerator PlaySpawnPops(IReadOnlyList<(GridPos at, TileType booster)> spawns, BoardView board)
        {
            Init();
            SyncDurations();
            if (spawns == null || spawns.Count == 0 || board == null) yield break;
            _ctx.EnsureSpritesLoaded();

            for (int i = 0; i < spawns.Count; i++)
            {
                var (at, type) = spawns[i];
                var world = board.transform.TransformPoint(board.CellLocal(at.Row, at.Col));
                StartCoroutine(SpawnPop(world, board.CellSizeSafe(), type));
            }

            float t = 0f;
            float dur = spawnPopDuration;
            while (t < dur)
            {
                t += Time.deltaTime;
                yield return null;
            }
        }

        IEnumerator PlayOne(ActivatedBooster a, BoardView board)
        {
            float cell = board.CellSizeSafe();
            var origin = board.transform.TransformPoint(board.CellLocal(a.Origin.Row, a.Origin.Col));

            switch (a.Type)
            {
                case TileType.RocketH:
                    yield return _rocket.PlaySweep(a.Origin, true, board);
                    break;
                case TileType.RocketV:
                    yield return _rocket.PlaySweep(a.Origin, false, board);
                    break;
                case TileType.Propeller:
                    if (a.HasTarget)
                    {
                        var target = board.transform.TransformPoint(board.CellLocal(a.Target.Row, a.Target.Col));
                        yield return _propeller.PlayCrossThenChase(origin, target, cell);
                    }
                    else
                        yield return _propeller.PlayCrossBurst(origin, cell);
                    break;
                case TileType.Bomb:
                    yield return _bomb.PlayFlash(origin, cell);
                    break;
                case TileType.ColorBall:
                    yield return _colorBall.PlayPulse(origin, cell);
                    break;
            }
        }

        IEnumerator SpawnPop(Vector3 world, float cell, TileType type)
        {
            var glow = _ctx.MakeSprite("SpawnGlow", _ctx.Glow, world, SortingOrder + 1);
            Color tint = type switch
            {
                TileType.Bomb => new Color(1f, 0.35f, 0.25f, 0.9f),
                TileType.Propeller => new Color(1f, 0.5f, 0.9f, 0.9f),
                TileType.ColorBall => new Color(1f, 0.85f, 0.3f, 0.9f),
                _ => new Color(0.5f, 0.85f, 1f, 0.9f)
            };
            BoosterFxContext.Tint(glow, tint);

            float t = 0f;
            float dur = spawnPopDuration;
            while (t < dur)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / dur);
                float s = 1f + Mathf.Sin(u * Mathf.PI) * 1.2f;
                if (glow != null)
                {
                    glow.transform.localScale = Vector3.one * (cell * s);
                    var c = glow.color;
                    c.a = tint.a * (1f - u);
                    glow.color = c;
                }
                yield return null;
            }
            _ctx.Release(glow);
        }
    }
}
