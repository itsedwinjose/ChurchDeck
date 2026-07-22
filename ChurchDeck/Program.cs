namespace ChurchDeck;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        var pathArgument = Array.FindIndex(args, value => value.Equals("--database", StringComparison.OrdinalIgnoreCase));
        var databasePath = pathArgument >= 0 && pathArgument + 1 < args.Length ? args[pathArgument + 1] : AppPaths.DatabasePath;
        var database = new ChurchDatabase(databasePath);
        database.Initialize();

        if (args.Contains("--initialize-db", StringComparer.OrdinalIgnoreCase))
            return;

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm(database));
    }
}
