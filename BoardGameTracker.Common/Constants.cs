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

    public static class AppConfig
    {
        public const string Currency = "currency";
        public const string DateFormat = "date_format";
        public const string TimeFormat = "time_format";
        public const string UiLanguage = "ui_language";
        public const string ShelfOfShameEnabled = "shelf_of_shame_enabled";
        public const string ShelfOfShameMonths = "shelf_of_shame_months";
        public const string GameNightsEnabled = "game_nights_enabled";
        public const string PublicUrl = "public_url";
        public const string RsvpAuthenticationEnabled = "rsvp_authentication_enabled";
    }

    public static class AuthRoles
    {
        public const string Admin = "Admin";
        public const string Reader = "Reader";
    }

    public static class UpdateConfig
    {
        public const string Prefix = "update_";
        public const string Track = "update_track";
        public const string CheckEnabled = "update_check_enabled";
        public const string CheckIntervalHours = "update_check_interval_hours";
        public const string CheckError = "update_check_error";
        public const string CheckLastRun = "update_check_last_run";
        public const string AvailableVersion = "update_available_version";
        public const string Available = "update_available";
    }
}