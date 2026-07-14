using CandyCrush.Data;
using CandyCrush.View;
using UnityEngine;

namespace CandyCrush.Game
{
    /// <summary>Idle 时滑动交换相邻格；Busy 丢弃输入。</summary>
    public class InputController : MonoBehaviour
    {
        [SerializeField] BoardView boardView;
        [SerializeField] GameFlowController flow;
        [SerializeField] Camera worldCamera;
        [Tooltip("滑动超过格子边长的该比例才触发交换")]
        [SerializeField] float swipeThresholdRatio = 0.28f;

        bool _pressing;
        bool _swappedThisPress;
        int _startRow = -1, _startCol = -1;
        TileView _pressed;
        Vector3 _pressWorld;

        void Awake()
        {
            if (worldCamera == null) worldCamera = Camera.main;
        }

        public void Bind(BoardView board, GameFlowController gameFlow)
        {
            boardView = board;
            flow = gameFlow;
        }

        void Update()
        {
            if (boardView == null || boardView.Model == null) return;
            if (flow != null && !flow.IsIdle)
            {
                if (_pressing) CancelPress();
                return;
            }

            if (Input.GetMouseButtonDown(0))
                BeginPress();
            else if (_pressing && Input.GetMouseButton(0))
                UpdatePress();
            else if (_pressing && Input.GetMouseButtonUp(0))
                EndPress();
        }

        void BeginPress()
        {
            if (!TryPointerCell(out int row, out int col, out var world)) return;

            var view = boardView.GetView(row, col);
            if (view == null) return;

            // 行李箱不可主动发起交换
            if (view.Type == TileType.Suitcase) return;

            _pressing = true;
            _swappedThisPress = false;
            _startRow = row;
            _startCol = col;
            _pressWorld = world;
            _pressed = view;
            _pressed.CacheBaseScale();
            _pressed.SetSelected(true);
        }

        void UpdatePress()
        {
            if (_swappedThisPress || _startRow < 0) return;

            var world = PointerWorld();
            var delta = world - _pressWorld;
            float thresh = boardView.CellSizeSafe() * Mathf.Clamp(swipeThresholdRatio, 0.1f, 0.9f);
            if (delta.sqrMagnitude < thresh * thresh) return;

            int dRow = 0, dCol = 0;
            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
                dCol = delta.x > 0f ? 1 : -1;
            else
                dRow = delta.y > 0f ? -1 : 1; // 本地/世界 Y 向上，行向下增大

            TrySwipeSwap(_startRow, _startCol, _startRow + dRow, _startCol + dCol);
        }

        void EndPress()
        {
            CancelPress();
        }

        void TrySwipeSwap(int r0, int c0, int r1, int c1)
        {
            var model = boardView.Model;
            if (!model.InBounds(r1, c1)) return;

            var a = boardView.GetView(r0, c0);
            var b = boardView.GetView(r1, c1);
            if (a == null || b == null) return;

            // 行李箱不可参与任何交换（含道具）
            if (a.Type == TileType.Suitcase || b.Type == TileType.Suitcase) return;

            _swappedThisPress = true;
            ClearPressVisual();

            if (flow != null)
                flow.TrySwap(r0, c0, r1, c1);
        }

        void CancelPress()
        {
            ClearPressVisual();
            _pressing = false;
            _swappedThisPress = false;
            _startRow = _startCol = -1;
            _pressed = null;
        }

        void ClearPressVisual()
        {
            if (_pressed != null) _pressed.SetSelected(false);
            _pressed = null;
        }

        bool TryPointerCell(out int row, out int col, out Vector3 world)
        {
            world = PointerWorld();
            return boardView.TryGetCell(world, out row, out col);
        }

        Vector3 PointerWorld()
        {
            if (worldCamera == null) worldCamera = Camera.main;
            var w = worldCamera.ScreenToWorldPoint(Input.mousePosition);
            w.z = 0f;
            return w;
        }
    }
}
