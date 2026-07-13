namespace CandyCrush.Data
{
    /// <summary>棋子类型枚举。行为与表现均通过类型查表，避免魔法数。</summary>
    public enum TileType
    {
        Empty = 0,
        Red = 1,      // candy_1_1 红帽
        Yellow = 2,   // candy_1_2 黄铃
        Blue = 3,     // candy_1_3 蓝枕
        Green = 4,    // candy_1_4 绿叶
        Suitcase = 10,
        RocketH = 20,
        RocketV = 21,
        Propeller = 22,
        Bomb = 23,
        ColorBall = 24
    }

    public enum ObjectiveType
    {
        CollectSuitcase = 0
    }

    public enum BoosterType
    {
        None = 0,
        RocketH = 1,
        RocketV = 2,
        Propeller = 3,
        Bomb = 4,
        ColorBall = 5
    }

    public static class TileTypeUtil
    {
        public static bool IsNormal(TileType t) =>
            t == TileType.Red || t == TileType.Yellow || t == TileType.Blue || t == TileType.Green;

        public static bool IsBooster(TileType t) =>
            t == TileType.RocketH || t == TileType.RocketV || t == TileType.Propeller ||
            t == TileType.Bomb || t == TileType.ColorBall;
    }
}
