using System.Collections;
using CandyCrush.View;
using UnityEngine;

namespace CandyCrush.Vfx
{
    /// <summary>炸弹：生成弹出 + 5×5 爆闪。</summary>
    public sealed class BombBoosterFx
    {
        readonly BoosterFxContext _fx;

        public BombBoosterFx(BoosterFxContext fx) => _fx = fx;

        /// <summary>L/T 合成出现：星光闪 + 多层冲击环 + 粒子，棋子从 0 弹起。</summary>
        public IEnumerator PlaySpawn(Vector3 origin, float cell, TileView tile = null)
        {
            float dur = Mathf.Max(0.42f, _fx.SpawnPopDuration + 0.2f);
            int order = BoosterFxContext.SortingOrder + 5;

            float baseScale = 1f;
            if (tile != null)
            {
                baseScale = tile.BaseScale > 0.0001f ? tile.BaseScale : tile.transform.localScale.x;
                tile.transform.localScale = Vector3.one * (baseScale * 0.12f);
                tile.SetAlpha(1f);
            }

            var flash = _fx.MakeSprite("BombSpawnFlash", _fx.Starlight != null ? _fx.Starlight : _fx.Flash,
                origin, order + 4, additive: true);
            BoosterFxContext.Tint(flash, new Color(1f, 0.95f, 0.7f, 1f));
            if (flash != null) flash.transform.localScale = Vector3.one * (cell * 0.25f);

            var core = _fx.MakeSprite("BombSpawnCore", _fx.Glow, origin, order + 3, additive: true);
            BoosterFxContext.Tint(core, new Color(1f, 0.55f, 0.2f, 0.95f));
            if (core != null) core.transform.localScale = Vector3.one * (cell * 0.35f);

            var ring0 = _fx.MakeSprite("BombSpawnRing0", BoosterFxContext.GetShockRing(), origin, order + 1, additive: true);
            var ring1 = _fx.MakeSprite("BombSpawnRing1", BoosterFxContext.GetShockRing(), origin, order + 2, additive: true);
            BoosterFxContext.Tint(ring0, new Color(1f, 0.85f, 0.35f, 0.95f));
            BoosterFxContext.Tint(ring1, new Color(1f, 0.55f, 0.2f, 0.85f));
            if (ring0 != null) ring0.transform.localScale = Vector3.one * (cell * 0.3f);
            if (ring1 != null) ring1.transform.localScale = Vector3.one * (cell * 0.3f);

            for (int i = 0; i < 12; i++)
            {
                float ang = (i / 12f) * Mathf.PI * 2f + Random.Range(-0.1f, 0.1f);
                var dir = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f);
                var spr = (i & 1) == 0 ? _fx.Star : _fx.Star2;
                _fx.SpawnFadingParticle(origin, spr,
                    cell * Random.Range(0.22f, 0.4f),
                    new Color(1f, 0.9f, 0.45f, 1f),
                    Random.Range(0.28f, 0.42f),
                    dir * cell * Random.Range(2.2f, 3.8f),
                    additive: true);
            }

            for (int i = 0; i < 5; i++)
            {
                float ang = Random.Range(0f, Mathf.PI * 2f);
                var dir = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f);
                _fx.SpawnFadingParticle(origin + dir * cell * 0.1f, _fx.Smoke,
                    cell * Random.Range(0.4f, 0.75f),
                    new Color(1f, 0.6f, 0.3f, 0.7f),
                    0.4f, dir * cell * 1.2f, additive: true);
            }

            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / dur);
                float ease = 1f - Mathf.Pow(1f - u, 2.6f);
                float punch = 1f + Mathf.Sin(Mathf.Clamp01(u * 1.35f) * Mathf.PI) * 0.35f;

                if (tile != null)
                {
                    float s = Mathf.Lerp(0.12f, 1f, ease) * punch;
                    if (u > 0.7f) s = Mathf.Lerp(s, 1f, (u - 0.7f) / 0.3f);
                    tile.transform.localScale = Vector3.one * (baseScale * s);
                }

                if (flash != null)
                {
                    flash.transform.localScale = Vector3.one * (cell * Mathf.Lerp(0.4f, 2.4f, Mathf.Sin(Mathf.Clamp01(u * 1.5f) * Mathf.PI)));
                    flash.transform.rotation = Quaternion.Euler(0f, 0f, u * 40f);
                    var c = flash.color;
                    c.a = u < 0.4f ? 1f : Mathf.Lerp(1f, 0f, (u - 0.4f) / 0.6f);
                    flash.color = c;
                }

                if (core != null)
                {
                    core.transform.localScale = Vector3.one * (cell * Mathf.Lerp(0.4f, 1.8f, ease));
                    var c = core.color;
                    c.a = 0.95f * (1f - u);
                    core.color = c;
                }

                if (ring0 != null)
                {
                    ring0.transform.localScale = Vector3.one * (cell * Mathf.Lerp(0.4f, 2.8f, ease));
                    var c = ring0.color;
                    c.a = 0.9f * (1f - u) * (1f - u);
                    ring0.color = c;
                }
                if (ring1 != null)
                {
                    float u2 = Mathf.Clamp01((u - 0.12f) / 0.88f);
                    ring1.transform.localScale = Vector3.one * (cell * Mathf.Lerp(0.5f, 3.4f, u2));
                    var c = ring1.color;
                    c.a = 0.75f * (1f - u2) * (1f - u2);
                    ring1.color = c;
                }

                yield return null;
            }

            if (tile != null)
                tile.RestoreVisual();

            _fx.Release(flash);
            _fx.Release(core);
            _fx.Release(ring0);
            _fx.Release(ring1);
        }

        public IEnumerator PlayFlash(Vector3 origin, float cell)
        {
            // 更壮观：双星光 + 三环冲击 + 大量星碎片 / 烟絮，覆盖约 5×5
            float dur = Mathf.Max(0.72f, _fx.BombDuration + 0.18f);
            float reach = cell * 3.1f;
            int order = BoosterFxContext.SortingOrder;

            var flash = _fx.MakeSprite("BombStarFlash", _fx.Starlight, origin, order + 14, additive: true);
            BoosterFxContext.Tint(flash, new Color(1f, 0.98f, 0.85f, 1f));
            flash.transform.localScale = Vector3.one * (cell * 0.25f);

            var flash2 = _fx.MakeSprite("BombStarFlash2", _fx.Starlight, origin, order + 13, additive: true);
            BoosterFxContext.Tint(flash2, new Color(1f, 0.75f, 0.35f, 0.95f));
            flash2.transform.localScale = Vector3.one * (cell * 0.2f);
            flash2.transform.rotation = Quaternion.Euler(0f, 0f, 45f);

            var core = _fx.MakeSprite("BombCore", _fx.Flash, origin, order + 12, additive: true);
            BoosterFxContext.Tint(core, new Color(1f, 0.95f, 0.7f, 1f));
            core.transform.localScale = Vector3.one * (cell * 0.45f);

            var ring = _fx.MakeSprite("BombRing", BoosterFxContext.GetShockRing(), origin, order + 9, additive: true);
            BoosterFxContext.Tint(ring, new Color(1f, 0.7f, 0.25f, 1f));
            ring.transform.localScale = Vector3.one * (cell * 0.45f);

            var ring2 = _fx.MakeSprite("BombRing2", BoosterFxContext.GetShockRing(), origin, order + 10, additive: true);
            BoosterFxContext.Tint(ring2, new Color(1f, 0.45f, 0.15f, 0.9f));
            ring2.transform.localScale = Vector3.one * (cell * 0.45f);

            var ring3 = _fx.MakeSprite("BombRing3", BoosterFxContext.GetShockRing(), origin, order + 8, additive: true);
            BoosterFxContext.Tint(ring3, new Color(1f, 0.9f, 0.5f, 0.75f));
            ring3.transform.localScale = Vector3.one * (cell * 0.4f);

            var heat = _fx.MakeSprite("BombHeat", _fx.Glow, origin, order + 7, additive: true);
            BoosterFxContext.Tint(heat, new Color(1f, 0.3f, 0.1f, 0.9f));
            heat.transform.localScale = Vector3.one * (cell * 0.9f);

            int burst = 28;
            for (int i = 0; i < burst; i++)
            {
                float ang = (i / (float)burst) * Mathf.PI * 2f + Random.Range(-0.1f, 0.1f);
                var dir = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f);
                var spr = (i % 3 == 0) ? _fx.BombShard : ((i & 1) == 0 ? _fx.Star : _fx.Star2);
                Color col = (i % 3 == 0)
                    ? new Color(1f, 0.45f, 0.85f, 1f)
                    : new Color(1f, 0.92f, 0.4f, 1f);
                float speed = reach * Random.Range(2.6f, 5.0f);
                _fx.SpawnFadingParticle(origin + dir * cell * 0.12f, spr,
                    cell * Random.Range(0.4f, 0.85f), col, Random.Range(0.4f, 0.65f), dir * speed, additive: true);
            }

            for (int i = 0; i < 16; i++)
            {
                float ang = Random.Range(0f, Mathf.PI * 2f);
                var dir = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f);
                _fx.SpawnFadingParticle(origin + dir * cell * Random.Range(0.05f, 0.55f), _fx.Smoke,
                    cell * Random.Range(0.7f, 1.35f),
                    new Color(1f, 0.5f, 0.2f, 0.8f),
                    Random.Range(0.45f, 0.75f),
                    dir * reach * Random.Range(0.9f, 1.9f),
                    additive: true);
            }

            // 二次爆：外圈星雨
            for (int i = 0; i < 10; i++)
            {
                float ang = Random.Range(0f, Mathf.PI * 2f);
                var dir = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f);
                _fx.SpawnFadingParticle(origin + dir * cell * Random.Range(0.8f, 1.6f), _fx.Starlight != null ? _fx.Starlight : _fx.Star,
                    cell * Random.Range(0.35f, 0.6f),
                    new Color(1f, 0.95f, 0.7f, 0.95f),
                    0.45f, dir * reach * 1.4f, additive: true);
            }

            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / dur);
                float ease = 1f - Mathf.Pow(1f - u, 2.4f);
                float punch = Mathf.Sin(Mathf.Clamp01(u * 1.25f) * Mathf.PI);

                if (flash != null)
                {
                    float fs = Mathf.Lerp(0.5f, 5.2f, punch);
                    flash.transform.localScale = Vector3.one * (cell * fs);
                    flash.transform.rotation = Quaternion.Euler(0f, 0f, u * 70f);
                    var c = flash.color;
                    c.a = u < 0.3f ? 1f : Mathf.Lerp(1f, 0f, (u - 0.3f) / 0.7f);
                    flash.color = c;
                }

                if (flash2 != null)
                {
                    float fs = Mathf.Lerp(0.4f, 4.4f, Mathf.Sin(Mathf.Clamp01(u * 1.55f) * Mathf.PI));
                    flash2.transform.localScale = Vector3.one * (cell * fs);
                    flash2.transform.rotation = Quaternion.Euler(0f, 0f, -u * 90f);
                    var c = flash2.color;
                    c.a = u < 0.25f ? 0.95f : Mathf.Lerp(0.95f, 0f, (u - 0.25f) / 0.75f);
                    flash2.color = c;
                }

                if (core != null)
                {
                    core.transform.localScale = Vector3.one * (cell * Mathf.Lerp(0.7f, 2.8f, ease));
                    var c = core.color;
                    c.a = 1f - u * 0.95f;
                    core.color = c;
                }

                if (ring != null)
                {
                    ring.transform.localScale = Vector3.one * (cell * Mathf.Lerp(0.7f, 6.2f, ease));
                    var c = ring.color;
                    c.a = 1f * (1f - u) * (1f - u);
                    ring.color = c;
                }

                if (ring2 != null)
                {
                    float u2 = Mathf.Clamp01((u - 0.08f) / 0.92f);
                    ring2.transform.localScale = Vector3.one * (cell * Mathf.Lerp(0.8f, 7.0f, 1f - Mathf.Pow(1f - u2, 2.2f)));
                    var c = ring2.color;
                    c.a = 0.9f * (1f - u2) * (1f - u2);
                    ring2.color = c;
                }

                if (ring3 != null)
                {
                    float u3 = Mathf.Clamp01((u - 0.18f) / 0.82f);
                    ring3.transform.localScale = Vector3.one * (cell * Mathf.Lerp(1.0f, 5.5f, u3));
                    var c = ring3.color;
                    c.a = 0.7f * (1f - u3);
                    ring3.color = c;
                }

                if (heat != null)
                {
                    heat.transform.localScale = Vector3.one * (cell * Mathf.Lerp(1.1f, 6.0f, ease));
                    var c = heat.color;
                    c.a = 0.85f * (1f - u);
                    heat.color = c;
                }

                if (u > 0.08f && u < 0.65f && Random.value < 0.7f)
                {
                    float ang = Random.Range(0f, Mathf.PI * 2f);
                    var dir = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f);
                    _fx.SpawnFadingParticle(origin + dir * cell * Random.Range(0.5f, 2.0f), _fx.Star,
                        cell * Random.Range(0.22f, 0.48f),
                        new Color(1f, 0.95f, 0.55f, 1f),
                        0.32f, dir * reach * 2.0f, additive: true);
                }

                yield return null;
            }

            _fx.Release(flash);
            _fx.Release(flash2);
            _fx.Release(core);
            _fx.Release(ring);
            _fx.Release(ring2);
            _fx.Release(ring3);
            _fx.Release(heat);
        }
    }
}
