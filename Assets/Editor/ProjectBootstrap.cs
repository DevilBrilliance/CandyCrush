using System.Collections.Generic;
using System.IO;
using CandyCrush.Data;
using CandyCrush.Game;
using CandyCrush.View;
using TMPro;
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
            CreateGameplayScene(catalog, level);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CandyCrush] Bootstrap complete → open Scenes/Gameplay.unity and Press Play.");
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

        static void EnsureDirs()
        {
            Directory.CreateDirectory(AtlasDir);
            Directory.CreateDirectory(ConfigDir);
            Directory.CreateDirectory("Assets/Scenes");
            Directory.CreateDirectory("Assets/Resources/Configs");
        }

        static void ConfigureAllSprites()
        {
            var folders = new[]
            {
                $"{ArtRoot}/Sprites/Tiles",
                $"{ArtRoot}/Sprites/UI",
                $"{ArtRoot}/Sprites/Vfx",
                $"{ArtRoot}/Sprites/Board",
                $"{ArtRoot}/Backgrounds"
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
                    if (importer.textureCompression != TextureImporterCompression.Compressed)
                    {
                        importer.textureCompression = TextureImporterCompression.Compressed;
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
                new TileSpriteCatalog.Entry { type = TileType.ColorBall, sprite = Load($"{ArtRoot}/Sprites/Tiles/boost_candy_rainbow.png") },
            };
            catalog.boardCellSprite = Load($"{ArtRoot}/Sprites/Board/candy_bg_01.png");
            catalog.boardFrameSprite = Load($"{ArtRoot}/Sprites/UI/UIpanel_outside.png");

            EditorUtility.SetDirty(catalog);
            return catalog;
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
            cam.backgroundColor = new Color(0.05f, 0.08f, 0.12f);
            cam.transform.position = new Vector3(0f, 0f, -10f);

            // Background
            var bgGo = new GameObject("Background");
            var bgSr = bgGo.AddComponent<SpriteRenderer>();
            bgSr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtRoot}/Backgrounds/bg_street_night.jpg");
            bgSr.sortingOrder = -20;
            FitSpriteToCamera(bgSr, cam);

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

            // Canvas HUD
            var canvasGo = new GameObject("Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            // Goal panel
            var goalPanel = new GameObject("GoalPanel", typeof(RectTransform));
            goalPanel.transform.SetParent(canvasGo.transform, false);
            var goalRt = goalPanel.GetComponent<RectTransform>();
            goalRt.anchorMin = new Vector2(0.5f, 1f);
            goalRt.anchorMax = new Vector2(0.5f, 1f);
            goalRt.pivot = new Vector2(0.5f, 1f);
            goalRt.anchoredPosition = new Vector2(0f, -40f);
            goalRt.sizeDelta = new Vector2(420f, 120f);

            var goalImg = goalPanel.AddComponent<Image>();
            goalImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtRoot}/Sprites/UI/UIpanel_goal_1.png");
            goalImg.type = Image.Type.Sliced;
            goalImg.preserveAspect = false;

            var iconGo = new GameObject("Icon", typeof(RectTransform));
            iconGo.transform.SetParent(goalPanel.transform, false);
            var iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = iconRt.anchorMax = new Vector2(0.28f, 0.5f);
            iconRt.sizeDelta = new Vector2(72f, 72f);
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.sprite = catalog.GetSprite(TileType.Suitcase);
            iconImg.preserveAspect = true;

            var textGo = new GameObject("Count", typeof(RectTransform));
            textGo.transform.SetParent(goalPanel.transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = textRt.anchorMax = new Vector2(0.62f, 0.5f);
            textRt.sizeDelta = new Vector2(160f, 80f);
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = "15";
            tmp.fontSize = 64;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.fontStyle = FontStyles.Bold;

            var goalHud = goalPanel.AddComponent<GoalHUD>();
            var hudSo = new SerializedObject(goalHud);
            hudSo.FindProperty("icon").objectReferenceValue = iconImg;
            hudSo.FindProperty("countText").objectReferenceValue = tmp;
            hudSo.ApplyModifiedPropertiesWithoutUndo();

            // Win panel
            var winGo = new GameObject("WinPanel", typeof(RectTransform));
            winGo.transform.SetParent(canvasGo.transform, false);
            var winRt = winGo.GetComponent<RectTransform>();
            winRt.anchorMin = Vector2.zero;
            winRt.anchorMax = Vector2.one;
            winRt.offsetMin = winRt.offsetMax = Vector2.zero;
            var winBg = winGo.AddComponent<Image>();
            winBg.color = new Color(0f, 0f, 0f, 0.45f);

            var greatGo = new GameObject("GreatText", typeof(RectTransform));
            greatGo.transform.SetParent(winGo.transform, false);
            var greatRt = greatGo.GetComponent<RectTransform>();
            greatRt.anchorMin = greatRt.anchorMax = new Vector2(0.5f, 0.72f);
            greatRt.sizeDelta = new Vector2(600f, 140f);
            var greatTmp = greatGo.AddComponent<TextMeshProUGUI>();
            greatTmp.text = "Great";
            greatTmp.fontSize = 96;
            greatTmp.alignment = TextAlignmentOptions.Center;
            greatTmp.color = new Color(1f, 0.85f, 0.2f);

            var winSuitcase = new GameObject("WinSuitcase", typeof(RectTransform));
            winSuitcase.transform.SetParent(winGo.transform, false);
            var wsRt = winSuitcase.GetComponent<RectTransform>();
            wsRt.anchorMin = wsRt.anchorMax = new Vector2(0.5f, 0.5f);
            wsRt.sizeDelta = new Vector2(160f, 160f);
            var wsImg = winSuitcase.AddComponent<Image>();
            wsImg.sprite = catalog.GetSprite(TileType.Suitcase);
            wsImg.preserveAspect = true;

            var winPanel = winGo.AddComponent<WinPanel>();
            var winSo = new SerializedObject(winPanel);
            winSo.FindProperty("root").objectReferenceValue = winGo;
            winSo.ApplyModifiedPropertiesWithoutUndo();
            winGo.SetActive(false);

            // EventSystem
            if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // LevelDirector
            var directorGo = new GameObject("LevelDirector");
            var director = directorGo.AddComponent<LevelDirector>();
            var dirSo = new SerializedObject(director);
            dirSo.FindProperty("levelConfig").objectReferenceValue = level;
            dirSo.FindProperty("catalog").objectReferenceValue = catalog;
            dirSo.FindProperty("boardView").objectReferenceValue = boardView;
            dirSo.FindProperty("goalHud").objectReferenceValue = goalHud;
            dirSo.FindProperty("winPanel").objectReferenceValue = winPanel;
            dirSo.FindProperty("atmosphereRoot").objectReferenceValue = boardRoot.transform;
            dirSo.FindProperty("background").objectReferenceValue = bgSr;
            dirSo.ApplyModifiedPropertiesWithoutUndo();

            var portrait = directorGo.AddComponent<PortraitSetup>();
            var portraitSo = new SerializedObject(portrait);
            portraitSo.FindProperty("targetCamera").objectReferenceValue = cam;
            portraitSo.FindProperty("background").objectReferenceValue = bgSr;
            portraitSo.FindProperty("portraitOrthoSize").floatValue = 8.2f;
            portraitSo.ApplyModifiedPropertiesWithoutUndo();

            var inputGo = new GameObject("InputController");
            var input = inputGo.AddComponent<InputController>();
            var inputSo = new SerializedObject(input);
            inputSo.FindProperty("boardView").objectReferenceValue = boardView;
            inputSo.FindProperty("worldCamera").objectReferenceValue = cam;
            inputSo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[CandyCrush] Scene saved: {ScenePath}");
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
