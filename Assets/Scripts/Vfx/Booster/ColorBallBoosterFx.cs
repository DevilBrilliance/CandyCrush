using System.Collections;
using UnityEngine;

namespace CandyCrush.Vfx
{
    /// <summary>彩球同色消除涟漪。</summary>
    public sealed class ColorBallBoosterFx
    {
        readonly BoosterFxContext _fx;

        public ColorBallBoosterFx(BoosterFxContext fx) => _fx = fx;

        public IEnumerator PlayPulse(Vector3 origin, float cell)
        {
            float dur = Mathf.Max(0.15f, _fx.ColorBallDuration);
            var orb = _fx.MakeSprite("ColorBall", _fx.Rainbow, origin, BoosterFxContext.SortingOrder + 6);
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
                    _fx.SpawnFadingParticle(origin + (Vector3)(Random.insideUnitCircle * cell), _fx.Dot, cell * 0.25f,
                        Color.HSVToRGB(Random.value, 0.7f, 1f), 0.25f);
                yield return null;
            }

            _fx.Release(orb);
        }
    }
}
