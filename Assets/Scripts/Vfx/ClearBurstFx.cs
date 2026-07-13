using System.Collections.Generic;
using CandyCrush.Data;
using UnityEngine;

namespace CandyCrush.Vfx
{
    /// <summary>
    /// 消除碎块：格心 Burst 对应颜色 particle_die，炸开后受重力下落。
    /// 伞形上抛后逐渐透明消失（尺寸不变）。
    /// </summary>
    public class ClearBurstFx : MonoBehaviour
    {
        [SerializeField] float duration = 1.5f;
        [SerializeField] float fadeDelay = 0.5f;

        float Duration => duration > 0.01f ? duration : 1.5f;
        float FadeDelay => Mathf.Clamp(fadeDelay, 0f, Duration);

        /// <summary>棋子 10，碎块夹在棋子与雪花(200)之间。</summary>
        public const int ShardSortingOrder = 100;
        const int BurstCount = 10;

        TileSpriteCatalog _catalog;
        readonly Dictionary<TileType, ParticleSystem> _systems = new Dictionary<TileType, ParticleSystem>();
        ParticleSystem _spark;
        static Material _mat;

        public static ClearBurstFx Ensure(Transform parent, TileSpriteCatalog catalog)
        {
            var existing = parent.GetComponentInChildren<ClearBurstFx>(true);
            if (existing != null)
            {
                existing.Configure(catalog);
                return existing;
            }

            var go = new GameObject("ClearBurstFx");
            go.transform.SetParent(parent, false);
            var fx = go.AddComponent<ClearBurstFx>();
            fx.Rebuild(catalog);
            return fx;
        }

        public void Configure(TileSpriteCatalog catalog)
        {
            if (_catalog == catalog && transform.childCount > 0) return;
            Rebuild(catalog);
        }

        void Rebuild(TileSpriteCatalog catalog)
        {
            _catalog = catalog;
            StopAllCoroutines();
            _systems.Clear();
            _spark = null;
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }
        }

        public void Play(TileType type, Vector3 worldPos, float cellSize)
        {
            if (TileTypeUtil.IsNormal(type))
            {
                var ps = GetOrCreate(type);
                if (ps != null)
                {
                    Tune(ps, cellSize);
                    ps.transform.position = worldPos;
                    ps.Emit(BurstCount);
                }
            }

            PlaySpark(worldPos, cellSize);
        }

        void PlaySpark(Vector3 worldPos, float cellSize)
        {
            EnsureSpark();
            if (_spark == null) return;
            var main = _spark.main;
            float s = Mathf.Max(0.4f, cellSize);
            main.startSize = new ParticleSystem.MinMaxCurve(s * 0.06f, s * 0.12f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(s * 1.2f, s * 2.2f);
            _spark.transform.position = worldPos;
            _spark.Emit(4);
        }

        ParticleSystem GetOrCreate(TileType type)
        {
            if (_systems.TryGetValue(type, out var existing) && existing != null)
                return existing;

            var shards = _catalog != null ? _catalog.GetClearShards(type) : null;
            if (shards == null || shards.Length == 0) return null;

            var go = new GameObject($"DieShards_{type}");
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = Duration + 0.1f;
            // 下落同时逐渐透明消失（不缩小）
            main.startLifetime = Duration;
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.1f, 2.25f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.32f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = Color.white;
            main.gravityModifier = 1.05f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 128;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;

            var emission = ps.emission;
            emission.enabled = false;
            emission.rateOverTime = 0f;

            // 伞形上抛：圆锥朝上散开，再被重力拉成下落弧线
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 58f;
            shape.radius = 0.02f;
            shape.radiusThickness = 1f;
            shape.length = 0f;
            shape.arc = 360f;
            shape.rotation = new Vector3(-90f, 0f, 0f); // 默认 +Z → 改为朝 +Y
            shape.position = Vector3.zero;
            shape.scale = Vector3.one;

            var limit = ps.limitVelocityOverLifetime;
            limit.enabled = false;

            var sizeOver = ps.sizeOverLifetime;
            sizeOver.enabled = false;

            // 逐渐透明消失
            ApplyFadeOverLifetime(ps);

            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.separateAxes = false;
            rot.z = new ParticleSystem.MinMaxCurve(-8f, 8f);

            var sheet = ps.textureSheetAnimation;
            sheet.enabled = true;
            sheet.mode = ParticleSystemAnimationMode.Sprites;
            for (int i = sheet.spriteCount - 1; i >= 0; i--)
                sheet.RemoveSprite(i);
            for (int i = 0; i < shards.Length; i++)
            {
                if (shards[i] != null)
                    sheet.AddSprite(shards[i]);
            }
            sheet.startFrame = new ParticleSystem.MinMaxCurve(0f, 1f);
            sheet.frameOverTime = new ParticleSystem.MinMaxCurve(0f);
            sheet.cycleCount = 1;

            ApplyShardSorting(go.GetComponent<ParticleSystemRenderer>());

            _systems[type] = ps;
            return ps;
        }

        void EnsureSpark()
        {
            if (_spark != null) return;
            var flashSprite = _catalog != null ? _catalog.clearFlashSprite : null;

            var go = new GameObject("ClearSparks");
            go.transform.SetParent(transform, false);
            _spark = go.AddComponent<ParticleSystem>();
            _spark.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = _spark.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.35f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.35f);
            main.startSpeed = 1.5f;
            main.startSize = 0.1f;
            main.startColor = Color.white;
            main.gravityModifier = 0.8f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 64;

            var emission = _spark.emission;
            emission.enabled = false;

            var shape = _spark.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 50f;
            shape.radius = 0.01f;
            shape.rotation = new Vector3(-90f, 0f, 0f);

            var sizeOver = _spark.sizeOverLifetime;
            sizeOver.enabled = false;

            var colorOver = _spark.colorOverLifetime;
            colorOver.enabled = false;

            if (flashSprite != null)
            {
                var sheet = _spark.textureSheetAnimation;
                sheet.enabled = true;
                sheet.mode = ParticleSystemAnimationMode.Sprites;
                for (int i = sheet.spriteCount - 1; i >= 0; i--)
                    sheet.RemoveSprite(i);
                sheet.AddSprite(flashSprite);
            }

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            ApplyShardSorting(renderer);
            renderer.sortingOrder = ShardSortingOrder + 1;
        }

        static void ApplyShardSorting(ParticleSystemRenderer renderer)
        {
            if (renderer == null) return;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.sortMode = ParticleSystemSortMode.YoungestInFront;
            renderer.sortingLayerName = "Default";
            renderer.sortingOrder = ShardSortingOrder;
            renderer.sharedMaterial = SharedMat();
            renderer.minParticleSize = 0f;
            renderer.maxParticleSize = 3f;
        }

        void Tune(ParticleSystem ps, float cellSize)
        {
            var main = ps.main;
            float s = Mathf.Max(0.45f, cellSize);
            main.startSize = new ParticleSystem.MinMaxCurve(s * 0.22f, s * 0.38f);
            // 下落速度约为原先一半
            main.startSpeed = new ParticleSystem.MinMaxCurve(s * 1.6f, s * 2.8f);
            main.gravityModifier = 1.4f;
            main.startLifetime = Duration;
            main.duration = Duration + 0.1f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 58f;
            shape.rotation = new Vector3(-90f, 0f, 0f);

            var sizeOver = ps.sizeOverLifetime;
            sizeOver.enabled = false;

            ApplyFadeOverLifetime(ps);
        }

        /// <summary>前 fadeDelay 秒保持不透明，之后再逐渐透明到结束。</summary>
        void ApplyFadeOverLifetime(ParticleSystem ps)
        {
            var colorOver = ps.colorOverLifetime;
            colorOver.enabled = true;

            float hold = Duration > 0.0001f ? FadeDelay / Duration : 0f;
            hold = Mathf.Clamp01(hold);

            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, hold),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOver.color = new ParticleSystem.MinMaxGradient(grad);
        }

        static Material SharedMat()
        {
            if (_mat != null) return _mat;
            // 与 SpriteRenderer 同一套排序，避免粒子材质盖住雪花
            var shader = Shader.Find("Sprites/Default")
                         ?? Shader.Find("Particles/Standard Unlit");
            _mat = new Material(shader) { name = "ClearDieShardMat" };
            if (_mat.HasProperty("_Color"))
                _mat.SetColor("_Color", Color.white);
            return _mat;
        }
    }
}
