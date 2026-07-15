using UnityEngine;
using UnityEngine.Rendering;

namespace CandyCrush.Vfx
{
    /// <summary>
    /// 全屏 Shader 大气：雨丝在棋盘下，飘雪在最上层。
    /// 观感对齐参考视频与旧粒子版（长细斜雨 + 软絮棉团雪）。
    /// sorting：夜景(-20) &lt; 雨(-18) &lt; 棋盘0 &lt; 棋子10 &lt; … &lt; 雪(900)
    /// </summary>
    public class AtmosphereFx : MonoBehaviour
    {
        public const int RainSortingOrder = -18;
        public const int SnowSortingOrder = 900;

        [SerializeField] MeshRenderer rainRenderer;
        [SerializeField] MeshRenderer snowRenderer;
        [SerializeField] bool playOnEnable = true;

        Camera _cam;
        Transform _rainTf;
        Transform _snowTf;
        Material _rainMat;
        Material _snowMat;
        bool _playing = true;

        static readonly int ColorId = Shader.PropertyToID("_Color");

        public void Play()
        {
            _playing = true;
            SetVisible(true);
        }

        public void Stop()
        {
            _playing = false;
            SetVisible(false);
        }

        public static AtmosphereFx CreateDefault(Transform parent)
        {
            if (parent != null)
            {
                var old = parent.GetComponentInChildren<AtmosphereFx>(true);
                if (old != null)
                {
                    if (Application.isPlaying) Object.Destroy(old.gameObject);
                    else Object.DestroyImmediate(old.gameObject);
                }
            }

            int ignore = LayerMask.NameToLayer("Ignore Raycast");
            var root = new GameObject("AtmosphereFx");
            root.transform.SetParent(parent, false);
            if (ignore >= 0) root.layer = ignore;

            var fx = root.AddComponent<AtmosphereFx>();
            fx.Build(ignore);
            fx.Play();
            return fx;
        }

        void OnEnable()
        {
            if (playOnEnable) Play();
        }

        void LateUpdate()
        {
            if (!_playing) return;
            FitToCamera();
        }

        void Build(int ignoreLayer)
        {
            var rainShader = Shader.Find("CandyCrush/AtmosphereRain");
            var snowShader = Shader.Find("CandyCrush/AtmosphereSnow");
            if (rainShader == null || snowShader == null)
            {
                Debug.LogError("[AtmosphereFx] Missing shaders CandyCrush/AtmosphereRain or AtmosphereSnow.");
                return;
            }

            _rainMat = new Material(rainShader)
            {
                name = "AtmRainMat",
                hideFlags = HideFlags.HideAndDontSave
            };
            _snowMat = new Material(snowShader)
            {
                name = "AtmSnowMat",
                hideFlags = HideFlags.HideAndDontSave
            };

            rainRenderer = CreateLayer("RainLayer", ignoreLayer, _rainMat, RainSortingOrder, out _rainTf);
            snowRenderer = CreateLayer("SnowLayer", ignoreLayer, _snowMat, SnowSortingOrder, out _snowTf);

            ApplyLook();
        }

        void ApplyLook()
        {
            // 雨更疏、条更长；雪絮保持软团，略减细雪抢戏
            if (_rainMat.HasProperty(ColorId))
                _rainMat.SetColor(ColorId, new Color(0.88f, 0.94f, 1f, 0.85f));
            _rainMat.SetFloat("_Density", 18f);
            _rainMat.SetFloat("_Speed", 1.35f);
            _rainMat.SetFloat("_Length", 0.5f);
            _rainMat.SetFloat("_Thickness", 0.011f);
            _rainMat.SetFloat("_Angle", -24f);
            _rainMat.SetFloat("_Opacity", 0.48f);

            if (_snowMat.HasProperty(ColorId))
                _snowMat.SetColor(ColorId, new Color(1f, 1f, 1f, 1f));
            _snowMat.SetFloat("_FluffDensity", 6.2f);
            _snowMat.SetFloat("_FineDensity", 12f);
            _snowMat.SetFloat("_FluffSpeed", 0.52f);
            _snowMat.SetFloat("_FineSpeed", 0.85f);
            _snowMat.SetFloat("_FluffSize", 0.12f);
            _snowMat.SetFloat("_FineSize", 0.038f);
            _snowMat.SetFloat("_Drift", 0.3f);
            _snowMat.SetFloat("_FluffOpacity", 0.88f);
            _snowMat.SetFloat("_FineOpacity", 0.55f);
        }

        MeshRenderer CreateLayer(string name, int ignoreLayer, Material mat, int sortingOrder, out Transform tf)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            if (ignoreLayer >= 0) go.layer = ignoreLayer;
            tf = go.transform;

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = QuadMesh();

            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.lightProbeUsage = LightProbeUsage.Off;
            mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
            mr.allowOcclusionWhenDynamic = false;
            mr.sortingLayerName = "Default";
            mr.sortingOrder = sortingOrder;
            return mr;
        }

        void FitToCamera()
        {
            if (_cam == null) _cam = Camera.main;
            if (_cam == null || !_cam.orthographic) return;

            float h = _cam.orthographicSize * 2f * 1.05f;
            float w = h * _cam.aspect * 1.05f;
            var center = _cam.transform.position;
            if (_rainTf != null)
            {
                _rainTf.position = new Vector3(center.x, center.y, 2f);
                _rainTf.rotation = Quaternion.identity;
                _rainTf.localScale = new Vector3(w, h, 1f);
            }
            if (_snowTf != null)
            {
                _snowTf.position = new Vector3(center.x, center.y, -1f);
                _snowTf.rotation = Quaternion.identity;
                _snowTf.localScale = new Vector3(w, h, 1f);
            }
        }

        void SetVisible(bool on)
        {
            if (rainRenderer != null) rainRenderer.enabled = on;
            if (snowRenderer != null) snowRenderer.enabled = on;
        }

        static Mesh _quad;

        static Mesh QuadMesh()
        {
            if (_quad != null) return _quad;
            _quad = new Mesh
            {
                name = "AtmosphereFullscreenQuad",
                hideFlags = HideFlags.HideAndDontSave
            };
            _quad.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3(0.5f, -0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f)
            };
            _quad.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f)
            };
            _quad.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            _quad.RecalculateBounds();
            return _quad;
        }

        void OnDestroy()
        {
            if (_rainMat != null) Destroy(_rainMat);
            if (_snowMat != null) Destroy(_snowMat);
        }
    }
}
