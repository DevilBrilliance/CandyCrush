using UnityEditor;
using UnityEngine;

namespace CandyCrush.EditorTools
{
    /// <summary>
    /// 鑴氭湰缂栬瘧瀹屾垚鍚庤嚜鍔ㄦ墽琛屼竴娆?Bootstrap锛堣嫢鍦烘櫙/鍥鹃泦灏氭湭鐢熸垚锛夈€?    /// 涔熷彲闅忔椂閫氳繃鑿滃崟 CandyCrush/Bootstrap Project 鎵嬪姩閲嶈窇銆?    /// </summary>
    [InitializeOnLoad]
    public static class AutoBootstrapOnLoad
    {
        const string PrefKey = "CandyCrush.BootstrapDone.v1";

        static AutoBootstrapOnLoad()
        {
            EditorApplication.delayCall += TryAutoBootstrap;
        }

        static void TryAutoBootstrap()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorPrefs.GetBool(PrefKey, false)) return;

            bool atlasReady = AssetDatabase.LoadAssetAtPath<UnityEngine.U2D.SpriteAtlas>(
                "Assets/Art/Atlases/Atlas_Tiles.spriteatlas") != null;
            bool sceneReady = System.IO.File.Exists("Assets/Scenes/Gameplay.unity");

            if (atlasReady && sceneReady)
            {
                EditorPrefs.SetBool(PrefKey, true);
                return;
            }

            Debug.Log("[CandyCrush] Auto bootstrap starting...");
            try
            {
                ProjectBootstrap.Bootstrap();
                EditorPrefs.SetBool(PrefKey, true);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[CandyCrush] Auto bootstrap failed: " + e);
            }
        }

        [MenuItem("CandyCrush/Reset Auto Bootstrap Flag")]
        static void ResetFlag()
        {
            EditorPrefs.DeleteKey(PrefKey);
            Debug.Log("[CandyCrush] Auto bootstrap flag cleared. Reimport/recompile to run again.");
        }
    }
}

