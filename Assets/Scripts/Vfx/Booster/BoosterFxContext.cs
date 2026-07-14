using System.Collections;
using CandyCrush.Common;
using UnityEngine;

namespace CandyCrush.Vfx
{
    /// <summary>道具 FX 共享：贴图、对象池、粒子爆发与常用绘制工具。</summary>
    public sealed class BoosterFxContext
    {
        public const int SortingOrder = BoosterFx.SortingOrder;

        public MonoBehaviour Host { get; }
        public Transform Root { get; }

        public float RocketDuration { get; set; } = 0.52f;
        public float PropellerDuration { get; set; } = 1.05f;
        public float BombDuration { get; set; } = 0.62f;
        public float ColorBallDuration { get; set; } = 0.4f;
        public float SpawnPopDuration { get; set; } = 0.28f;

        public Sprite Dot { get; private set; }
        public Sprite Smoke { get; private set; }
        public Sprite Star { get; private set; }
        public Sprite Star2 { get; private set; }
        public Sprite Glow { get; private set; }
        public Sprite Rainbow { get; private set; }
        public Sprite Flash { get; private set; }
        public Sprite Starlight { get; private set; }
        public Sprite BombShard { get; private set; }
        public Sprite ArrowUp { get; private set; }
        public Sprite ArrowDown { get; private set; }
        public Sprite Propeller { get; private set; }
        public Sprite Flower { get; private set; }
        public Sprite XCross { get; private set; }
        public Sprite LeavesX { get; private set; }
        public Sprite LeavesXBlur { get; private set; }
        public Sprite LeavesHeng { get; private set; }
        public Sprite LeavesShu { get; private set; }
        public Sprite LeavesLizi { get; private set; }

        static Material _mat;
        static Material _additiveMat;
        static Texture2D _whiteTex;
        static Texture2D _ringTex;
        static Sprite _ringSprite;
        static Sprite _dotSprite;

        SimplePool<SpriteRenderer> _spritePool;
        Transform _poolRoot;
        ParticleSystem _burstPs;

        public BoosterFxContext(MonoBehaviour host)
        {
            Host = host;
            Root = host.transform;
        }

        public void EnsureSpritesLoaded()
        {
            if (Dot != null && LeavesX != null && Propeller != null) return;
            LoadSprites();
        }

        public void LoadSprites()
        {
            Dot = Load("boost_other_dot") ?? Load("particle_other_dot") ?? CachedDot();
            Smoke = Load("efx_smoke_1") ?? Load("particle_smoke") ?? Dot;
            Star = Load("particle_die_star_1") ?? Load("particle_die_star_2") ?? Dot;
            Star2 = Load("particle_die_star_2") ?? Star;
            Glow = Load("efx_candy_27") ?? Dot;
            Rainbow = Load("efx_Rainbowefx") ?? Load("efx_Rainbowefx_03") ?? Glow;
            Flash = Load("candy_12_particle") ?? Glow;
            Starlight = Load("UIpanel_starlight") ?? Star;
            BombShard = Load("particle_die_candy_27") ?? Star;
            ArrowUp = Load("efx_arrow_2") ?? Star;
            ArrowDown = Load("efx_arrow_1") ?? ArrowUp;
            Propeller = Load("royal_leaves_feiji") ?? Load("boost_candy_propeller") ?? Rainbow;
            Flower = Load("particle_die_candy_flower_small") ?? Star;
            XCross = Load("particle_other_X_cross") ?? GetShockRing();
            LeavesX = Load("royal_leaves_X") ?? XCross;
            LeavesXBlur = Load("royal_leaves_X_mohu") ?? LeavesX;
            LeavesHeng = Load("royal_leaves_heng") ?? Glow;
            LeavesShu = Load("royal_leaves_shu") ?? Glow;
            LeavesLizi = Load("royal_leaves_lizi1") ?? Load("royal_leaves_lizi2") ?? Flower;
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

        public void SpawnFadingParticle(Vector3 pos, Sprite sprite, float size, Color color, float life,
            Vector3 velocity = default, bool additive = false)
        {
            if (!additive && (sprite == null || sprite == Dot))
            {
                EmitBurst(pos, size, color, life, velocity);
                return;
            }

            var sr = RentSprite(sprite != null ? sprite : Dot, pos, SortingOrder + 8, additive);
            if (sr == null) return;
            sr.transform.localScale = Vector3.one * size;
            Tint(sr, color);
            Host.StartCoroutine(FadeMove(sr, life, velocity));
        }

        void EmitBurst(Vector3 pos, float size, Color color, float life, Vector3 velocity)
        {
            EnsureBurstPs();
            var ep = new ParticleSystem.EmitParams
            {
                position = pos,
                startColor = color,
                startSize = Mathf.Max(0.05f, size),
                startLifetime = Mathf.Max(0.05f, life),
                velocity = velocity
            };
            _burstPs.Emit(ep, 1);
        }

        void EnsureBurstPs()
        {
            if (_burstPs != null) return;
            var go = new GameObject("BoosterBurstPs");
            go.transform.SetParent(Root, false);
            _burstPs = go.AddComponent<ParticleSystem>();
            _burstPs.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = _burstPs.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 256;
            main.startSpeed = 0f;
            main.gravityModifier = 0.15f;
            main.startLifetime = 0.3f;
            main.startSize = 0.2f;
            main.startColor = Color.white;

            var emission = _burstPs.emission;
            emission.enabled = false;

            var shape = _burstPs.shape;
            shape.enabled = false;

            var colorOver = _burstPs.colorOverLifetime;
            colorOver.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            colorOver.color = new ParticleSystem.MinMaxGradient(grad);

            var sizeOver = _burstPs.sizeOverLifetime;
            sizeOver.enabled = true;
            sizeOver.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.65f));

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sortingOrder = SortingOrder + 8;
            var mat = new Material(GetDefaultMat()) { hideFlags = HideFlags.HideAndDontSave, name = "BoosterBurstPsMat" };
            if (Dot != null && Dot.texture != null)
                mat.mainTexture = Dot.texture;
            renderer.material = mat;
            _burstPs.Play();
        }

        IEnumerator FadeMove(SpriteRenderer sr, float life, Vector3 velocity)
        {
            float t = 0f;
            var baseCol = sr.color;
            var baseScale = sr.transform.localScale;
            while (t < life && sr != null)
            {
                if (!sr.gameObject.activeSelf) yield break;
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / life);
                sr.transform.position += velocity * Time.deltaTime;
                velocity *= 0.9f;
                var c = baseCol;
                c.a = baseCol.a * (1f - u);
                sr.color = c;
                sr.transform.localScale = baseScale * (1f - u * 0.35f);
                yield return null;
            }
            if (sr != null && sr.gameObject.activeSelf)
                Release(sr);
        }

        public SpriteRenderer MakeSprite(string name, Sprite sprite, Vector3 pos, int order, bool additive = false) =>
            RentSprite(sprite, pos, order, additive);

        public SpriteRenderer RentSprite(Sprite sprite, Vector3 pos, int order, bool additive = false)
        {
            EnsurePool();
            var sr = _spritePool.Rent();
            sr.transform.SetParent(Root, false);
            sr.transform.position = pos;
            sr.transform.rotation = Quaternion.identity;
            sr.transform.localScale = Vector3.one;
            sr.sprite = sprite != null ? sprite : CachedDot();
            sr.sortingOrder = order;
            sr.sharedMaterial = additive ? GetAdditiveMat() : GetDefaultMat();
            sr.color = Color.white;
            sr.drawMode = SpriteDrawMode.Simple;
            return sr;
        }

        public void Release(SpriteRenderer sr)
        {
            if (sr == null) return;
            EnsurePool();
            sr.sprite = null;
            _spritePool.Release(sr);
        }

        void EnsurePool()
        {
            if (_spritePool != null) return;
            if (_poolRoot == null)
            {
                var go = new GameObject("BoosterFxPool");
                go.transform.SetParent(Root, false);
                go.SetActive(false);
                _poolRoot = go.transform;
            }
            _spritePool = new SimplePool<SpriteRenderer>(_poolRoot, () =>
            {
                var go = new GameObject("FxSprite");
                go.transform.SetParent(_poolRoot, false);
                return go.AddComponent<SpriteRenderer>();
            }, prewarm: 24);
        }

        public static Material GetDefaultMat()
        {
            if (_mat != null) return _mat;
            var sh = Shader.Find("Sprites/Default") ?? Shader.Find("UI/Default");
            if (sh != null) _mat = new Material(sh) { hideFlags = HideFlags.HideAndDontSave };
            return _mat;
        }

        public static Material GetAdditiveMat()
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

        public static Sprite GetShockRing()
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

        public static void Tint(SpriteRenderer sr, Color c)
        {
            if (sr != null) sr.color = c;
        }

        public static void FitSpriteWorld(SpriteRenderer sr, float worldW, float worldH)
        {
            if (sr == null || sr.sprite == null) return;
            var b = sr.sprite.bounds.size;
            if (b.x < 0.0001f || b.y < 0.0001f) return;
            sr.drawMode = SpriteDrawMode.Simple;
            sr.transform.localScale = new Vector3(worldW / b.x, worldH / b.y, 1f);
        }

        public static Vector3 QuadBezier(Vector3 a, Vector3 b, Vector3 c, float t)
        {
            float u = 1f - t;
            return u * u * a + 2f * u * t * b + t * t * c;
        }

        static Sprite CachedDot()
        {
            if (_dotSprite != null) return _dotSprite;
            const int size = 32;
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
            _dotSprite = Sprite.Create(_whiteTex, new Rect(0, 0, _whiteTex.width, _whiteTex.height), new Vector2(0.5f, 0.5f), size);
            return _dotSprite;
        }
    }
}
