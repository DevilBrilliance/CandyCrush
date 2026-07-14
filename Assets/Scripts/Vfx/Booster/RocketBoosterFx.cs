using System.Collections;
using CandyCrush.Core;
using CandyCrush.View;
using UnityEngine;

namespace CandyCrush.Vfx
{
    /// <summary>火箭行/列扫射表现。</summary>
    public sealed class RocketBoosterFx
    {
        readonly BoosterFxContext _fx;

        public RocketBoosterFx(BoosterFxContext fx) => _fx = fx;

        public IEnumerator PlaySweep(GridPos originCell, bool horizontal, BoardView board)
        {
            float cell = board.CellSizeSafe();
            float dur = Mathf.Max(0.28f, _fx.RocketDuration);
            int row = originCell.Row;
            int col = originCell.Col;
            int rows = board.Model.Rows;
            int cols = board.Model.Cols;

            Vector3 origin = board.transform.TransformPoint(board.CellLocal(row, col));
            Vector3 endA, endB;
            if (horizontal)
            {
                endA = board.transform.TransformPoint(board.CellLocal(row, cols - 1));
                endB = board.transform.TransformPoint(board.CellLocal(row, 0));
            }
            else
            {
                endA = board.transform.TransformPoint(board.CellLocal(0, col));
                endB = board.transform.TransformPoint(board.CellLocal(rows - 1, col));
            }

            Vector3 dirA = endA - origin;
            Vector3 dirB = endB - origin;
            float lenA = dirA.magnitude;
            float lenB = dirB.magnitude;
            if (lenA > 0.0001f) dirA /= lenA; else dirA = horizontal ? Vector3.right : Vector3.up;
            if (lenB > 0.0001f) dirB /= lenB; else dirB = -dirA;

            _fx.SpawnFadingParticle(origin, _fx.Flash, cell * 1.6f, new Color(1f, 0.95f, 0.55f, 1f), 0.28f, additive: true);
            _fx.SpawnFadingParticle(origin, _fx.Starlight, cell * 2.2f, new Color(1f, 0.85f, 0.35f, 1f), 0.32f, additive: true);

            int order = BoosterFxContext.SortingOrder;
            var beamSoft = _fx.MakeSprite("RocketBeamSoft", _fx.Glow, origin, order + 10, additive: true);
            BoosterFxContext.Tint(beamSoft, new Color(1f, 0.75f, 0.25f, 0.95f));
            var beamCore = _fx.MakeSprite("RocketBeamCore", _fx.Flash, origin, order + 11, additive: true);
            BoosterFxContext.Tint(beamCore, new Color(1f, 0.98f, 0.85f, 1f));

            var headA = _fx.MakeSprite("RocketHeadA", _fx.ArrowUp, origin, order + 13);
            var headB = _fx.MakeSprite("RocketHeadB", _fx.ArrowDown, origin, order + 13);
            if (horizontal)
            {
                if (headA != null) headA.transform.rotation = Quaternion.Euler(0f, 0f, -90f);
                if (headB != null)
                {
                    headB.sprite = _fx.ArrowUp;
                    headB.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
                }
            }
            else
            {
                if (headA != null) { headA.sprite = _fx.ArrowUp; headA.transform.rotation = Quaternion.identity; }
                if (headB != null) { headB.sprite = _fx.ArrowDown; headB.transform.rotation = Quaternion.identity; }
            }

            float headScale = cell * 0.95f;
            if (headA != null) { BoosterFxContext.Tint(headA, Color.white); headA.transform.localScale = Vector3.one * headScale; }
            if (headB != null) { BoosterFxContext.Tint(headB, Color.white); headB.transform.localScale = Vector3.one * headScale; }

            var glowA = _fx.MakeSprite("RocketGlowA", _fx.Glow, origin, order + 12, additive: true);
            var glowB = _fx.MakeSprite("RocketGlowB", _fx.Glow, origin, order + 12, additive: true);
            BoosterFxContext.Tint(glowA, new Color(1f, 0.7f, 0.2f, 1f));
            BoosterFxContext.Tint(glowB, new Color(1f, 0.7f, 0.2f, 1f));
            if (glowA != null) glowA.transform.localScale = Vector3.one * (cell * 1.1f);
            if (glowB != null) glowB.transform.localScale = Vector3.one * (cell * 1.1f);

            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / dur);
                float ease = 1f - Mathf.Pow(1f - u, 2.4f);
                float pulse = 0.55f + 0.45f * Mathf.Sin(u * Mathf.PI);

                Vector3 posA = Vector3.Lerp(origin, endA, ease);
                Vector3 posB = Vector3.Lerp(origin, endB, ease);
                if (headA != null) headA.transform.position = posA;
                if (headB != null) headB.transform.position = posB;
                if (glowA != null)
                {
                    glowA.transform.position = posA;
                    glowA.transform.localScale = Vector3.one * (cell * (1.0f + 0.35f * pulse));
                }
                if (glowB != null)
                {
                    glowB.transform.position = posB;
                    glowB.transform.localScale = Vector3.one * (cell * (1.0f + 0.35f * pulse));
                }

                Vector3 mid = (posA + posB) * 0.5f;
                float len = Vector3.Distance(posA, posB) + cell * 0.5f;
                float thickSoft = cell * (0.85f + 0.35f * pulse);
                float thickCore = cell * (0.38f + 0.18f * pulse);
                if (beamSoft != null)
                {
                    beamSoft.transform.position = mid;
                    beamSoft.transform.localScale = horizontal
                        ? new Vector3(len, thickSoft, 1f)
                        : new Vector3(thickSoft, len, 1f);
                    var c = beamSoft.color;
                    c.a = 0.95f * (1f - u * 0.25f);
                    beamSoft.color = c;
                }
                if (beamCore != null)
                {
                    beamCore.transform.position = mid;
                    beamCore.transform.localScale = horizontal
                        ? new Vector3(len, thickCore, 1f)
                        : new Vector3(thickCore, len, 1f);
                    var c = beamCore.color;
                    c.a = 1f * (1f - u * 0.35f);
                    beamCore.color = c;
                }

                if (Random.value < 0.85f)
                {
                    _fx.SpawnFadingParticle(posA, _fx.Star, cell * Random.Range(0.28f, 0.5f),
                        new Color(1f, 0.95f, 0.45f, 1f), 0.28f, dirA * cell * 2.2f, additive: true);
                    _fx.SpawnFadingParticle(posB, _fx.Star, cell * Random.Range(0.28f, 0.5f),
                        new Color(1f, 0.95f, 0.45f, 1f), 0.28f, dirB * cell * 2.2f, additive: true);
                }
                if (Random.value < 0.55f)
                {
                    _fx.SpawnFadingParticle(posA, _fx.Smoke, cell * 0.55f,
                        new Color(1f, 0.65f, 0.25f, 0.85f), 0.32f, additive: true);
                    _fx.SpawnFadingParticle(posB, _fx.Smoke, cell * 0.55f,
                        new Color(1f, 0.65f, 0.25f, 0.85f), 0.32f, additive: true);
                }

                yield return null;
            }

            _fx.SpawnFadingParticle((endA + endB) * 0.5f, _fx.Flash, cell * 1.4f,
                new Color(1f, 0.95f, 0.6f, 1f), 0.22f, additive: true);

            _fx.Release(headA);
            _fx.Release(headB);
            _fx.Release(glowA);
            _fx.Release(glowB);
            _fx.Release(beamSoft);
            _fx.Release(beamCore);
        }
    }
}
