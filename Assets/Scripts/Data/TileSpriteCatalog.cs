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

        [Serializable]
        public struct ClearParticleSet
        {
            public TileType type;
            public Sprite[] shards;
        }

        public Entry[] entries;
        public Sprite boardCellSprite;
        public ClearParticleSet[] clearParticles;
        public Sprite clearFlashSprite;

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

        public Sprite[] GetClearShards(TileType type)
        {
            if (clearParticles == null) return null;
            for (int i = 0; i < clearParticles.Length; i++)
            {
                if (clearParticles[i].type == type)
                    return clearParticles[i].shards;
            }
            return null;
        }
    }
}
