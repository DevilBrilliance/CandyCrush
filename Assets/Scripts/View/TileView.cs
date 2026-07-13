using CandyCrush.Data;
using UnityEngine;

namespace CandyCrush.View
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class TileView : MonoBehaviour
    {
        public TileType Type { get; private set; }
        public int Row { get; private set; }
        public int Col { get; private set; }

        SpriteRenderer _sr;
        float _baseScale = 1f;

        public SpriteRenderer Renderer
        {
            get
            {
                if (_sr == null) _sr = GetComponent<SpriteRenderer>();
                return _sr;
            }
        }

        void Awake() => _sr = GetComponent<SpriteRenderer>();

        public void Setup(TileType type, Sprite sprite, int row, int col, float cellSize)
        {
            Type = type;
            Row = row;
            Col = col;
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            _sr.sprite = sprite;
            _sr.sortingOrder = 10;
            SetAlpha(1f);
            FitToCell(cellSize);
            CacheBaseScale();
            gameObject.name = $"Tile_{row}_{col}_{type}";
        }

        public void SetGridPos(int row, int col)
        {
            Row = row;
            Col = col;
        }

        public void FitToCell(float cellSize)
        {
            if (_sr == null || _sr.sprite == null) return;
            var size = _sr.sprite.bounds.size;
            float max = Mathf.Max(size.x, size.y);
            if (max <= 0.0001f) return;
            float scale = cellSize * 0.88f / max;
            transform.localScale = Vector3.one * scale;
        }

        public void CacheBaseScale() => _baseScale = transform.localScale.x;

        public void SetSelected(bool selected)
        {
            if (_baseScale <= 0.0001f) CacheBaseScale();
            transform.localScale = Vector3.one * (_baseScale * (selected ? 1.1f : 1f));
        }

        public void SetAlpha(float a)
        {
            if (_sr == null) return;
            var c = _sr.color;
            c.a = a;
            _sr.color = c;
        }

        public void RestoreVisual()
        {
            SetAlpha(1f);
            if (_baseScale > 0.0001f)
                transform.localScale = Vector3.one * _baseScale;
        }
    }
}
