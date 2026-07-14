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

        [SerializeField] float rocketDuration = 0.52f;
        [SerializeField] float propellerDuration = 0.48f;
        [SerializeField] float bombDuration = 0.62f;
        [SerializeField] float colorBallDuration = 0.4f;
        [SerializeField] float spawnPopDuration = 0.28f;

        Sprite _dot;
        Sprite _smoke;
        Sprite _star;
        Sprite _star2;
        Sprite _glow;
        Sprite _rainbow;
        Sprite _flash;
        Sprite _starlight;
        Sprite _bombShard;
        Sprite _arrowUp;
        Sprite _arrowDown;
        static Material _mat;
        static Material _additiveMat;
        static Texture2D _whiteTex;
        static Texture2D _ringTex;
        static Sprite _ringSprite;

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
            _star2 = Load("particle_die_star_2") ?? _star;
            _glow = Load("efx_candy_27") ?? _dot;
            _rainbow = Load("efx_Rainbowefx") ?? Load("efx_Rainbowefx_03") ?? _glow;
            _flash = Load("candy_12_particle") ?? _glow;
            _starlight = Load("UIpanel_starlight") ?? _star;
            _bombShard = Load("particle_die_candy_27") ?? _star;
            _arrowUp = Load("efx_arrow_2") ?? _star;
            _arrowDown = Load("efx_arrow_1") ?? _arrowUp;
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
            float dur = Mathf.Max(0.28f, rocketDuration);
            float half = Mathf.Max(cell, (count - 1) * 0.5f * cell);
            Vector3 dirA = horizontal ? Vector3.right : Vector3.up;
            Vector3 dirB = -dirA;

            // 起爆闪光
            SpawnFadingParticle(origin, _flash, cell * 1.6f, new Color(1f, 0.95f, 0.55f, 1f), 0.28f, additive: true);
            SpawnFadingParticle(origin, _starlight, cell * 2.2f, new Color(1f, 0.85f, 0.35f, 1f), 0.32f, additive: true);

            // 粗光带（底）+ 亮芯（上）
            var beamSoft = MakeSprite("RocketBeamSoft", _glow, origin, SortingOrder + 10, additive: true);
            Tint(beamSoft, new Color(1f, 0.75f, 0.25f, 0.95f));
            var beamCore = MakeSprite("RocketBeamCore", _flash, origin, SortingOrder + 11, additive: true);
            Tint(beamCore, new Color(1f, 0.98f, 0.85f, 1f));

            // 双向箭头头
            var headA = MakeSprite("RocketHeadA", horizontal || dirA.y > 0 ? _arrowUp : _arrowDown, origin, SortingOrder + 13);
            var headB = MakeSprite("RocketHeadB", _arrowDown, origin, SortingOrder + 13);
            if (horizontal)
            {
                if (headA != null) headA.transform.rotation = Quaternion.Euler(0f, 0f, -90f); // 右
                if (headB != null)
                {
                    headB.sprite = _arrowUp;
                    headB.transform.rotation = Quaternion.Euler(0f, 0f, 90f); // 左
                }
            }
            else
            {
                if (headA != null) { headA.sprite = _arrowUp; headA.transform.rotation = Quaternion.identity; }
                if (headB != null) { headB.sprite = _arrowDown; headB.transform.rotation = Quaternion.identity; }
            }
            float headScale = cell * 0.95f;
            if (headA != null) { Tint(headA, Color.white); headA.transform.localScale = Vector3.one * headScale; }
            if (headB != null) { Tint(headB, Color.white); headB.transform.localScale = Vector3.one * headScale; }

            // 箭头拖尾光晕
            var glowA = MakeSprite("RocketGlowA", _glow, origin, SortingOrder + 12, additive: true);
            var glowB = MakeSprite("RocketGlowB", _glow, origin, SortingOrder + 12, additive: true);
            Tint(glowA, new Color(1f, 0.7f, 0.2f, 1f));
            Tint(glowB, new Color(1f, 0.7f, 0.2f, 1f));
            if (glowA != null) glowA.transform.localScale = Vector3.one * (cell * 1.1f);
            if (glowB != null) glowB.transform.localScale = Vector3.one * (cell * 1.1f);

            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / dur);
                float ease = 1f - Mathf.Pow(1f - u, 2.4f);
                float dist = Mathf.Lerp(0f, half + cell * 0.35f, ease);
                float pulse = 0.55f + 0.45f * Mathf.Sin(u * Mathf.PI);

                Vector3 posA = origin + dirA * dist;
                Vector3 posB = origin + dirB * dist;
                if (headA != null) headA.transform.position = posA;
                if (headB != null) headB.transform.position = posB;
                if (glowA != null)
                {
                    glowA.transform.position = posA;
                    glowA.transform.localScale = Vector3.one * (cell * (1.0f + 0.35f * pulse));
                }
                if (glowB != null)
                {
                    glowB.transform.position = posB;
                    glowB.transform.localScale = Vector3.one * (cell * (1.0f + 0.35f * pulse));
                }

                float len = dist * 2f + cell * 0.85f;
                float thickSoft = cell * (0.85f + 0.35f * pulse);
                float thickCore = cell * (0.38f + 0.18f * pulse);
                if (beamSoft != null)
                {
                    beamSoft.transform.position = origin;
                    beamSoft.transform.localScale = horizontal
                        ? new Vector3(len, thickSoft, 1f)
                        : new Vector3(thickSoft, len, 1f);
                    var c = beamSoft.color;
                    c.a = 0.95f * (1f - u * 0.25f);
                    beamSoft.color = c;
                }
                if (beamCore != null)
                {
                    beamCore.transform.position = origin;
                    beamCore.transform.localScale = horizontal
                        ? new Vector3(len, thickCore, 1f)
                        : new Vector3(thickCore, len, 1f);
                    var c = beamCore.color;
                    c.a = 1f * (1f - u * 0.35f);
                    beamCore.color = c;
                }

                // 沿途密集火花 + 烟雾
                if (Random.value < 0.85f)
                {
                    SpawnFadingParticle(posA, _star, cell * Random.Range(0.28f, 0.5f),
                        new Color(1f, 0.95f, 0.45f, 1f), 0.28f, dirA * cell * 2.2f, additive: true);
                    SpawnFadingParticle(posB, _star, cell * Random.Range(0.28f, 0.5f),
                        new Color(1f, 0.95f, 0.45f, 1f), 0.28f, dirB * cell * 2.2f, additive: true);
                }
                if (Random.value < 0.55f)
                {
                    SpawnFadingParticle(posA, _smoke, cell * 0.55f,
                        new Color(1f, 0.65f, 0.25f, 0.85f), 0.32f, additive: true);
                    SpawnFadingParticle(posB, _smoke, cell * 0.55f,
                        new Color(1f, 0.65f, 0.25f, 0.85f), 0.32f, additive: true);
                }

                yield return null;
            }

            // 收尾整条闪一下
            SpawnFadingParticle(origin, _flash,
                horizontal ? cell * count * 0.35f : cell * 1.4f,
                new Color(1f, 0.95f, 0.6f, 1f), 0.22f, additive: true);

            if (headA != null) Destroy(headA.gameObject);
            if (headB != null) Destroy(headB.gameObject);
            if (glowA != null) Destroy(glowA.gameObject);
            if (glowB != null) Destroy(glowB.gameObject);
            if (beamSoft != null) Destroy(beamSoft.gameObject);
            if (beamCore != null) Destroy(beamCore.gameObject);
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
            // 对齐架构/视频：炽白爆闪 + 扩散热圈 + 星光甩射（覆盖约 5×5）
            float dur = Mathf.Max(0.45f, bombDuration);
            float reach = cell * 2.6f; // 半边约 2.5 格

            var flash = MakeSprite("BombStarFlash", _starlight, origin, SortingOrder + 12, additive: true);
            Tint(flash, new Color(1f, 0.95f, 0.75f, 1f));
            flash.transform.localScale = Vector3.one * (cell * 0.2f);

            var core = MakeSprite("BombCore", _flash, origin, SortingOrder + 11, additive: true);
            Tint(core, new Color(1f, 0.9f, 0.55f, 1f));
            core.transform.localScale = Vector3.one * (cell * 0.4f);

            var ring = MakeSprite("BombRing", GetShockRing(), origin, SortingOrder + 9, additive: true);
            Tint(ring, new Color(1f, 0.55f, 0.2f, 0.95f));
            ring.transform.localScale = Vector3.one * (cell * 0.5f);

            var heat = MakeSprite("BombHeat", _glow, origin, SortingOrder + 8, additive: true);
            Tint(heat, new Color(1f, 0.35f, 0.12f, 0.85f));
            heat.transform.localScale = Vector3.one * (cell * 0.8f);

            // 星光 / 碎片放射
            int burst = 16;
            for (int i = 0; i < burst; i++)
            {
                float ang = (i / (float)burst) * Mathf.PI * 2f + Random.Range(-0.08f, 0.08f);
                var dir = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f);
                var spr = (i % 3 == 0) ? _bombShard : ((i & 1) == 0 ? _star : _star2);
                Color col = (i % 3 == 0)
                    ? new Color(1f, 0.45f, 0.85f, 1f)
                    : new Color(1f, 0.9f, 0.45f, 1f);
                float speed = reach * Random.Range(2.4f, 4.2f);
                SpawnFadingParticle(origin + dir * cell * 0.15f, spr,
                    cell * Random.Range(0.35f, 0.7f), col, Random.Range(0.35f, 0.55f), dir * speed, additive: true);
            }

            // 烟雾团
            for (int i = 0; i < 10; i++)
            {
                float ang = Random.Range(0f, Mathf.PI * 2f);
                var dir = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f);
                SpawnFadingParticle(origin + dir * cell * Random.Range(0.1f, 0.6f), _smoke,
                    cell * Random.Range(0.55f, 1.1f),
                    new Color(1f, 0.55f, 0.25f, 0.75f),
                    Random.Range(0.4f, 0.65f),
                    dir * reach * Random.Range(0.8f, 1.6f),
                    additive: true);
            }

            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / dur);
                float ease = 1f - Mathf.Pow(1f - u, 2.2f);

                if (flash != null)
                {
                    float fs = Mathf.Lerp(0.4f, 4.2f, Mathf.Sin(Mathf.Clamp01(u * 1.4f) * Mathf.PI));
                    flash.transform.localScale = Vector3.one * (cell * fs);
                    flash.transform.rotation = Quaternion.Euler(0f, 0f, u * 55f);
                    var c = flash.color;
                    c.a = u < 0.35f ? 1f : Mathf.Lerp(1f, 0f, (u - 0.35f) / 0.65f);
                    flash.color = c;
                }

                if (core != null)
                {
                    core.transform.localScale = Vector3.one * (cell * Mathf.Lerp(0.6f, 2.2f, ease));
                    var c = core.color;
                    c.a = 1f - u;
                    core.color = c;
                }

                if (ring != null)
                {
                    // 扩到约 5 格直径
                    ring.transform.localScale = Vector3.one * (cell * Mathf.Lerp(0.8f, 5.4f, ease));
                    var c = ring.color;
                    c.a = 0.95f * (1f - u) * (1f - u);
                    ring.color = c;
                }

                if (heat != null)
                {
                    heat.transform.localScale = Vector3.one * (cell * Mathf.Lerp(1.0f, 5.2f, ease));
                    var c = heat.color;
                    c.a = 0.7f * (1f - u);
                    heat.color = c;
                }

                // mid burst stars
                if (u > 0.12f && u < 0.55f && Random.value < 0.55f)
                {
                    float ang = Random.Range(0f, Mathf.PI * 2f);
                    var dir = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f);
                    SpawnFadingParticle(origin + dir * cell * Random.Range(0.4f, 1.5f), _star,
                        cell * Random.Range(0.2f, 0.4f),
                        new Color(1f, 0.95f, 0.6f, 0.95f),
                        0.28f, dir * reach * 1.8f, additive: true);
                }

                yield return null;
            }

            if (flash != null) Destroy(flash.gameObject);
            if (core != null) Destroy(core.gameObject);
            if (ring != null) Destroy(ring.gameObject);
            if (heat != null) Destroy(heat.gameObject);
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

        void SpawnFadingParticle(Vector3 pos, Sprite sprite, float size, Color color, float life,
            Vector3 velocity = default, bool additive = false)
        {
            var sr = MakeSprite("P", sprite != null ? sprite : _dot, pos, SortingOrder + 8, additive);
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
                velocity *= 0.9f;
                var c = baseCol;
                c.a = baseCol.a * (1f - u);
                sr.color = c;
                sr.transform.localScale *= 0.978f;
                yield return null;
            }
            if (sr != null) Destroy(sr.gameObject);
        }

        SpriteRenderer MakeTrailHead(string name, Sprite sprite, Vector3 pos, int order) =>
            MakeSprite(name, sprite, pos, order, additive: true);

        SpriteRenderer MakeSprite(string name, Sprite sprite, Vector3 pos, int order, bool additive = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite != null ? sprite : MakeDot(32);
            sr.sortingOrder = order;
            sr.sharedMaterial = additive ? GetAdditiveMat() : GetDefaultMat();
            return sr;
        }

        static Material GetDefaultMat()
        {
            if (_mat != null) return _mat;
            var sh = Shader.Find("Sprites/Default") ?? Shader.Find("UI/Default");
            if (sh != null) _mat = new Material(sh) { hideFlags = HideFlags.HideAndDontSave };
            return _mat;
        }

        static Material GetAdditiveMat()
        {
            if (_additiveMat != null) return _additiveMat;
            var sh = Shader.Find("Legacy Shaders/Particles/Additive")
                     ?? Shader.Find("Particles/Additive")
                     ?? Shader.Find("Mobile/Particles/Additive")
                     ?? Shader.Find("Sprites/Default");
            _additiveMat = new Material(sh) { hideFlags = HideFlags.HideAndDontSave, name = "BoosterFxAdditive" };
            if (_additiveMat.HasProperty("_TintColor"))
                _additiveMat.SetColor("_TintColor", new Color(1f, 1f, 1f, 0.6f));
            return _additiveMat;
        }

        static Sprite GetShockRing()
        {
            if (_ringSprite != null) return _ringSprite;

            const int size = 64;
            _ringTex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            float mid = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x - mid) / mid;
                float dy = (y - mid) / mid;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(1f - Mathf.Abs(d - 0.72f) / 0.22f);
                a = a * a;
                _ringTex.SetPixel(x, y, new Color(a, a, a, 1f));
            }
            _ringTex.Apply(false, true);
            _ringSprite = Sprite.Create(_ringTex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return _ringSprite;
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
