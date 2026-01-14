namespace BoardGameTracker.Common;

public static class Constants
{
    public static class Bgg
    {
        public const string Category = "boardgamecategory";
        public const string Mechanic = "boardgamemechanic";
        public const string Artist = "boardgameartist";
        public const string Designer = "boardgamedesigner";
        public const string Publisher = "boardgamepublisher";
        public const string Expansion = "boardgameexpansion";
    }
    
    public static class Game
    {
        public const int ChartHistoryDays = 200;
        public const int TopPlayersCount = 5;
    }

}