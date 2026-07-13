using System;
using UnityEngine;

namespace CandyCrush.Data
{
    /// <summary>棋子 Sprite 查表。图集内 Sprite 通过名称引用。</summary>
    [CreateAssetMenu(menuName = "CandyCrush/Tile Sprite Catalog", fileName = "TileSpriteCatalog")]
    public class TileSpriteCatalog : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public TileType type;
            public Sprite sprite;
        }

        public Entry[] entries;
        public Sprite boardCellSprite;
        public Sprite boardFrameSprite;

        public Sprite GetSprite(TileType type)
        {
            if (entries == null) return null;
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].type == type)
                    return entries[i].sprite;
            }
            return null;
        }
    }
}
