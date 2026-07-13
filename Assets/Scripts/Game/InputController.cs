using CandyCrush.Data;
using CandyCrush.View;
using UnityEngine;

namespace CandyCrush.Game
{
    /// <summary>Idle 时可点选/交换；Busy 丢弃输入。</summary>
    public class InputController : MonoBehaviour
    {
        [SerializeField] BoardView boardView;
        [SerializeField] GameFlowController flow;
        [SerializeField] Camera worldCamera;

        int _selRow = -1, _selCol = -1;
        TileView _selected;

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
            if (flow != null && !flow.IsIdle) return;
            if (!Input.GetMouseButtonDown(0)) return;

            var world = worldCamera.ScreenToWorldPoint(Input.mousePosition);
            world.z = 0f;
            if (!boardView.TryGetCell(world, out int row, out int col)) return;

            var view = boardView.GetView(row, col);
            if (view == null) return;

            // 行李箱不可主动交换（只能被波及/道具）
            if (view.Type == TileType.Suitcase && _selRow < 0)
                return;

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

            int r0 = _selRow, c0 = _selCol;
            var first = boardView.GetView(r0, c0);
            ClearSelection();

            // 行李箱不可参与普通交换；道具可与行李箱交换以激活
            bool involvesSuitcase = (first != null && first.Type == TileType.Suitcase) || view.Type == TileType.Suitcase;
            bool involvesBooster = (first != null && TileTypeUtil.IsBooster(first.Type)) || TileTypeUtil.IsBooster(view.Type);
            if (involvesSuitcase && !involvesBooster) return;

            if (flow != null)
                flow.TrySwap(r0, c0, row, col);
            else
                Select(view, row, col);
        }

        void Select(TileView view, int row, int col)
        {
            _selected = view;
            _selRow = row;
            _selCol = col;
            view.CacheBaseScale();
            view.SetSelected(true);
        }

        void ClearSelection()
        {
            if (_selected != null) _selected.SetSelected(false);
            _selected = null;
            _selRow = _selCol = -1;
        }
    }
}
