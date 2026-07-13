using UnityEngine;

namespace CandyCrush.Vfx
{
    /// <summary>
    /// 全屏白点飘雪：最上层（高于棋子与消除碎块），Ignore Raycast 不挡点击。
    /// sorting：棋子 10 &lt; 碎块 100 &lt; 雪花 200
    /// </summary>
    public class AtmosphereFx : MonoBehaviour
    {
        public const int SnowSortingOrder = 200;

        [SerializeField] ParticleSystem snow;
        static Texture2D _sharedDot;
        static Material _sharedMat;

        public void Play()
        {
            if (snow != null && !snow.isPlaying)
                snow.Play(true);
        }

        public void Stop()
        {
            if (snow != null)
                snow.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        public static AtmosphereFx CreateDefault(Transform parent, Sprite unused = null)
        {
            // 旧实例常绑了整张图集，强制重建
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

            var snowGo = new GameObject("Snow");
            snowGo.transform.SetParent(root.transform, false);
            snowGo.transform.localPosition = new Vector3(0f, 7.5f, 0f);
            if (ignore >= 0) snowGo.layer = ignore;

            var ps = snowGo.AddComponent<ParticleSystem>();
            fx.snow = ps;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(3.5f, 6.5f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.14f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 1f, 1f, 0.55f),
                new Color(1f, 1f, 1f, 0.95f));
            main.maxParticles = 320;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0f;

            var emission = ps.emission;
            emission.rateOverTime = 38f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(14f, 0.2f, 0.1f);

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(-0.4f, 0.4f);
            vel.y = new ParticleSystem.MinMaxCurve(-1.9f, -0.55f);
            vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            var colorOver = ps.colorOverLifetime;
            colorOver.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.9f, 0.12f),
                    new GradientAlphaKey(0.65f, 0.75f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOver.color = new ParticleSystem.MinMaxGradient(grad);

            // 关闭贴图序列，避免图集 UV 错乱
            var sheet = ps.textureSheetAnimation;
            sheet.enabled = false;

            var renderer = snowGo.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingLayerName = "Default";
            renderer.sortingOrder = SnowSortingOrder;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.sharedMaterial = GetSharedSnowMaterial();

            fx.Play();
            return fx;
        }

        static Material GetSharedSnowMaterial()
        {
            if (_sharedMat != null) return _sharedMat;

            if (_sharedDot == null)
                _sharedDot = CreateSoftDotTexture(64);

            var shader = Shader.Find("Particles/Standard Unlit")
                         ?? Shader.Find("Legacy Shaders/Particles/Additive")
                         ?? Shader.Find("Sprites/Default");

            _sharedMat = new Material(shader) { name = "SnowSoftDotMat" };
            _sharedMat.mainTexture = _sharedDot;
            if (_sharedMat.HasProperty("_Color"))
                _sharedMat.SetColor("_Color", Color.white);
            if (_sharedMat.HasProperty("_TintColor"))
                _sharedMat.SetColor("_TintColor", new Color(1f, 1f, 1f, 0.75f));
            // Additive 更像参考视频里的亮点雪花
            if (_sharedMat.HasProperty("_Mode"))
                _sharedMat.SetFloat("_Mode", 1f);

            return _sharedMat;
        }

        static Texture2D CreateSoftDotTexture(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "SnowSoftDot",
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
                a *= a;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }

            tex.Apply(false, true);
            return tex;
        }
    }
}
