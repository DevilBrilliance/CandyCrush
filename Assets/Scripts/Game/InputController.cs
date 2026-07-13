using CandyCrush.View;
using UnityEngine;

namespace CandyCrush.Game
{
    /// <summary>输入骨架：点击选中格子（非 Idle 时可扩展丢弃）。</summary>
    public class InputController : MonoBehaviour
    {
        [SerializeField] BoardView boardView;
        [SerializeField] Camera worldCamera;

        int _selRow = -1, _selCol = -1;
        TileView _selected;

        void Awake()
        {
            if (worldCamera == null) worldCamera = Camera.main;
        }

        void Update()
        {
            if (boardView == null || boardView.Model == null) return;
            if (!Input.GetMouseButtonDown(0)) return;

            var world = worldCamera.ScreenToWorldPoint(Input.mousePosition);
            world.z = 0f;
            if (!boardView.TryGetCell(world, out int row, out int col)) return;

            var view = boardView.GetView(row, col);
            if (view == null) return;

            if (_selRow < 0)
            {
                Select(view, row, col);
                return;
            }

            if (_selRow == row && _selCol == col)
            {
                ClearSelection();
                return;
            }

            // 相邻交换留给后续行为层；当前仅高亮切换
            ClearSelection();
            Select(view, row, col);
        }

        void Select(TileView view, int row, int col)
        {
            _selected = view;
            _selRow = row;
            _selCol = col;
            view.SetSelected(true);
        }

        void ClearSelection()
        {
            if (_selected != null) _selected.SetSelected(false);
            _selected = null;
            _selRow = _selCol = -1;
        }

        public void Bind(BoardView board) => boardView = board;
    }
}
