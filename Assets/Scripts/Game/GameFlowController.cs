using System.Collections;
using CandyCrush.Common;
using CandyCrush.Core;
using CandyCrush.Data;
using CandyCrush.View;
using UnityEngine;

namespace CandyCrush.Game
{
    public enum GameFlowState
    {
        Idle,
        Busy,
        Won
    }

    /// <summary>串联：输入 → 交换校验 → 连锁结算 → 等表现 → Idle / 胜利。</summary>
    public class GameFlowController : MonoBehaviour
    {
        [SerializeField] BoardView boardView;
        [SerializeField] GoalHUD goalHud;
        [SerializeField] WinPanel winPanel;

        CascadeResolver _cascade;
        ObjectiveTracker _objective;
        bool _enableColorBall;

        public GameFlowState State { get; private set; } = GameFlowState.Idle;
        public bool IsIdle => State == GameFlowState.Idle;
        public ObjectiveTracker Objective => _objective;

        public void Bind(BoardView board, GoalHUD hud, WinPanel win)
        {
            boardView = board;
            goalHud = hud;
            winPanel = win;
        }

        public void BeginLevel(LevelConfig config, int suitcaseOnBoard)
        {
            int target = config.objectiveCount > 0
                ? Mathf.Min(config.objectiveCount, suitcaseOnBoard)
                : suitcaseOnBoard;

            _objective = new ObjectiveTracker(config.objectiveType, target);
            _cascade = new CascadeResolver(config, _objective);
            _enableColorBall = config.enableColorBall;
            State = GameFlowState.Idle;

            if (goalHud != null) goalHud.SetRemaining(_objective.Remaining);
            if (winPanel != null) winPanel.Hide();

            // 开局若已有匹配则自动消一轮
            StartCoroutine(ResolveBoardRoutine(null));
        }

        public bool TrySwap(int r0, int c0, int r1, int c1)
        {
            if (!IsIdle || boardView == null || boardView.Model == null) return false;
            if (!SwapValidator.AreAdjacent(r0, c0, r1, c1)) return false;

            var board = boardView.Model;
            var a = board.Get(r0, c0);
            var b = board.Get(r1, c1);
            if (a == TileType.Empty || b == TileType.Empty) return false;

            bool boosterSwap = TileTypeUtil.IsBooster(a) || TileTypeUtil.IsBooster(b);
            bool valid = boosterSwap || SwapValidator.WouldCreateMatch(board, r0, c0, r1, c1, _enableColorBall);

            StartCoroutine(SwapRoutine(r0, c0, r1, c1, valid));
            return true;
        }

        IEnumerator SwapRoutine(int r0, int c0, int r1, int c1, bool valid)
        {
            State = GameFlowState.Busy;

            if (!valid)
            {
                yield return boardView.AnimateSwap(r0, c0, r1, c1);
                yield return boardView.AnimateSwapBack(r0, c0, r1, c1);
                State = GameFlowState.Idle;
                yield break;
            }

            boardView.Model.Swap(r0, c0, r1, c1);
            yield return boardView.AnimateSwap(r0, c0, r1, c1);

            yield return ResolveBoardRoutine((r0, c0, r1, c1));
        }

        IEnumerator ResolveBoardRoutine((int r0, int c0, int r1, int c1)? swapOrigin)
        {
            State = GameFlowState.Busy;
            if (_cascade == null)
                _cascade = new CascadeResolver(boardView.Config, _objective);
            _cascade.BeginResolve();

            bool first = true;
            while (true)
            {
                CascadeStepResult step;
                if (first && swapOrigin.HasValue)
                {
                    var s = swapOrigin.Value;
                    step = _cascade.StepAfterSwap(boardView.Model, s.r0, s.c0, s.r1, s.c1);
                    first = false;
                }
                else
                {
                    first = false;
                    step = _cascade.StepMatches(boardView.Model);
                }

                if (!step.HadWork) break;

                if (step.CollectedSuitcases.Count > 0)
                {
                    EventBus.Publish(new ObjectiveChangedEvent(_objective.Remaining));
                    if (goalHud != null && boardView != null)
                    {
                        // 格心立刻摘掉箱子视图，副本飞向 UI；连锁继续
                        boardView.ConsumeViews(step.CollectedSuitcases);
                        yield return CandyCrush.Vfx.CollectFx.FlySuitcases(
                            step.CollectedSuitcases,
                            boardView,
                            goalHud,
                            boardView.Catalog,
                            _objective.Remaining);
                    }
                }

                yield return boardView.PlayCascadeStep(step);

                if (_objective.IsComplete)
                {
                    boardView.SyncFromModel();
                    State = GameFlowState.Won;
                    EventBus.Publish(new LevelWinEvent(_objective.Remaining));
                    if (winPanel != null) winPanel.Show();
                    yield break;
                }
            }

            // 兜底：再压实一次 + 增量对齐视图（非整盘重建）
            EnsureBoardCompact();
            boardView.SyncFromModel();
            State = GameFlowState.Idle;
        }

        void EnsureBoardCompact()
        {
            if (boardView == null || boardView.Model == null || boardView.Config == null) return;
            GravitySystem.ApplyGravity(boardView.Model);
            TileSpawner.FillEmpties(boardView.Model, boardView.Config.spawnWeights, null);
        }
    }
}
