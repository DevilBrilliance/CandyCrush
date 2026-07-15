using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CandyCrush.EditorTools
{
    /// <summary>
    /// 一键打 Android APK。菜单：CandyCrush / Build Android APK
    /// 也可 batchmode：-executeMethod CandyCrush.EditorTools.AndroidBuilder.BuildApk
    /// </summary>
    public static class AndroidBuilder
    {
        const string DefaultOutRel = "Builds/Android/CandyCrush.apk";

        [MenuItem("CandyCrush/Build Android APK")]
        public static void BuildApkMenu()
        {
            var ok = BuildApkInternal(false);
            EditorUtility.DisplayDialog(
                "Android Build",
                ok ? $"打包成功：\n{Path.GetFullPath(DefaultOutRel)}" : "打包失败，请看 Console / 日志。",
                "OK");
        }

        /// <summary>供命令行调用。</summary>
        public static void BuildApk()
        {
            var ok = BuildApkInternal(true);
            EditorApplication.Exit(ok ? 0 : 1);
        }

        static bool BuildApkInternal(bool batch)
        {
            try
            {
                ConfigurePlayer();
                EnsureScenes();

                var outPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", DefaultOutRel));
                var dir = Path.GetDirectoryName(outPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                if (File.Exists(outPath))
                    File.Delete(outPath);

                var scenes = EditorBuildSettings.scenes;
                var enabled = Array.FindAll(scenes, s => s.enabled);
                if (enabled.Length == 0)
                {
                    Debug.LogError("[AndroidBuilder] No enabled scenes in Build Settings.");
                    return false;
                }

                var opts = new BuildPlayerOptions
                {
                    scenes = Array.ConvertAll(enabled, s => s.path),
                    locationPathName = outPath,
                    target = BuildTarget.Android,
                    options = BuildOptions.CompressWithLz4
                };

                Debug.Log($"[AndroidBuilder] Building APK → {outPath}");
                var report = BuildPipeline.BuildPlayer(opts);
                var summary = report.summary;
                Debug.Log($"[AndroidBuilder] Result={summary.result} size={summary.totalSize} time={summary.totalTime}");

                if (summary.result != BuildResult.Succeeded)
                {
                    foreach (var step in report.steps)
                    {
                        foreach (var msg in step.messages)
                        {
                            if (msg.type == LogType.Error || msg.type == LogType.Exception)
                                Debug.LogError(msg.content);
                        }
                    }
                    return false;
                }

                return File.Exists(outPath);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }
        }

        static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "CandyCrush";
            PlayerSettings.productName = "CandyCrush";
            PlayerSettings.bundleVersion = "1.0";
            PlayerSettings.Android.bundleVersionCode = 1;
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.candycrush.demo");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            // ARMv7 + ARM64
            PlayerSettings.Android.targetArchitectures =
                AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;

            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel22;
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
            EditorUserBuildSettings.androidCreateSymbols = AndroidCreateSymbols.Disabled;
        }

        static void EnsureScenes()
        {
            const string scene = "Assets/Scenes/Gameplay.unity";
            var scenes = EditorBuildSettings.scenes;
            for (int i = 0; i < scenes.Length; i++)
            {
                if (scenes[i].path == scene && scenes[i].enabled)
                    return;
            }

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(scene, true)
            };
        }
    }
}
