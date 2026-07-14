using System.Collections.Generic;
using System.IO;
using CandyCrush.Data;
using CandyCrush.Game;
using CandyCrush.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace CandyCrush.EditorTools
{
    /// <summary>
    /// 一键：导入 Sprite 设置 → 打图集 → 生成配置 → 搭建 Gameplay 场景。
    /// Menu: CandyCrush / Bootstrap Project
    /// </summary>
    public static class ProjectBootstrap
    {
        const string ArtRoot = "Assets/Art";
        const string AtlasDir = "Assets/Art/Atlases";
        const string ConfigDir = "Assets/Resources/Configs";
        const string UiPrefabDir = "Assets/Resources/Prefabs/UI";
        const string ScenePath = "Assets/Scenes/Gameplay.unity";

        [MenuItem("CandyCrush/Bootstrap Project")]
        public static void Bootstrap()
        {
            EnsureDirs();
            ConfigureAllSprites();
            CreateAtlases();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var catalog = CreateTileCatalog();
            var level = CreateDemoLevelConfig();
            CreateOrUpdateUiPrefabs(catalog);
            CreateGameplayScene(catalog, level);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CandyCrush] Bootstrap complete → open Scenes/Gameplay.unity and Press Play.");
        }

        [MenuItem("CandyCrush/Rebuild UI Prefabs")]
        public static void RebuildUiPrefabsMenu()
        {
            var goal = EnsureUiPrefabsExist();
            WirePrefabsIntoOpenOrSavedScene(goal.goal, goal.win);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CandyCrush] UI prefabs rebuilt → Resources/Prefabs/UI/");
        }

        /// <summary>确保 GoalHUD / WinPanel 预制体存在于 Resources，供运行时加载。</summary>
        public static (GoalHUD goal, WinPanel win) EnsureUiPrefabsExist()
        {
            EnsureDirs();
            var catalog = AssetDatabase.LoadAssetAtPath<TileSpriteCatalog>(ConfigDir + "/TileSpriteCatalog.asset");
            if (catalog == null)
                catalog = CreateTileCatalog();
            return CreateOrUpdateUiPrefabs(catalog);
        }

        [MenuItem("CandyCrush/Rebuild Sprite Atlases Only")]
        public static void RebuildAtlasesOnly()
        {
            ConfigureAllSprites();
            CreateAtlases();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CandyCrush] Atlases rebuilt.");
        }

        [MenuItem("CandyCrush/Fix Vfx Texture Compression")]
        public static void FixVfxTextureCompression()
        {
            ConfigureAllSprites();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CandyCrush] Vfx textures set to Uncompressed for SpriteAtlas packing.");
        }

        static void EnsureDirs()
        {
            Directory.CreateDirectory(AtlasDir);
            Directory.CreateDirectory(ConfigDir);
            Directory.CreateDirectory(UiPrefabDir);
            Directory.CreateDirectory("Assets/Scenes");
            Directory.CreateDirectory("Assets/Resources/Configs");
            Directory.CreateDirectory("Assets/Resources/Prefabs/UI");
        }

        static void ConfigureAllSprites()
        {
            var folders = new[]
            {
                $"{ArtRoot}/Sprites/Tiles",
                $"{ArtRoot}/Sprites/UI",
                $"{ArtRoot}/Sprites/Vfx",
                $"{ArtRoot}/Sprites/Board",
                $"{ArtRoot}/Backgrounds",
                "Assets/Resources/Vfx"
            };

            foreach (var folder in folders)
            {
                if (!AssetDatabase.IsValidFolder(folder)) continue;
                var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer == null) continue;

                    bool changed = false;
                    if (importer.textureType != TextureImporterType.Sprite)
                    {
                        importer.textureType = TextureImporterType.Sprite;
                        changed = true;
                    }
                    if (importer.spritePixelsPerUnit != 100f)
                    {
                        importer.spritePixelsPerUnit = 100f;
                        changed = true;
                    }
                    if (importer.mipmapEnabled)
                    {
                        importer.mipmapEnabled = false;
                        changed = true;
                    }
                    if (importer.filterMode != FilterMode.Bilinear)
                    {
                        importer.filterMode = FilterMode.Bilinear;
                        changed = true;
                    }
                    bool isVfx = folder.Contains("/Vfx") || folder.Contains("\\Vfx") || path.Contains("/Vfx/") || path.Contains("\\Vfx\\");
                    var wantCompression = isVfx
                        ? TextureImporterCompression.Uncompressed
                        : TextureImporterCompression.Compressed;
                    if (importer.textureCompression != wantCompression)
                    {
                        importer.textureCompression = wantCompression;
                        changed = true;
                    }

                    // 背景用 Default 也可，但 Sprite 便于挂 SpriteRenderer
                    if (folder.EndsWith("Backgrounds"))
                    {
                        importer.spritePixelsPerUnit = 100f;
                    }

                    // UI 面板九宫
                    var name = Path.GetFileNameWithoutExtension(path);
                    if (name.StartsWith("UIpanel_goal") || name == "ui_text_background" || name == "UIpanel_outside")
                    {
                        importer.spriteBorder = new Vector4(40, 20, 40, 20);
                        changed = true;
                    }

                    if (changed)
                        importer.SaveAndReimport();
                }
            }
        }

        static void CreateAtlases()
        {
            CreateAtlas("Atlas_Tiles", new[]
            {
                $"{ArtRoot}/Sprites/Tiles",
                $"{ArtRoot}/Sprites/Board"
            });
            CreateAtlas("Atlas_UI", new[]
            {
                $"{ArtRoot}/Sprites/UI"
            });
            CreateAtlas("Atlas_Vfx", new[]
            {
                $"{ArtRoot}/Sprites/Vfx"
            });
        }

        static void CreateAtlas(string atlasName, string[] includeFolders)
        {
            var atlasPath = $"{AtlasDir}/{atlasName}.spriteatlas";
            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
            if (atlas == null)
            {
                atlas = new SpriteAtlas();
                AssetDatabase.CreateAsset(atlas, atlasPath);
            }

            // 清空旧 packing
            var so = new SerializedObject(atlas);
            var packables = so.FindProperty("m_EditorData.packables");
            packables.ClearArray();
            so.ApplyModifiedPropertiesWithoutUndo();

            var objects = new List<Object>();
            foreach (var folder in includeFolders)
            {
                if (!AssetDatabase.IsValidFolder(folder)) continue;
                var folderAsset = AssetDatabase.LoadAssetAtPath<Object>(folder);
                if (folderAsset != null) objects.Add(folderAsset);
            }

            SpriteAtlasExtensions.Add(atlas, objects.ToArray());

            var packing = new SpriteAtlasPackingSettings
            {
                enableRotation = false,
                enableTightPacking = false,
                padding = 4
            };
            atlas.SetPackingSettings(packing);

            var texture = new SpriteAtlasTextureSettings
            {
                readable = false,
                generateMipMaps = false,
                sRGB = true,
                filterMode = FilterMode.Bilinear
            };
            atlas.SetTextureSettings(texture);

            var platform = atlas.GetPlatformSettings("DefaultTexturePlatform");
            platform.maxTextureSize = 2048;
            platform.format = TextureImporterFormat.Automatic;
            platform.textureCompression = TextureImporterCompression.Compressed;
            atlas.SetPlatformSettings(platform);

            EditorUtility.SetDirty(atlas);
            SpriteAtlasUtility.PackAtlases(new[] { atlas }, EditorUserBuildSettings.activeBuildTarget);
            Debug.Log($"[CandyCrush] Packed {atlasName} with {objects.Count} folders.");
        }

        static TileSpriteCatalog CreateTileCatalog()
        {
            const string path = ConfigDir + "/TileSpriteCatalog.asset";
            var catalog = AssetDatabase.LoadAssetAtPath<TileSpriteCatalog>(path);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<TileSpriteCatalog>();
                AssetDatabase.CreateAsset(catalog, path);
            }

            Sprite Load(string p) => AssetDatabase.LoadAssetAtPath<Sprite>(p);

            catalog.entries = new[]
            {
                new TileSpriteCatalog.Entry { type = TileType.Red, sprite = Load($"{ArtRoot}/Sprites/Tiles/candy_1_1_1.png") },
                new TileSpriteCatalog.Entry { type = TileType.Yellow, sprite = Load($"{ArtRoot}/Sprites/Tiles/candy_1_2_1.png") },
                new TileSpriteCatalog.Entry { type = TileType.Blue, sprite = Load($"{ArtRoot}/Sprites/Tiles/candy_1_3_1.png") },
                new TileSpriteCatalog.Entry { type = TileType.Green, sprite = Load($"{ArtRoot}/Sprites/Tiles/candy_1_4_1.png") },
                new TileSpriteCatalog.Entry { type = TileType.Suitcase, sprite = Load($"{ArtRoot}/Sprites/Tiles/tile_suitcase.png") },
                new TileSpriteCatalog.Entry { type = TileType.Bomb, sprite = Load($"{ArtRoot}/Sprites/Tiles/boost_candy_bomb.png") },
                new TileSpriteCatalog.Entry { type = TileType.RocketH, sprite = Load($"{ArtRoot}/Sprites/Tiles/boost_candy_hv.png") },
                new TileSpriteCatalog.Entry { type = TileType.RocketV, sprite = Load($"{ArtRoot}/Sprites/Tiles/boost_candy_hv.png") },
                new TileSpriteCatalog.Entry { type = TileType.Propeller, sprite = Load($"{ArtRoot}/Sprites/Tiles/royal_leaves_feiji.png") },
                new TileSpriteCatalog.Entry { type = TileType.ColorBall, sprite = Load($"{ArtRoot}/Sprites/Tiles/boost_candy_rainbow.png") },
            };
            catalog.boardCellSprite = Load($"{ArtRoot}/Sprites/Board/tileA.png");
            catalog.boardCellAltSprite = Load($"{ArtRoot}/Sprites/Board/tileB.png");
            catalog.boardPanelSprite = Load($"{ArtRoot}/Sprites/Board/UIpanel_outside.png")
                                        ?? Load($"{ArtRoot}/Sprites/Board/candy_bg_01.png");
            catalog.clearFlashSprite = Load($"{ArtRoot}/Sprites/Vfx/candy_12_particle.png");
            catalog.clearParticles = new[]
            {
                MakeClearSet(TileType.Red, "1_1"),
                MakeClearSet(TileType.Yellow, "1_2"),
                MakeClearSet(TileType.Blue, "1_3"),
                MakeClearSet(TileType.Green, "1_4"),
            };

            EditorUtility.SetDirty(catalog);
            return catalog;

            TileSpriteCatalog.ClearParticleSet MakeClearSet(TileType type, string colorKey)
            {
                var shards = new Sprite[4];
                for (int i = 0; i < 4; i++)
                    shards[i] = Load($"{ArtRoot}/Sprites/Vfx/particle_die_candy_{colorKey}_0{i + 1}.png");
                return new TileSpriteCatalog.ClearParticleSet { type = type, shards = shards };
            }
        }

        static LevelConfig CreateDemoLevelConfig()
        {
            const string path = ConfigDir + "/Level_Demo_Suitcase.asset";
            var level = AssetDatabase.LoadAssetAtPath<LevelConfig>(path);
            if (level == null)
            {
                level = ScriptableObject.CreateInstance<LevelConfig>();
                AssetDatabase.CreateAsset(level, path);
            }

            // 架构默认 8 行 × 9 列；目标 Demo 默认 15（视频为 33，可改）
            level.rows = 8;
            level.cols = 9;
            level.objectiveType = ObjectiveType.CollectSuitcase;
            level.objectiveCount = 33;
            level.spawnWeights = new[] { 1, 1, 1, 1 };
            level.enableColorBall = false;
            level.initialBoard = null; // 运行时用 DemoLayouts

            EditorUtility.SetDirty(level);
            return level;
        }

        static void CreateGameplayScene(TileSpriteCatalog catalog, LevelConfig level)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var cam = Camera.main;
            cam.orthographic = true;
            cam.orthographicSize = 8.2f;
            cam.backgroundColor = new Color(0.04f, 0.06f, 0.10f);
            cam.transform.position = new Vector3(0f, 0f, -10f);

            // 不引入夜景贴图：仅用相机清屏色
            // Board root
            var boardRoot = new GameObject("Board");
            boardRoot.transform.position = new Vector3(0f, -0.6f, 0f);

            var boardBgGo = new GameObject("BoardBg");
            boardBgGo.transform.SetParent(boardRoot.transform, false);
            var boardBg = boardBgGo.AddComponent<SpriteRenderer>();
            boardBg.sortingOrder = 0;

            var tileRoot = new GameObject("Tiles");
            tileRoot.transform.SetParent(boardRoot.transform, false);

            var boardView = boardRoot.AddComponent<BoardView>();
            var boardSo = new SerializedObject(boardView);
            boardSo.FindProperty("levelConfig").objectReferenceValue = level;
            boardSo.FindProperty("catalog").objectReferenceValue = catalog;
            boardSo.FindProperty("tileRoot").objectReferenceValue = tileRoot.transform;
            boardSo.FindProperty("boardBg").objectReferenceValue = boardBg;
            boardSo.FindProperty("cellSize").floatValue = 0.95f;
            boardSo.FindProperty("boardOrigin").vector2Value = Vector2.zero;
            boardSo.ApplyModifiedPropertiesWithoutUndo();

            // 空 Canvas：Goal/Win 由 LevelDirector 运行时实例化预制体
            var canvasGo = new GameObject("Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var uiPrefabs = CreateOrUpdateUiPrefabs(catalog);

            // EventSystem
            if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // LevelDirector
            var directorGo = new GameObject("LevelDirector");
            var flow = directorGo.AddComponent<GameFlowController>();
            var director = directorGo.AddComponent<LevelDirector>();
            var dirSo = new SerializedObject(director);
            dirSo.FindProperty("levelConfig").objectReferenceValue = level;
            dirSo.FindProperty("catalog").objectReferenceValue = catalog;
            dirSo.FindProperty("boardView").objectReferenceValue = boardView;
            dirSo.FindProperty("goalHudPrefab").objectReferenceValue = uiPrefabs.goal;
            dirSo.FindProperty("winPanelPrefab").objectReferenceValue = uiPrefabs.win;
            dirSo.FindProperty("uiRoot").objectReferenceValue = canvasGo.transform;
            dirSo.FindProperty("atmosphereRoot").objectReferenceValue = boardRoot.transform;
            dirSo.FindProperty("background").objectReferenceValue = null;
            dirSo.FindProperty("flow").objectReferenceValue = flow;
            dirSo.FindProperty("portraitOrthoSize").floatValue = 8.2f;
            dirSo.ApplyModifiedPropertiesWithoutUndo();

            var inputGo = new GameObject("InputController");
            var input = inputGo.AddComponent<InputController>();
            var inputSo = new SerializedObject(input);
            inputSo.FindProperty("boardView").objectReferenceValue = boardView;
            inputSo.FindProperty("flow").objectReferenceValue = flow;
            inputSo.FindProperty("worldCamera").objectReferenceValue = cam;
            inputSo.ApplyModifiedPropertiesWithoutUndo();

            dirSo = new SerializedObject(director);
            dirSo.FindProperty("input").objectReferenceValue = input;
            dirSo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[CandyCrush] Scene saved: {ScenePath}");
        }

        static (GoalHUD goal, WinPanel win) CreateOrUpdateUiPrefabs(TileSpriteCatalog catalog)
        {
            EnsureDirs();
            var panelSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtRoot}/Sprites/UI/UIpanel_goal_1.png");
            var font = AssetDatabase.LoadAssetAtPath<Font>($"{ArtRoot}/Fonts/BPreplay.ttf");
            var suitcase = catalog != null ? catalog.GetSprite(TileType.Suitcase) : null;

            var tempRoot = new GameObject("_UiPrefabBake");
            try
            {
                var goal = GameUiFactory.CreateGoalHud(tempRoot.transform, panelSprite, suitcase, font);
                var win = GameUiFactory.CreateWinPanel(tempRoot.transform, suitcase, font);

                string goalPath = UiPrefabDir + "/GoalHUD.prefab";
                string winPath = UiPrefabDir + "/WinPanel.prefab";

                var goalGo = PrefabUtility.SaveAsPrefabAsset(goal.gameObject, goalPath);
                var winGo = PrefabUtility.SaveAsPrefabAsset(win.gameObject, winPath);
                if (goalGo == null || winGo == null)
                    throw new System.Exception("Failed to save UI prefabs.");
                return (goalGo.GetComponent<GoalHUD>(), winGo.GetComponent<WinPanel>());
            }
            finally
            {
                Object.DestroyImmediate(tempRoot);
            }
        }

        static void WirePrefabsIntoOpenOrSavedScene(GoalHUD goalPrefab, WinPanel winPrefab)
        {
            var scene = File.Exists(ScenePath)
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single)
                : default;
            if (!scene.IsValid()) return;

            // 删除场景内烘焙的 Goal/Win
            foreach (var hud in Object.FindObjectsOfType<GoalHUD>())
                Object.DestroyImmediate(hud.gameObject);
            foreach (var win in Object.FindObjectsOfType<WinPanel>())
                Object.DestroyImmediate(win.gameObject);

            var director = Object.FindObjectOfType<LevelDirector>();
            if (director != null)
            {
                var dirSo = new SerializedObject(director);
                dirSo.FindProperty("goalHudPrefab").objectReferenceValue = goalPrefab;
                dirSo.FindProperty("winPanelPrefab").objectReferenceValue = winPrefab;
                var canvas = Object.FindObjectOfType<Canvas>();
                if (canvas != null)
                    dirSo.FindProperty("uiRoot").objectReferenceValue = canvas.transform;
                dirSo.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        static void FitSpriteToCamera(SpriteRenderer sr, Camera cam)
        {
            if (sr.sprite == null) return;
            float worldH = cam.orthographicSize * 2f;
            float worldW = worldH * cam.aspect;
            var size = sr.sprite.bounds.size;
            sr.transform.localScale = new Vector3(worldW / size.x, worldH / size.y, 1f);
            sr.transform.position = new Vector3(0f, 0f, 1f);
        }
    }

    /// <summary>命令行入口：Unity -batchmode -executeMethod CandyCrush.EditorTools.ProjectBootstrap.BootstrapCli</summary>
    public static class ProjectBootstrapCli
    {
        public static void BootstrapCli()
        {
            try
            {
                ProjectBootstrap.Bootstrap();
                EditorApplication.Exit(0);
            }
            catch (System.Exception e)
            {
                Debug.LogError(e);
                EditorApplication.Exit(1);
            }
        }
    }
}
