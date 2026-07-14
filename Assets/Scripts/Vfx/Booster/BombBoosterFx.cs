using System.Collections;
using UnityEngine;

namespace CandyCrush.Vfx
{
    /// <summary>炸弹 5×5 爆闪表现。</summary>
    public sealed class BombBoosterFx
    {
        readonly BoosterFxContext _fx;

        public BombBoosterFx(BoosterFxContext fx) => _fx = fx;

        public IEnumerator PlayFlash(Vector3 origin, float cell)
        {
            float dur = Mathf.Max(0.45f, _fx.BombDuration);
            float reach = cell * 2.6f;
            int order = BoosterFxContext.SortingOrder;

            var flash = _fx.MakeSprite("BombStarFlash", _fx.Starlight, origin, order + 12, additive: true);
            BoosterFxContext.Tint(flash, new Color(1f, 0.95f, 0.75f, 1f));
            flash.transform.localScale = Vector3.one * (cell * 0.2f);

            var core = _fx.MakeSprite("BombCore", _fx.Flash, origin, order + 11, additive: true);
            BoosterFxContext.Tint(core, new Color(1f, 0.9f, 0.55f, 1f));
            core.transform.localScale = Vector3.one * (cell * 0.4f);

            var ring = _fx.MakeSprite("BombRing", BoosterFxContext.GetShockRing(), origin, order + 9, additive: true);
            BoosterFxContext.Tint(ring, new Color(1f, 0.55f, 0.2f, 0.95f));
            ring.transform.localScale = Vector3.one * (cell * 0.5f);

            var heat = _fx.MakeSprite("BombHeat", _fx.Glow, origin, order + 8, additive: true);
            BoosterFxContext.Tint(heat, new Color(1f, 0.35f, 0.12f, 0.85f));
            heat.transform.localScale = Vector3.one * (cell * 0.8f);

            int burst = 16;
            for (int i = 0; i < burst; i++)
            {
                float ang = (i / (float)burst) * Mathf.PI * 2f + Random.Range(-0.08f, 0.08f);
                var dir = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f);
                var spr = (i % 3 == 0) ? _fx.BombShard : ((i & 1) == 0 ? _fx.Star : _fx.Star2);
                Color col = (i % 3 == 0)
                    ? new Color(1f, 0.45f, 0.85f, 1f)
                    : new Color(1f, 0.9f, 0.45f, 1f);
                float speed = reach * Random.Range(2.4f, 4.2f);
                _fx.SpawnFadingParticle(origin + dir * cell * 0.15f, spr,
                    cell * Random.Range(0.35f, 0.7f), col, Random.Range(0.35f, 0.55f), dir * speed, additive: true);
            }

            for (int i = 0; i < 10; i++)
            {
                float ang = Random.Range(0f, Mathf.PI * 2f);
                var dir = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f);
                _fx.SpawnFadingParticle(origin + dir * cell * Random.Range(0.1f, 0.6f), _fx.Smoke,
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

                if (u > 0.12f && u < 0.55f && Random.value < 0.55f)
                {
                    float ang = Random.Range(0f, Mathf.PI * 2f);
                    var dir = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f);
                    _fx.SpawnFadingParticle(origin + dir * cell * Random.Range(0.4f, 1.5f), _fx.Star,
                        cell * Random.Range(0.2f, 0.4f),
                        new Color(1f, 0.95f, 0.6f, 0.95f),
                        0.28f, dir * reach * 1.8f, additive: true);
                }

                yield return null;
            }

            _fx.Release(flash);
            _fx.Release(core);
            _fx.Release(ring);
            _fx.Release(heat);
        }
    }
}
