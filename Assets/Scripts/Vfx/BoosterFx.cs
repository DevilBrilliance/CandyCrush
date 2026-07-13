using System.Collections;
using System.Collections.Generic;
using CandyCrush.Core;
using CandyCrush.Data;
using CandyCrush.View;
using UnityEngine;

namespace CandyCrush.Vfx
{
    /// <summary>
    /// 道具表现：火箭扫射拖尾、螺旋桨飞向目标、炸弹爆闪、彩球涟漪、生成弹出。
    /// 贴图从 Resources/Vfx/Booster 加载，缺失时用程序白点兜底。
    /// </summary>
    public class BoosterFx : MonoBehaviour
    {
        public const int SortingOrder = 120;

        [SerializeField] float rocketDuration = 0.38f;
        [SerializeField] float propellerDuration = 0.48f;
        [SerializeField] float bombDuration = 0.28f;
        [SerializeField] float colorBallDuration = 0.4f;
        [SerializeField] float spawnPopDuration = 0.28f;

        Sprite _dot;
        Sprite _smoke;
        Sprite _star;
        Sprite _glow;
        Sprite _rainbow;
        Sprite _flash;
        static Material _mat;
        static Texture2D _whiteTex;

        public static BoosterFx Ensure(Transform parent)
        {
            var existing = parent.GetComponentInChildren<BoosterFx>(true);
            if (existing != null) return existing;
            var go = new GameObject("BoosterFx");
            go.transform.SetParent(parent, false);
            return go.AddComponent<BoosterFx>();
        }

        void Awake() => LoadSprites();

        void LoadSprites()
        {
            _dot = Load("boost_other_dot") ?? Load("particle_other_dot") ?? MakeDot(64);
            _smoke = Load("efx_smoke_1") ?? Load("particle_smoke") ?? _dot;
            _star = Load("particle_die_star_1") ?? Load("particle_die_star_2") ?? _dot;
            _glow = Load("efx_candy_27") ?? Load("efx_candy_27_1") ?? _dot;
            _rainbow = Load("efx_Rainbowefx") ?? Load("efx_Rainbowefx_03") ?? _glow;
            _flash = Load("candy_12_particle") ?? _glow;
        }

        static Sprite Load(string name)
        {
            var path = "Vfx/Booster/" + name;
            var s = Resources.Load<Sprite>(path);
            if (s != null) return s;
            var tex = Resources.Load<Texture2D>(path);
            if (tex == null) return null;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }

        public IEnumerator PlayActivations(IReadOnlyList<ActivatedBooster> activations, BoardView board)
        {
            if (activations == null || activations.Count == 0 || board == null) yield break;
            if (_dot == null) LoadSprites();

            float wait = 0f;
            for (int i = 0; i < activations.Count; i++)
            {
                var a = activations[i];
                float d = a.Type switch
                {
                    TileType.RocketH => rocketDuration,
                    TileType.RocketV => rocketDuration,
                    TileType.Propeller => propellerDuration,
                    TileType.Bomb => bombDuration,
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
            if (spawns == null || spawns.Count == 0 || board == null) yield break;
            if (_dot == null) LoadSprites();

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
                    yield return RocketSweep(origin, true, board.Model.Cols, cell, board.transform);
                    break;
                case TileType.RocketV:
                    yield return RocketSweep(origin, false, board.Model.Rows, cell, board.transform);
                    break;
                case TileType.Propeller:
                    if (a.HasTarget)
                    {
                        var target = board.transform.TransformPoint(board.CellLocal(a.Target.Row, a.Target.Col));
                        yield return PropellerFly(origin, target, cell);
                    }
                    else
                        yield return BombFlash(origin, cell);
                    break;
                case TileType.Bomb:
                    yield return BombFlash(origin, cell);
                    break;
                case TileType.ColorBall:
                    yield return ColorBallPulse(origin, cell);
                    break;
            }
        }

        IEnumerator RocketSweep(Vector3 origin, bool horizontal, int count, float cell, Transform board)
        {
            float dur = Mathf.Max(0.15f, rocketDuration);
            var beam = MakeSprite("RocketBeam", _glow, origin, SortingOrder + 2);
            beam.transform.localScale = horizontal
                ? new Vector3(cell * 0.35f, cell * 0.55f, 1f)
                : new Vector3(cell * 0.55f, cell * 0.35f, 1f);
            Tint(beam, new Color(0.45f, 0.9f, 1f, 0.95f));

            // 双向扫
            float half = (count - 1) * 0.5f * cell;
            Vector3 dirA = horizontal ? Vector3.right : Vector3.up;
            Vector3 dirB = -dirA;

            var trailA = MakeTrailHead("RocketA", _smoke, origin, SortingOrder + 3);
            var trailB = MakeTrailHead("RocketB", _smoke, origin, SortingOrder + 3);
            Tint(trailA, new Color(1f, 0.85f, 0.4f, 1f));
            Tint(trailB, new Color(1f, 0.85f, 0.4f, 1f));
            trailA.transform.localScale = Vector3.one * (cell * 0.45f);
            trailB.transform.localScale = Vector3.one * (cell * 0.45f);

            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / dur);
                float ease = 1f - Mathf.Pow(1f - u, 2f);
                float dist = Mathf.Lerp(0f, half + cell, ease);

                if (trailA != null) trailA.transform.position = origin + dirA * dist;
                if (trailB != null) trailB.transform.position = origin + dirB * dist;

                if (beam != null)
                {
                    float len = dist * 2f + cell * 0.4f;
                    beam.transform.position = origin;
                    beam.transform.localScale = horizontal
                        ? new Vector3(len, cell * (0.35f + 0.2f * Mathf.Sin(u * Mathf.PI)), 1f)
                        : new Vector3(cell * (0.35f + 0.2f * Mathf.Sin(u * Mathf.PI)), len, 1f);
                    var c = beam.color;
                    c.a = 0.85f * (1f - u * 0.3f);
                    beam.color = c;
                }

                // 偶尔喷烟点
                if (Random.value < 0.45f)
                {
                    SpawnFadingParticle(origin + dirA * dist, _smoke, cell * 0.35f, new Color(1f, 0.8f, 0.35f, 0.8f), 0.25f);
                    SpawnFadingParticle(origin + dirB * dist, _smoke, cell * 0.35f, new Color(1f, 0.8f, 0.35f, 0.8f), 0.25f);
                }

                yield return null;
            }

            if (trailA != null) Destroy(trailA.gameObject);
            if (trailB != null) Destroy(trailB.gameObject);
            if (beam != null) Destroy(beam.gameObject);
        }

        IEnumerator PropellerFly(Vector3 from, Vector3 to, float cell)
        {
            float dur = Mathf.Max(0.2f, propellerDuration);
            var flyer = MakeSprite("PropellerFly", _star, from, SortingOrder + 5);
            flyer.transform.localScale = Vector3.one * (cell * 0.7f);
            Tint(flyer, Color.white);

            var glow = MakeSprite("PropellerGlow", _glow, from, SortingOrder + 4);
            glow.transform.localScale = Vector3.one * (cell * 0.9f);
            Tint(glow, new Color(1f, 0.55f, 0.95f, 0.7f));

            Vector3 mid = Vector3.Lerp(from, to, 0.45f);
            mid.y += cell * 1.1f;

            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / dur);
                float ease = u * u * (3f - 2f * u);
                var pos = QuadBezier(from, mid, to, ease);
                if (flyer != null)
                {
                    flyer.transform.position = pos;
                    flyer.transform.rotation = Quaternion.Euler(0f, 0f, u * 720f);
                    flyer.transform.localScale = Vector3.one * (cell * (0.7f + 0.15f * Mathf.Sin(u * Mathf.PI)));
                }
                if (glow != null)
                {
                    glow.transform.position = pos;
                    var c = glow.color;
                    c.a = 0.55f * (1f - u * 0.4f);
                    glow.color = c;
                }

                if (Random.value < 0.55f)
                    SpawnFadingParticle(pos, _star, cell * 0.28f, new Color(1f, 0.95f, 0.55f, 0.9f), 0.3f);

                yield return null;
            }

            SpawnFadingParticle(to, _flash, cell * 1.1f, new Color(1f, 0.9f, 0.4f, 0.95f), 0.22f);
            if (flyer != null) Destroy(flyer.gameObject);
            if (glow != null) Destroy(glow.gameObject);
        }

        IEnumerator BombFlash(Vector3 origin, float cell)
        {
            float dur = Mathf.Max(0.12f, bombDuration);
            var ring = MakeSprite("BombRing", _glow, origin, SortingOrder + 6);
            Tint(ring, new Color(1f, 0.35f, 0.2f, 0.9f));
            var core = MakeSprite("BombCore", _flash, origin, SortingOrder + 7);
            Tint(core, new Color(1f, 0.95f, 0.6f, 1f));
            core.transform.localScale = Vector3.one * (cell * 0.5f);

            for (int i = 0; i < 8; i++)
            {
                float ang = i * 45f * Mathf.Deg2Rad;
                var dir = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f);
                SpawnFadingParticle(origin + dir * cell * 0.2f, _smoke, cell * 0.4f,
                    new Color(1f, 0.45f, 0.15f, 0.85f), 0.35f, dir * cell * 2.2f);
            }

            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / dur);
                float s = Mathf.Lerp(0.4f, 2.4f, 1f - Mathf.Pow(1f - u, 2f));
                if (ring != null)
                {
                    ring.transform.localScale = Vector3.one * (cell * s);
                    var c = ring.color;
                    c.a = 0.9f * (1f - u);
                    ring.color = c;
                }
                if (core != null)
                {
                    core.transform.localScale = Vector3.one * (cell * Mathf.Lerp(0.8f, 0.1f, u));
                    var c = core.color;
                    c.a = 1f - u;
                    core.color = c;
                }
                yield return null;
            }

            if (ring != null) Destroy(ring.gameObject);
            if (core != null) Destroy(core.gameObject);
        }

        IEnumerator ColorBallPulse(Vector3 origin, float cell)
        {
            float dur = Mathf.Max(0.15f, colorBallDuration);
            var orb = MakeSprite("ColorBall", _rainbow, origin, SortingOrder + 6);
            orb.transform.localScale = Vector3.one * (cell * 0.2f);

            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / dur);
                if (orb != null)
                {
                    orb.transform.localScale = Vector3.one * (cell * Mathf.Lerp(0.3f, 2.6f, u));
                    orb.transform.rotation = Quaternion.Euler(0f, 0f, u * 180f);
                    var c = orb.color;
                    c.a = 0.95f * (1f - u * 0.7f);
                    orb.color = c;
                }
                if (Random.value < 0.4f)
                    SpawnFadingParticle(origin + (Vector3)(Random.insideUnitCircle * cell), _dot, cell * 0.25f,
                        Color.HSVToRGB(Random.value, 0.7f, 1f), 0.25f);
                yield return null;
            }

            if (orb != null) Destroy(orb.gameObject);
        }

        IEnumerator SpawnPop(Vector3 world, float cell, TileType type)
        {
            var glow = MakeSprite("SpawnGlow", _glow, world, SortingOrder + 1);
            Color tint = type switch
            {
                TileType.Bomb => new Color(1f, 0.35f, 0.25f, 0.9f),
                TileType.Propeller => new Color(1f, 0.5f, 0.9f, 0.9f),
                TileType.ColorBall => new Color(1f, 0.85f, 0.3f, 0.9f),
                _ => new Color(0.5f, 0.85f, 1f, 0.9f)
            };
            Tint(glow, tint);

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
            if (glow != null) Destroy(glow.gameObject);
        }

        void SpawnFadingParticle(Vector3 pos, Sprite sprite, float size, Color color, float life, Vector3 velocity = default)
        {
            var sr = MakeSprite("P", sprite != null ? sprite : _dot, pos, SortingOrder + 8);
            if (sr == null) return;
            sr.transform.localScale = Vector3.one * size;
            Tint(sr, color);
            StartCoroutine(FadeMove(sr, life, velocity));
        }

        IEnumerator FadeMove(SpriteRenderer sr, float life, Vector3 velocity)
        {
            float t = 0f;
            var baseCol = sr.color;
            while (t < life && sr != null)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / life);
                sr.transform.position += velocity * Time.deltaTime;
                velocity *= 0.92f;
                var c = baseCol;
                c.a = baseCol.a * (1f - u);
                sr.color = c;
                sr.transform.localScale *= 0.985f;
                yield return null;
            }
            if (sr != null) Destroy(sr.gameObject);
        }

        SpriteRenderer MakeTrailHead(string name, Sprite sprite, Vector3 pos, int order) =>
            MakeSprite(name, sprite, pos, order);

        SpriteRenderer MakeSprite(string name, Sprite sprite, Vector3 pos, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite != null ? sprite : MakeDot(32);
            sr.sortingOrder = order;
            if (_mat == null)
            {
                var sh = Shader.Find("Sprites/Default");
                if (sh != null) _mat = new Material(sh);
            }
            if (_mat != null) sr.sharedMaterial = _mat;
            return sr;
        }

        static void Tint(SpriteRenderer sr, Color c)
        {
            if (sr != null) sr.color = c;
        }

        static Vector3 QuadBezier(Vector3 a, Vector3 b, Vector3 c, float t)
        {
            float u = 1f - t;
            return u * u * a + 2f * u * t * b + t * t * c;
        }

        static Sprite MakeDot(int size)
        {
            if (_whiteTex == null)
            {
                _whiteTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                _whiteTex.filterMode = FilterMode.Bilinear;
                var px = new Color32[size * size];
                float r = size * 0.5f;
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - r;
                    float dy = y + 0.5f - r;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / r;
                    byte a = d >= 1f ? (byte)0 : (byte)Mathf.Clamp(Mathf.RoundToInt((1f - d) * (1f - d) * 255f), 0, 255);
                    px[y * size + x] = new Color32(255, 255, 255, a);
                }
                _whiteTex.SetPixels32(px);
                _whiteTex.Apply(false, true);
            }
            return Sprite.Create(_whiteTex, new Rect(0, 0, _whiteTex.width, _whiteTex.height), new Vector2(0.5f, 0.5f), size);
        }
    }
}
