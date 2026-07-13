using UnityEngine;

namespace CandyCrush.Vfx
{
    /// <summary>全屏飘雪/雨丝，不拦截点击。</summary>
    public class AtmosphereFx : MonoBehaviour
    {
        [SerializeField] ParticleSystem snow;
        [SerializeField] ParticleSystem rain;

        public void Play()
        {
            if (snow != null && !snow.isPlaying) snow.Play(true);
            if (rain != null && !rain.isPlaying) rain.Play(true);
        }

        public void Stop()
        {
            if (snow != null) snow.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (rain != null) rain.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        public static AtmosphereFx CreateDefault(Transform parent, Sprite snowSprite)
        {
            // 避免重复创建导致粒子报错刷屏
            var existing = parent != null ? parent.GetComponentInChildren<AtmosphereFx>(true) : null;
            if (existing != null)
            {
                existing.Play();
                return existing;
            }

            var root = new GameObject("AtmosphereFx");
            root.transform.SetParent(parent, false);
            var fx = root.AddComponent<AtmosphereFx>();

            var snowGo = new GameObject("Snow");
            snowGo.transform.SetParent(root.transform, false);
            snowGo.transform.localPosition = new Vector3(0f, 8f, 0f);

            var ps = snowGo.AddComponent<ParticleSystem>();
            fx.snow = ps;

            // 先停掉默认播放，配置完成后再 Play
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = true;
            main.startLifetime = 4f;
            main.startSpeed = 0f; // 下落交给 velocityOverLifetime，避免冲突
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.18f);
            main.startColor = new Color(1f, 1f, 1f, 0.85f);
            main.maxParticles = 400;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0f;

            var emission = ps.emission;
            emission.rateOverTime = 45f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(12f, 0.2f, 1f);

            // 三轴必须同一 MinMaxCurve 模式，否则报：
            // "Particle Velocity curves must all be in the same mode"
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.Local;
            vel.x = new ParticleSystem.MinMaxCurve(-0.25f, 0.25f); // TwoConstants
            vel.y = new ParticleSystem.MinMaxCurve(-1.6f, -0.7f);  // TwoConstants
            vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);        // TwoConstants（不可用 Constant）

            var colorOver = ps.colorOverLifetime;
            colorOver.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.9f, 0.2f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOver.color = new ParticleSystem.MinMaxGradient(grad);

            var renderer = snowGo.GetComponent<ParticleSystemRenderer>();
            renderer.sortingOrder = 50;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader != null)
            {
                var mat = new Material(shader);
                if (snowSprite != null && snowSprite.texture != null)
                    mat.mainTexture = snowSprite.texture;
                renderer.sharedMaterial = mat;
            }

            fx.Play();
            return fx;
        }
    }
}
