using System.Collections;
using CandyCrush.Common;
using CandyCrush.View;
using UnityEngine;
using UnityEngine.UI;

namespace CandyCrush.Vfx
{
    /// <summary>胜利结算：Great 弹出 + 星光粒子。挂在 WinPanel 上或由 WinPanel 触发。</summary>
    public class SettleFx : MonoBehaviour
    {
        [SerializeField] RectTransform greatText;
        [SerializeField] float punchDuration = 0.55f;
        [SerializeField] int starCount = 18;

        Sprite _star;
        Sprite _glow;
        static Texture2D _dotTex;

        void OnEnable() => EventBus.Subscribe<LevelWinEvent>(OnWin);
        void OnDisable() => EventBus.Unsubscribe<LevelWinEvent>(OnWin);

        void Awake()
        {
            if (greatText == null)
            {
                var t = transform.Find("Visual/GreatText") ?? transform.Find("GreatText");
                if (t != null) greatText = t as RectTransform;
            }
            _star = Resources.Load<Sprite>("Vfx/Booster/particle_die_star_1")
                    ?? Resources.Load<Sprite>("Vfx/Booster/particle_die_star_2");
            _glow = Resources.Load<Sprite>("Vfx/Booster/efx_candy_27")
                    ?? Resources.Load<Sprite>("Vfx/Booster/UIpanel_starlight");
            if (_star == null)
            {
                var tex = Resources.Load<Texture2D>("Vfx/Booster/particle_die_star_1");
                if (tex != null)
                    _star = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            }
        }

        public void BindGreat(RectTransform great) => greatText = great;

        void OnWin(LevelWinEvent _)
        {
            var win = GetComponent<WinPanel>();
            if (win != null) win.Show();
            StopAllCoroutines();
            StartCoroutine(Play());
        }

        public IEnumerator Play()
        {
            yield return null; // 等 WinPanel 把 Visual 打开
            if (greatText != null)
                yield return PunchGreat(greatText);
            yield return BurstStars();
        }

        IEnumerator PunchGreat(RectTransform target)
        {
            var baseScale = Vector3.one;
            target.localScale = Vector3.one * 0.35f;
            var text = target.GetComponent<Text>();
            Color baseColor = text != null ? text.color : Color.white;

            float t = 0f;
            float dur = punchDuration;
            while (t < dur)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / dur);
                // overshoot
                float s = u < 0.55f
                    ? Mathf.Lerp(0.35f, 1.25f, u / 0.55f)
                    : Mathf.Lerp(1.25f, 1f, (u - 0.55f) / 0.45f);
                target.localScale = baseScale * s;
                if (text != null)
                {
                    var c = baseColor;
                    c.a = Mathf.Clamp01(u * 1.5f);
                    text.color = c;
                }
                yield return null;
            }
            target.localScale = baseScale;
        }

        IEnumerator BurstStars()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) yield break;
            var root = canvas.rootCanvas.transform as RectTransform;
            if (root == null) yield break;

            Vector2 center = Vector2.zero;
            if (greatText != null)
            {
                var screen = RectTransformUtility.WorldToScreenPoint(null, greatText.position);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, null, out center);
            }

            var layer = EnsureLayer(root);
            int count = Mathf.Max(8, starCount);
            for (int i = 0; i < count; i++)
            {
                float ang = (i / (float)count) * Mathf.PI * 2f + Random.Range(-0.1f, 0.1f);
                var dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                StartCoroutine(FlyStar(layer, center, dir));
            }

            // 中心光
            StartCoroutine(CenterFlash(layer, center));

            float wait = 0.85f;
            float t = 0f;
            while (t < wait)
            {
                t += Time.deltaTime;
                yield return null;
            }
        }

        IEnumerator CenterFlash(RectTransform layer, Vector2 center)
        {
            var go = new GameObject("WinFlash", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(layer, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = center;
            rt.sizeDelta = new Vector2(40f, 40f);
            var img = go.GetComponent<Image>();
            img.sprite = _glow != null ? _glow : MakeUiDot();
            img.raycastTarget = false;
            img.color = new Color(1f, 0.92f, 0.45f, 0.95f);

            float t = 0f;
            while (t < 0.45f)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / 0.45f);
                rt.sizeDelta = Vector2.one * Mathf.Lerp(40f, 520f, u);
                var c = img.color;
                c.a = 0.9f * (1f - u);
                img.color = c;
                yield return null;
            }
            Destroy(go);
        }

        IEnumerator FlyStar(RectTransform layer, Vector2 center, Vector2 dir)
        {
            var go = new GameObject("WinStar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(layer, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = center;
            float size = Random.Range(28f, 52f);
            rt.sizeDelta = new Vector2(size, size);
            var img = go.GetComponent<Image>();
            img.sprite = _star != null ? _star : MakeUiDot();
            img.raycastTarget = false;
            img.color = Color.HSVToRGB(Random.Range(0.08f, 0.18f), 0.55f, 1f);

            float dist = Random.Range(180f, 420f);
            float dur = Random.Range(0.55f, 0.9f);
            float spin = Random.Range(-360f, 360f);
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / dur);
                float ease = 1f - Mathf.Pow(1f - u, 2f);
                rt.anchoredPosition = center + dir * (dist * ease);
                rt.localRotation = Quaternion.Euler(0f, 0f, spin * u);
                float s = 1f + 0.35f * Mathf.Sin(u * Mathf.PI);
                rt.localScale = Vector3.one * s * (1f - u * 0.35f);
                var c = img.color;
                c.a = 1f - u;
                img.color = c;
                yield return null;
            }
            Destroy(go);
        }

        static RectTransform EnsureLayer(RectTransform canvasRt)
        {
            const string name = "SettleFxLayer";
            var existing = canvasRt.Find(name) as RectTransform;
            if (existing != null)
            {
                existing.SetAsLastSibling();
                return existing;
            }

            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(canvasRt, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var canvas = go.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 600;
            go.AddComponent<GraphicRaycaster>().enabled = false;
            return rt;
        }

        static Sprite MakeUiDot()
        {
            if (_dotTex == null)
            {
                const int size = 32;
                _dotTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                var px = new Color32[size * size];
                float r = size * 0.5f;
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - r;
                    float dy = y + 0.5f - r;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / r;
                    byte a = d >= 1f ? (byte)0 : (byte)Mathf.Clamp(Mathf.RoundToInt((1f - d * d) * 255f), 0, 255);
                    px[y * size + x] = new Color32(255, 255, 255, a);
                }
                _dotTex.SetPixels32(px);
                _dotTex.Apply(false, true);
            }
            return Sprite.Create(_dotTex, new Rect(0, 0, _dotTex.width, _dotTex.height), new Vector2(0.5f, 0.5f), 32f);
        }
    }
}
