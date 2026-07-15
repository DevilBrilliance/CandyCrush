using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace CandyCrush.EditorTools
{
    /// <summary>
    /// 给 Game 视图预设常见竖屏分辨率（FixedResolution）。
    /// 菜单：CandyCrush → Add Portrait Game View Resolutions
    /// </summary>
    public static class PortraitGameViewResolutions
    {
        const string PrefKey = "CandyCrush.PortraitGameViewResolutions.v1";

        // name, width, height
        static readonly (string name, int w, int h)[] Presets =
        {
            ("Portrait 9:16 720p", 720, 1280),
            ("Portrait iPhone SE", 750, 1334),
            ("Portrait 9:16 1080p", 1080, 1920),
            ("Portrait iPhone X/11", 1125, 2436),
            ("Portrait iPhone 12/13", 1170, 2532),
            ("Portrait 19.5:9 1080", 1080, 2340),
            ("Portrait 20:9 1080", 1080, 2400),
            ("Portrait iPhone 14 Pro Max", 1290, 2796),
            ("Portrait Android FHD+", 1080, 2400),
            ("Portrait iPad 3:4", 1536, 2048),
            ("Portrait iPad Pro 11", 1668, 2388),
        };

        [InitializeOnLoadMethod]
        static void AutoOnce()
        {
            if (EditorPrefs.GetBool(PrefKey, false)) return;
            EditorApplication.delayCall += () =>
            {
                if (EditorPrefs.GetBool(PrefKey, false)) return;
                try
                {
                    EnsurePresets();
                    EditorPrefs.SetBool(PrefKey, true);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[PortraitGameView] Auto add failed: {e.Message}");
                }
            };
        }

        [MenuItem("CandyCrush/Add Portrait Game View Resolutions")]
        public static void MenuEnsurePresets()
        {
            int added = EnsurePresets();
            EditorPrefs.SetBool(PrefKey, true);
            EditorUtility.DisplayDialog(
                "Portrait Resolutions",
                added > 0
                    ? $"已添加 {added} 个竖屏分辨率到 Game 视图。\n打开 Game 窗口分辨率下拉即可选择。"
                    : "预设已存在，无需重复添加。\n打开 Game 窗口分辨率下拉即可选择。",
                "OK");
        }

        public static int EnsurePresets()
        {
            int added = 0;
            var groups = new[]
            {
                GameViewSizeGroupType.Standalone,
                GameViewSizeGroupType.Android,
                GameViewSizeGroupType.iOS
            };

            foreach (var groupType in groups)
            {
                foreach (var p in Presets)
                {
                    if (SizeExists(groupType, p.w, p.h, p.name)) continue;
                    AddCustomSize(groupType, p.w, p.h, p.name);
                    added++;
                }
            }

            SaveToHdd();
            return added;
        }

        static object GetGroup(GameViewSizeGroupType type)
        {
            var sizesType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizes");
            var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var instance = singleton.GetProperty("instance")?.GetValue(null, null);
            var getGroup = sizesType.GetMethod("GetGroup");
            return getGroup?.Invoke(instance, new object[] { (int)type });
        }

        static void AddCustomSize(GameViewSizeGroupType groupType, int width, int height, string name)
        {
            var group = GetGroup(groupType);
            if (group == null) return;

            var asm = typeof(Editor).Assembly;
            var gameViewSizeType = asm.GetType("UnityEditor.GameViewSizeType");
            var gameViewSize = asm.GetType("UnityEditor.GameViewSize");
            // FixedResolution = 1
            object sizeTypeEnum = Enum.ToObject(gameViewSizeType, 1);
            var ctor = gameViewSize.GetConstructor(new[]
            {
                gameViewSizeType, typeof(int), typeof(int), typeof(string)
            });
            if (ctor == null) return;

            var newSize = ctor.Invoke(new[] { sizeTypeEnum, width, height, name });
            var add = group.GetType().GetMethod("AddCustomSize");
            add?.Invoke(group, new[] { newSize });
        }

        static bool SizeExists(GameViewSizeGroupType groupType, int width, int height, string name)
        {
            var group = GetGroup(groupType);
            if (group == null) return false;

            var getTotal = group.GetType().GetMethod("GetTotalCount");
            var getSize = group.GetType().GetMethod("GetGameViewSize");
            if (getTotal == null || getSize == null) return false;

            int total = (int)getTotal.Invoke(group, null);
            for (int i = 0; i < total; i++)
            {
                var size = getSize.Invoke(group, new object[] { i });
                if (size == null) continue;
                var t = size.GetType();
                int w = (int)t.GetProperty("width").GetValue(size, null);
                int h = (int)t.GetProperty("height").GetValue(size, null);
                string baseText = t.GetProperty("baseText")?.GetValue(size, null) as string
                                  ?? t.GetProperty("displayText")?.GetValue(size, null) as string
                                  ?? "";
                if (w == width && h == height) return true;
                if (!string.IsNullOrEmpty(name) &&
                    baseText.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }

        static void SaveToHdd()
        {
            var sizesType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizes");
            var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var instance = singleton.GetProperty("instance")?.GetValue(null, null);
            sizesType.GetMethod("SaveToHDD")?.Invoke(instance, null);
        }
    }
}
