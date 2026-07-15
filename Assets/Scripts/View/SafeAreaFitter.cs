using UnityEngine;

namespace CandyCrush.View
{
    /// <summary>把 RectTransform 锚到 Screen.safeArea，避开刘海 / 底部横条（竖屏）。</summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        RectTransform _rt;
        Rect _lastSafe;
        Vector2Int _lastScreen;

        void Awake()
        {
            _rt = (RectTransform)transform;
            Apply();
        }

        void OnEnable() => Apply();

        void Update()
        {
            if (Screen.safeArea == _lastSafe &&
                Screen.width == _lastScreen.x &&
                Screen.height == _lastScreen.y)
                return;
            Apply();
        }

        public void Apply()
        {
            if (_rt == null) _rt = (RectTransform)transform;
            _lastSafe = Screen.safeArea;
            _lastScreen = new Vector2Int(Screen.width, Screen.height);

            float w = Mathf.Max(1f, Screen.width);
            float h = Mathf.Max(1f, Screen.height);
            var sa = _lastSafe;

            _rt.anchorMin = new Vector2(sa.xMin / w, sa.yMin / h);
            _rt.anchorMax = new Vector2(sa.xMax / w, sa.yMax / h);
            _rt.offsetMin = Vector2.zero;
            _rt.offsetMax = Vector2.zero;
            _rt.localScale = Vector3.one;
        }
    }
}
