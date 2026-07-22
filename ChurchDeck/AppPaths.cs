namespace ChurchDeck;

internal static class AppPaths
{
    // The installer deploys the seed database here. This makes the application fully offline.
    public static string DataDirectory => Path.Combine(AppContext.BaseDirectory, "data");
    public static string DatabasePath => Path.Combine(DataDirectory, "churchdeck.db");
}
