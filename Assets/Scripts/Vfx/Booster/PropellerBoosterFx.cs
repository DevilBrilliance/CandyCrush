using System.Collections;
using UnityEngine;

namespace CandyCrush.Vfx
{
    /// <summary>螺旋桨十字爆发 + 追击飞行。</summary>
    public sealed class PropellerBoosterFx
    {
        readonly BoosterFxContext _fx;

        public PropellerBoosterFx(BoosterFxContext fx) => _fx = fx;

        public IEnumerator PlayCrossThenChase(Vector3 from, Vector3 to, float cell)
        {
            yield return PlayCrossBurst(from, cell);
            yield return PlayFly(from, to, cell);
        }

        public IEnumerator PlayCrossBurst(Vector3 origin, float cell)
        {
            float dur = 0.55f;
            int order = BoosterFxContext.SortingOrder + 20;

            var blur = _fx.MakeSprite("PropXBlur", _fx.LeavesXBlur != null ? _fx.LeavesXBlur : _fx.LeavesX, origin, order, additive: true);
            BoosterFxContext.Tint(blur, new Color(1f, 1f, 1f, 0.9f));
            if (blur != null) blur.transform.localScale = Vector3.one * (cell * 0.4f);

            var plus = _fx.MakeSprite("PropX", _fx.LeavesX != null ? _fx.LeavesX : _fx.XCross, origin, order + 2);
            BoosterFxContext.Tint(plus, Color.white);
            if (plus != null) plus.transform.localScale = Vector3.one * (cell * 0.35f);

            var barH = _fx.MakeSprite("PropBarH", _fx.LeavesHeng != null ? _fx.LeavesHeng : _fx.Glow, origin, order + 1);
            BoosterFxContext.Tint(barH, Color.white);
            var barV = _fx.MakeSprite("PropBarV", _fx.LeavesShu != null ? _fx.LeavesShu : _fx.Glow, origin, order + 1);
            BoosterFxContext.Tint(barV, Color.white);
            if (barH != null) BoosterFxContext.FitSpriteWorld(barH, cell * 0.4f, cell * 0.55f);
            if (barV != null) BoosterFxContext.FitSpriteWorld(barV, cell * 0.55f, cell * 0.4f);

            Vector3[] dirs = { Vector3.up, Vector3.down, Vector3.left, Vector3.right };
            var tips = new SpriteRenderer[4];
            for (int i = 0; i < 4; i++)
            {
                tips[i] = _fx.MakeSprite($"PropTip{i}", _fx.LeavesLizi != null ? _fx.LeavesLizi : _fx.Star, origin, order + 3);
                BoosterFxContext.Tint(tips[i], Color.white);
                if (tips[i] != null) tips[i].transform.localScale = Vector3.one * (cell * 0.45f);
            }

            var plane = _fx.MakeSprite("PropPulse", _fx.Propeller != null ? _fx.Propeller : _fx.Star, origin, order + 4);
            BoosterFxContext.Tint(plane, Color.white);
            if (plane != null) plane.transform.localScale = Vector3.one * (cell * 0.9f);

            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / dur);
                float ease = 1f - Mathf.Pow(1f - u, 2.4f);
                float pulse = Mathf.Sin(u * Mathf.PI);

                float arm = cell * Mathf.Lerp(0.35f, 3.05f, ease);
                float thick = cell * Mathf.Lerp(0.55f, 0.85f, pulse);

                if (barH != null)
                {
                    barH.transform.position = origin;
                    BoosterFxContext.FitSpriteWorld(barH, arm, thick);
                    var c = barH.color;
                    c.a = u < 0.7f ? 1f : Mathf.Lerp(1f, 0f, (u - 0.7f) / 0.3f);
                    barH.color = c;
                }
                if (barV != null)
                {
                    barV.transform.position = origin;
                    BoosterFxContext.FitSpriteWorld(barV, thick, arm);
                    var c = barV.color;
                    c.a = u < 0.7f ? 1f : Mathf.Lerp(1f, 0f, (u - 0.7f) / 0.3f);
                    barV.color = c;
                }

                if (plus != null)
                {
                    float ps = cell * Mathf.Lerp(0.5f, 1.8f, Mathf.Sin(Mathf.Clamp01(u * 1.2f) * Mathf.PI));
                    plus.transform.localScale = Vector3.one * ps;
                    plus.transform.rotation = Quaternion.Euler(0f, 0f, u * 25f);
                    var c = plus.color;
                    c.a = u < 0.55f ? 1f : Mathf.Lerp(1f, 0f, (u - 0.55f) / 0.45f);
                    plus.color = c;
                }
                if (blur != null)
                {
                    blur.transform.localScale = Vector3.one * (cell * Mathf.Lerp(0.6f, 2.8f, ease));
                    var c = blur.color;
                    c.a = 0.85f * (1f - u);
                    blur.color = c;
                }
                if (plane != null)
                {
                    plane.transform.localScale = Vector3.one * (cell * (0.85f + 0.35f * pulse));
                    plane.transform.rotation = Quaternion.Euler(0f, 0f, u * 180f);
                    var c = plane.color;
                    c.a = 1f - u * 0.85f;
                    plane.color = c;
                }

                for (int i = 0; i < 4; i++)
                {
                    if (tips[i] == null) continue;
                    float d = cell * Mathf.Lerp(0.15f, 1.05f, ease);
                    tips[i].transform.position = origin + dirs[i] * d;
                    tips[i].transform.localScale = Vector3.one * (cell * (0.4f + 0.25f * pulse));
                    var c = tips[i].color;
                    c.a = 1f - u * 0.6f;
                    tips[i].color = c;
                }

                if (u > 0.15f && u < 0.55f && Random.value < 0.55f)
                {
                    int di = Random.Range(0, 4);
                    _fx.SpawnFadingParticle(origin + dirs[di] * cell * Random.Range(0.2f, 0.9f),
                        _fx.LeavesLizi != null ? _fx.LeavesLizi : _fx.Star,
                        cell * Random.Range(0.25f, 0.45f),
                        Color.white, 0.28f, dirs[di] * cell * 2.5f);
                }

                yield return null;
            }

            _fx.Release(blur);
            _fx.Release(plus);
            _fx.Release(barH);
            _fx.Release(barV);
            _fx.Release(plane);
            for (int i = 0; i < tips.Length; i++)
                _fx.Release(tips[i]);
        }

        public IEnumerator PlayFly(Vector3 from, Vector3 to, float cell)
        {
            float dur = Mathf.Max(0.35f, _fx.PropellerDuration - 0.55f);
            var spr = _fx.Propeller != null ? _fx.Propeller : _fx.Star;
            int order = BoosterFxContext.SortingOrder;
            var flyer = _fx.MakeSprite("PropellerFly", spr, from, order + 25);
            flyer.transform.localScale = Vector3.one * (cell * 0.95f);
            BoosterFxContext.Tint(flyer, Color.white);

            var glow = _fx.MakeSprite("PropellerGlow", _fx.Glow, from, order + 24, additive: true);
            glow.transform.localScale = Vector3.one * (cell * 1.2f);
            BoosterFxContext.Tint(glow, new Color(1f, 0.85f, 0.35f, 0.9f));

            Vector3 mid = Vector3.Lerp(from, to, 0.45f);
            mid.y += cell * 1.25f;

            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / dur);
                float ease = u * u * (3f - 2f * u);
                var pos = BoosterFxContext.QuadBezier(from, mid, to, ease);
                if (flyer != null)
                {
                    flyer.transform.position = pos;
                    flyer.transform.rotation = Quaternion.Euler(0f, 0f, u * 720f);
                    flyer.transform.localScale = Vector3.one * (cell * (0.9f + 0.2f * Mathf.Sin(u * Mathf.PI)));
                }
                if (glow != null)
                {
                    glow.transform.position = pos;
                    var c = glow.color;
                    c.a = 0.75f * (1f - u * 0.35f);
                    glow.color = c;
                }

                if (Random.value < 0.7f)
                    _fx.SpawnFadingParticle(pos, _fx.LeavesLizi != null ? _fx.LeavesLizi : _fx.Flower, cell * 0.3f, Color.white, 0.25f);

                yield return null;
            }

            _fx.SpawnFadingParticle(to, _fx.Flash, cell * 1.3f, new Color(1f, 0.9f, 0.4f, 1f), 0.24f, additive: true);
            _fx.SpawnFadingParticle(to, _fx.LeavesX != null ? _fx.LeavesX : _fx.XCross, cell * 1.2f, Color.white, 0.22f);
            _fx.Release(flyer);
            _fx.Release(glow);
        }
    }
}
