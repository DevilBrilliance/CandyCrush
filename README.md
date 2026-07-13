# Unity 三消 Demo

## 操作

1. 打开 `Assets/Scenes/Gameplay.unity`，竖屏 Game 视图（9:16）
2. Play：点击选中棋子，再点相邻棋子交换
3. 无效交换会回弹；有效则消除 → 下落补块 → 连锁
4. 行李箱被消除波及或道具打到会收集；目标归零出 Great

## 已实现（架构 P0 + 部分 P1）

| 模块 | 内容 |
|------|------|
| Core | MatchFinder / SwapValidator / ClearResolver / Gravity / Spawner / Cascade / Booster |
| Game | InputController / GameFlowController / LevelDirector |
| View | BoardView 交换/消除/下落动画、GoalHUD、WinPanel |
| Vfx | AtmosphereFx 飘雪、CollectFx punch |

道具：4 连火箭、2×2 螺旋桨、L/T 炸弹（彩球默认关）

## 菜单

`CandyCrush → Bootstrap Project` 可重建图集与场景
