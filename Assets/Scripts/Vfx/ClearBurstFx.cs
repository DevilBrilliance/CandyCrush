using System.Collections.Generic;
using CandyCrush.Data;
using UnityEngine;

namespace CandyCrush.Vfx
{
    /// <summary>
    /// 消除碎块：格心沿伞形弧线分散发射对应颜色 particle_die。
    /// 4~6 块随机从格心出生、抛射角度各不同，先上抛再下落；前 fadeDelay 实色，之后逐渐透明。
    /// </summary>
    public class ClearBurstFx : MonoBehaviour
    {
        [Header("Timing")]
        [SerializeField] float duration = 1.5f;
        [SerializeField] float fadeDelay = 0.5f;

        [Header("Burst")]
        [SerializeField] int burstCountMin = 4;
        [SerializeField] int burstCountMax = 6;
        [Tooltip("伞形弧线总张角（度），碎块抛射方向分布在此弧上")]
        [SerializeField] float arcSpan = 130f;
        [Tooltip("每个碎块相对弧线槽位的角度抖动（度）")]
        [SerializeField] float angleJitter = 14f;
        [Tooltip("相对格子边长的初速（上抛要看得见）")]
        [SerializeField] Vector2 speedMul = new Vector2(2.1f, 3.5f);
        [Tooltip("Unity 重力倍率")]
        [SerializeField] float gravityModifier = 0.55f;
        [Tooltip("相对格子边长的碎块尺寸")]
        [SerializeField] Vector2 sizeMul = new Vector2(0.2f, 0.32f);
        [SerializeField] int sparkCount = 3;

        float Duration => duration > 0.01f ? duration : 1.5f;
        float FadeDelay => Mathf.Clamp(fadeDelay, 0f, Duration);

        /// <summary>棋子 10，碎块夹在棋子与雪花(200)之间。</summary>
        public const int ShardSortingOrder = 100;

        TileSpriteCatalog _catalog;
        readonly Dictionary<TileType, ParticleSystem> _systems = new Dictionary<TileType, ParticleSystem>();
        ParticleSystem _spark;
        static Material _mat;
        readonly List<float> _angleSlots = new List<float>(8);

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
                    EmitUmbrella(ps, worldPos, cellSize);
                }
            }

            PlaySpark(worldPos, cellSize);
        }

        /// <summary>
        /// 格心出生，沿伞形弧线用不同角度上抛：数量 4~6 随机，方向各不相同。
        /// </summary>
        void EmitUmbrella(ParticleSystem ps, Vector3 center, float cellSize)
        {
            float s = Mathf.Max(0.45f, cellSize);
            int min = Mathf.Max(1, Mathf.Min(burstCountMin, burstCountMax));
            int max = Mathf.Max(min, burstCountMax);
            int count = Random.Range(min, max + 1);

            float half = Mathf.Max(20f, arcSpan) * 0.5f;
            _angleSlots.Clear();
            for (int i = 0; i < count; i++)
            {
                float slot = (i + 0.5f) / count;
                float baseAngle = Mathf.Lerp(-half, half, slot);
                _angleSlots.Add(baseAngle + Random.Range(-angleJitter, angleJitter));
            }

            for (int i = _angleSlots.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (_angleSlots[i], _angleSlots[j]) = (_angleSlots[j], _angleSlots[i]);
            }

            for (int i = 0; i < count; i++)
            {
                float angleDeg = _angleSlots[i];
                Vector3 dir = Quaternion.Euler(0f, 0f, angleDeg) * Vector3.up;

                float speed = s * Random.Range(speedMul.x, speedMul.y);
                Vector3 vel = dir * speed + Vector3.up * (s * Random.Range(0.35f, 0.85f));

                var ep = new ParticleSystem.EmitParams
                {
                    position = center,
                    velocity = vel,
                    startSize = s * Random.Range(sizeMul.x, sizeMul.y),
                    startLifetime = Duration,
                    startColor = Color.white,
                    rotation = Random.Range(0f, 360f),
                    applyShapeToPosition = false
                };
                ps.Emit(ep, 1);
            }
        }

        void PlaySpark(Vector3 worldPos, float cellSize)
        {
            if (sparkCount <= 0) return;
            EnsureSpark();
            if (_spark == null) return;
            var main = _spark.main;
            float s = Mathf.Max(0.4f, cellSize);
            main.startSize = new ParticleSystem.MinMaxCurve(s * 0.05f, s * 0.1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(s * 0.6f, s * 1.2f);
            main.gravityModifier = gravityModifier * 0.6f;
            _spark.transform.position = worldPos;
            _spark.Emit(sparkCount);
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
            main.startColor = Color.white;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 64;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.startSpeed = 0f;
            main.startLifetime = Duration;
            main.duration = Duration + 0.1f;

            var emission = ps.emission;
            emission.enabled = false;
            emission.rateOverTime = 0f;

            var shape = ps.shape;
            shape.enabled = false;

            var limit = ps.limitVelocityOverLifetime;
            limit.enabled = false;

            var sizeOver = ps.sizeOverLifetime;
            sizeOver.enabled = false;

            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.separateAxes = false;
            rot.z = new ParticleSystem.MinMaxCurve(-5f, 5f);

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
            Tune(ps, 0.95f);

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
            main.duration = 0.4f;
            main.startLifetime = 0.35f;
            main.startColor = Color.white;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 32;

            var emission = _spark.emission;
            emission.enabled = false;

            var shape = _spark.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 45f;
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
            main.startSpeed = 0f;
            main.gravityModifier = gravityModifier;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var shape = ps.shape;
            shape.enabled = false;

            var sizeOver = ps.sizeOverLifetime;
            sizeOver.enabled = false;

            // 仅在首次 / hold 变化时写 Gradient，避免每格消都分配
            ApplyFadeOverLifetime(ps);
        }

        float _cachedHold = -1f;
        Gradient _fadeGradient;
        GradientColorKey[] _fadeColors;
        GradientAlphaKey[] _fadeAlphas;

        void ApplyFadeOverLifetime(ParticleSystem ps)
        {
            var colorOver = ps.colorOverLifetime;
            colorOver.enabled = true;

            float hold = Duration > 0.0001f ? FadeDelay / Duration : 0f;
            hold = Mathf.Clamp01(hold);

            if (_fadeGradient == null || !Mathf.Approximately(_cachedHold, hold))
            {
                _cachedHold = hold;
                if (_fadeGradient == null)
                {
                    _fadeGradient = new Gradient();
                    _fadeColors = new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) };
                    _fadeAlphas = new GradientAlphaKey[3];
                }
                _fadeAlphas[0] = new GradientAlphaKey(1f, 0f);
                _fadeAlphas[1] = new GradientAlphaKey(1f, hold);
                _fadeAlphas[2] = new GradientAlphaKey(0f, 1f);
                _fadeGradient.SetKeys(_fadeColors, _fadeAlphas);
            }

            colorOver.color = new ParticleSystem.MinMaxGradient(_fadeGradient);
        }

        static Material SharedMat()
        {
            if (_mat != null) return _mat;
            var shader = Shader.Find("Sprites/Default")
                         ?? Shader.Find("Particles/Standard Unlit");
            _mat = new Material(shader) { name = "ClearDieShardMat" };
            if (_mat.HasProperty("_Color"))
                _mat.SetColor("_Color", Color.white);
            return _mat;
        }
    }
}
