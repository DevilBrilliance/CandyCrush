# Unity 三消 Demo — 工程说明

## 快速开始

1. 用 Unity **2022.3.34f1c1** 打开本工程
2. 菜单执行 **CandyCrush → Bootstrap Project**（首次必做：导入 Sprite、打包图集、生成场景）
3. 打开 `Assets/Scenes/Gameplay.unity`，点击 Play

命令行等价：

```bat
"C:\Program Files\Unity\Hub\Editor\2022.3.34f1c1\Editor\Unity.exe" ^
  -batchmode -quit -projectPath "%CD%" ^
  -executeMethod CandyCrush.EditorTools.ProjectBootstrapCli.BootstrapCli ^
  -logFile "%CD%\bootstrap.log"
```

## 资源与图集

| 路径 | 说明 |
|------|------|
| `Assets/Art/Sprites/Tiles` | 棋子 / 行李箱 / 道具 |
| `Assets/Art/Sprites/UI` | HUD / 面板 / 按钮 |
| `Assets/Art/Sprites/Vfx` | 消除碎屑粒子图 |
| `Assets/Art/Backgrounds` | 街景背景（参考视频截帧） |
| `Assets/Art/Atlases/Atlas_Tiles.spriteatlas` | 棋子图集 |
| `Assets/Art/Atlases/Atlas_UI.spriteatlas` | UI 图集 |
| `Assets/Art/Atlases/Atlas_Vfx.spriteatlas` | 特效图集 |

棋子对应：红帽 `candy_1_1` / 黄铃 `candy_1_2` / 蓝枕 `candy_1_3` / 绿叶 `candy_1_4` / 行李箱 `tile_suitcase`。

## 场景结构（参考视频）

- 暗色街景背景 + 全屏飘雪（`AtmosphereFx`）
- 9 列 × 8 行棋盘（`LevelConfig` 可改）
- 顶栏 GoalHUD：行李箱图标 + 剩余数量
- WinPanel：`Great` 结算（目标归零后显示）

## 架构目录

```
Scripts/Data|Core|View|Vfx|Audio|Game|Common
```

本期已落地：数据层骨架、BoardView 镜像、LevelDirector、图集资源管线、Gameplay 场景。
