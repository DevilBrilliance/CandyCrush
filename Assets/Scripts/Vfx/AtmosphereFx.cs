using UnityEngine;

namespace CandyCrush.Vfx
{
    /// <summary>
    /// 前景软边飘雪 + 背景斜向雨丝（朝向跟速度一致，左下）。
    /// sorting：夜景 &lt; 雨丝 &lt; 棋盘0 &lt; 棋子10 &lt; 碎块100 &lt; 雪絮200
    /// </summary>
    public class AtmosphereFx : MonoBehaviour
    {
        public const int RainSortingOrder = -5;
        public const int SnowSortingOrder = 200;

        [SerializeField] ParticleSystem snow;
        [SerializeField] ParticleSystem rain;
        [SerializeField] ParticleSystem fineSnow;

        static Material _snowMat;
        static Material _rainMat;
        static Material _fineMat;
        static Texture2D _clumpTex;
        static Texture2D _streakTex;
        static Texture2D _dotTex;

        public void Play()
        {
            if (snow != null && !snow.isPlaying) snow.Play(true);
            if (rain != null && !rain.isPlaying) rain.Play(true);
            if (fineSnow != null && !fineSnow.isPlaying) fineSnow.Play(true);
        }

        public static AtmosphereFx CreateDefault(Transform parent)
        {
            // 重建时清掉缓存，避免沿用坏材质/贴图
            _snowMat = null;
            _rainMat = null;
            _fineMat = null;
            _clumpTex = null;
            _streakTex = null;
            _dotTex = null;

            if (parent != null)
            {
                var old = parent.GetComponentInChildren<AtmosphereFx>(true);
                if (old != null)
                {
                    if (Application.isPlaying) Object.Destroy(old.gameObject);
                    else Object.DestroyImmediate(old.gameObject);
                }
            }

            int ignore = LayerMask.NameToLayer("Ignore Raycast");
            var root = new GameObject("AtmosphereFx");
            root.transform.SetParent(parent, false);
            if (ignore >= 0) root.layer = ignore;

            var fx = root.AddComponent<AtmosphereFx>();
            fx.rain = BuildRain(root.transform, ignore);
            fx.snow = BuildSnowFluff(root.transform, ignore);
            fx.fineSnow = BuildFineSnow(root.transform, ignore);
            fx.Play();
            return fx;
        }

        static ParticleSystem BuildSnowFluff(Transform parent, int ignoreLayer)
        {
            var go = new GameObject("SnowFluff");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 8f, 0f);
            if (ignoreLayer >= 0) go.layer = ignoreLayer;

            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = true;
            main.startLifetime = TwoConst(4.5f, 7.5f);
            main.startSpeed = 0f;
            main.startSize = TwoConst(0.12f, 0.32f);
            main.startRotation = TwoConst(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 1f, 1f, 0.55f),
                new Color(1f, 1f, 1f, 0.95f));
            main.maxParticles = 70;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0f;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;

            var emission = ps.emission;
            emission.rateOverTime = 11f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(15f, 0.35f, 0.1f);

            SetVelocity(ps, -0.25f, 0.35f, -1.2f, -0.35f);

            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = TwoConst(-30f * Mathf.Deg2Rad, 30f * Mathf.Deg2Rad);

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.18f;
            noise.frequency = 0.28f;
            noise.scrollSpeed = 0.1f;
            noise.damping = true;
            noise.octaveCount = 1;

            var colorOver = ps.colorOverLifetime;
            colorOver.enabled = true;
            colorOver.color = FadeGradient(0.12f, 0.72f);

            var sizeOver = ps.sizeOverLifetime;
            sizeOver.enabled = true;
            sizeOver.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.75f, 1f, 1.05f));

            DisableSheet(ps);
            ApplyRenderer(go, SnowSortingOrder, GetSnowMaterial(), ParticleSystemRenderMode.Billboard);
            return ps;
        }

        static ParticleSystem BuildFineSnow(Transform parent, int ignoreLayer)
        {
            var go = new GameObject("SnowFine");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 8.2f, 0f);
            if (ignoreLayer >= 0) go.layer = ignoreLayer;

            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = true;
            main.startLifetime = TwoConst(3.5f, 6f);
            main.startSpeed = 0f;
            main.startSize = TwoConst(0.02f, 0.06f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 1f, 1f, 0.4f),
                new Color(1f, 1f, 1f, 0.9f));
            main.maxParticles = 140;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 24f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(15f, 0.25f, 0.1f);

            SetVelocity(ps, -0.3f, 0.3f, -1.8f, -0.6f);

            var colorOver = ps.colorOverLifetime;
            colorOver.enabled = true;
            colorOver.color = FadeGradient(0.08f, 0.7f);

            DisableSheet(ps);
            ApplyRenderer(go, SnowSortingOrder - 1, GetFineMaterial(), ParticleSystemRenderMode.Billboard);
            return ps;
        }

        static ParticleSystem BuildRain(Transform parent, int ignoreLayer)
        {
            var go = new GameObject("RainStreaks");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 9.5f, 0f);
            go.transform.localRotation = Quaternion.identity;
            if (ignoreLayer >= 0) go.layer = ignoreLayer;

            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = true;
            main.startLifetime = TwoConst(0.75f, 1.25f);
            // 方向只走 VelocityOverLifetime，Stretch 会沿速度拉长 —— 朝向与运动一致
            main.startSpeed = 0f;
            main.startSize = TwoConst(0.04f, 0.07f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.85f, 0.92f, 1f, 0.55f),
                new Color(1f, 1f, 1f, 0.95f));
            main.maxParticles = 240;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0f;
            main.startRotation = 0f;

            var emission = ps.emission;
            emission.rateOverTime = 85f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(18f, 0.25f, 0.1f);

            // 左下：vx<0, vy<0 → 视觉应为「/」斜线，朝向跟运动一致
            SetVelocity(ps, -7.5f, -5.0f, -16f, -11f);

            var colorOver = ps.colorOverLifetime;
            colorOver.enabled = true;
            colorOver.color = FadeGradient(0.04f, 0.78f);

            DisableSheet(ps);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 2.2f;
            renderer.velocityScale = 0.06f;
            renderer.cameraVelocityScale = 0f;
            renderer.sortingOrder = RainSortingOrder;
            var rainMat = GetRainMaterial();
            renderer.sharedMaterial = rainMat;
            renderer.material = rainMat;
            return ps;
        }

        static void ApplyRenderer(GameObject go, int sortingOrder, Material mat, ParticleSystemRenderMode mode)
        {
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = mode;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.sortingOrder = sortingOrder;
            renderer.sharedMaterial = mat;
            // 强制实例材质，避免被 Default-Particle 白方块顶掉
            renderer.material = mat;
        }

        static void SetVelocity(ParticleSystem ps, float x0, float x1, float y0, float y1)
        {
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = TwoConst(x0, x1);
            vel.y = TwoConst(y0, y1);
            vel.z = TwoConst(0f, 0f);
        }

        static ParticleSystem.MinMaxCurve TwoConst(float a, float b) =>
            new ParticleSystem.MinMaxCurve(a, b);

        static ParticleSystem.MinMaxGradient FadeGradient(float inEnd, float outStart)
        {
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, inEnd),
                    new GradientAlphaKey(0.85f, outStart),
                    new GradientAlphaKey(0f, 1f)
                });
            return new ParticleSystem.MinMaxGradient(grad);
        }

        static void DisableSheet(ParticleSystem ps)
        {
            var sheet = ps.textureSheetAnimation;
            sheet.enabled = false;
        }

        static Material GetSnowMaterial()
        {
            if (_snowMat != null) return _snowMat;
            _snowMat = MakeParticleMat("AtmSnowFluffMat", GetClumpTexture(), additive: true);
            return _snowMat;
        }

        static Material GetFineMaterial()
        {
            if (_fineMat != null) return _fineMat;
            _fineMat = MakeParticleMat("AtmSnowFineMat", GetDotTexture(), additive: true);
            return _fineMat;
        }

        static Material GetRainMaterial()
        {
            if (_rainMat != null) return _rainMat;
            // 雨丝用 Alpha 混合更清晰，避免 Additive 在暗景里发糊
            _rainMat = MakeParticleMat("AtmRainMat", GetStreakTexture(), additive: false);
            return _rainMat;
        }

        static Material MakeParticleMat(string name, Texture2D tex, bool additive)
        {
            Shader shader = null;
            if (additive)
            {
                shader = Shader.Find("Legacy Shaders/Particles/Additive")
                         ?? Shader.Find("Particles/Additive")
                         ?? Shader.Find("Mobile/Particles/Additive");
            }

            if (shader == null)
            {
                shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended")
                         ?? Shader.Find("Particles/Standard Unlit")
                         ?? Shader.Find("Sprites/Default")
                         ?? Shader.Find("UI/Default");
            }

            var mat = new Material(shader) { name = name, hideFlags = HideFlags.HideAndDontSave };
            mat.mainTexture = tex;
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_TintColor"))
                mat.SetColor("_TintColor", new Color(1f, 1f, 1f, 0.55f));
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", Color.white);
            return mat;
        }

        static Texture2D GetClumpTexture()
        {
            if (_clumpTex != null) return _clumpTex;
            const int size = 64;
            _clumpTex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "AtmSoftClump",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };

            // 多圆重叠软絮（Additive 下黑=透明，这里仍写好 alpha）
            var centers = new[]
            {
                new Vector2(0.50f, 0.52f),
                new Vector2(0.38f, 0.44f),
                new Vector2(0.62f, 0.46f),
                new Vector2(0.48f, 0.64f),
                new Vector2(0.36f, 0.58f),
                new Vector2(0.60f, 0.58f)
            };
            var radii = new[] { 0.42f, 0.28f, 0.27f, 0.24f, 0.22f, 0.22f };

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = (x + 0.5f) / size;
                float v = (y + 0.5f) / size;
                float a = 0f;
                for (int i = 0; i < centers.Length; i++)
                {
                    float dx = (u - centers[i].x) / radii[i];
                    float dy = (v - centers[i].y) / radii[i];
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float s = Mathf.Clamp01(1f - d);
                    a = Mathf.Max(a, s * s * s);
                }
                // Additive：RGB 本身当亮度；周边必须是黑
                _clumpTex.SetPixel(x, y, new Color(a, a, a, 1f));
            }
            _clumpTex.Apply(false, true);
            return _clumpTex;
        }

        static Texture2D GetDotTexture()
        {
            if (_dotTex != null) return _dotTex;
            const int size = 32;
            _dotTex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "AtmFineDot",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            float half = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x - half) / half;
                float dy = (y - half) / half;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(1f - d);
                a = a * a * a;
                _dotTex.SetPixel(x, y, new Color(a, a, a, 1f));
            }
            _dotTex.Apply(false, true);
            return _dotTex;
        }

        static Texture2D GetStreakTexture()
        {
            if (_streakTex != null) return _streakTex;
            const int w = 16;
            const int h = 48;
            _streakTex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = "AtmRainStreak",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            float cx = (w - 1) * 0.5f;
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float nx = Mathf.Abs(x - cx) / Mathf.Max(0.001f, cx);
                float ny = y / (float)(h - 1);
                float radial = Mathf.Clamp01(1f - nx);
                radial = radial * radial;
                float tip = Mathf.Sin(ny * Mathf.PI);
                float a = radial * tip;
                // Alpha Blended：白 + alpha
                _streakTex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            _streakTex.Apply(false, true);
            return _streakTex;
        }
    }
}
