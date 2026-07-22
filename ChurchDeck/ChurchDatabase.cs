using Microsoft.Data.Sqlite;

namespace ChurchDeck;

internal sealed class ChurchDatabase
{
    private readonly string _databasePath;
    private string ConnectionString => new SqliteConnectionStringBuilder { DataSource = _databasePath }.ToString();

    public ChurchDatabase(string databasePath) => _databasePath = databasePath;

    public void Initialize()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_databasePath)!);
        // The installer seed is replaced with a real SQLite file on first run.
        if (File.Exists(_databasePath) && new FileInfo(_databasePath).Length < 16)
            File.Delete(_databasePath);
        using var connection = Open();
        Execute(connection, @"CREATE TABLE IF NOT EXISTS BibleBooks (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL UNIQUE, SortOrder INTEGER NOT NULL);
CREATE TABLE IF NOT EXISTS BibleVerses (Id INTEGER PRIMARY KEY AUTOINCREMENT, BookId INTEGER NOT NULL, ChapterNumber INTEGER NOT NULL, VerseNumber INTEGER NOT NULL, Text TEXT NOT NULL, UNIQUE(BookId, ChapterNumber, VerseNumber), FOREIGN KEY(BookId) REFERENCES BibleBooks(Id) ON DELETE CASCADE);
CREATE TABLE IF NOT EXISTS Settings (Key TEXT PRIMARY KEY, Value TEXT NOT NULL);
CREATE TABLE IF NOT EXISTS Plans (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL UNIQUE);
CREATE TABLE IF NOT EXISTS PlannerItems (Id INTEGER PRIMARY KEY AUTOINCREMENT, ItemType TEXT NOT NULL, Reference TEXT NOT NULL, Content TEXT NOT NULL, SortOrder INTEGER NOT NULL);
CREATE TABLE IF NOT EXISTS Songs (Id INTEGER PRIMARY KEY AUTOINCREMENT, Title TEXT NOT NULL, Lyrics TEXT NOT NULL);
CREATE TABLE IF NOT EXISTS MediaItems (Id INTEGER PRIMARY KEY AUTOINCREMENT, MediaType TEXT NOT NULL, Title TEXT NOT NULL, FilePath TEXT NOT NULL);");
        EnsureDefaultPlan(connection);
        try { Execute(connection, "ALTER TABLE PlannerItems ADD COLUMN PlanId INTEGER NULL;"); } catch (SqliteException) { /* Column already exists. */ }
        try { Execute(connection, "ALTER TABLE PlannerItems ADD COLUMN FontColor TEXT NULL;"); } catch (SqliteException) { /* Column already exists. */ }
        try { Execute(connection, "ALTER TABLE PlannerItems ADD COLUMN SourceValue TEXT NULL;"); } catch (SqliteException) { /* Column already exists. */ }
        try { Execute(connection, "ALTER TABLE PlannerItems ADD COLUMN FontSize INTEGER NULL;"); } catch (SqliteException) { /* Column already exists. */ }
        Execute(connection, "UPDATE PlannerItems SET PlanId = COALESCE(PlanId, (SELECT Id FROM Plans WHERE Name='Default Plan' LIMIT 1));");
        Execute(connection, "INSERT OR IGNORE INTO Settings (Key, Value) VALUES ('PresentationBackground', '#FFFFFF'), ('PresentationFontColor', '#000000'), ('PresentationFontSize', '48'), ('PresentationFontFamily', 'Segoe UI');");
    }

    public List<BibleBook> GetBooks()
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Name, SortOrder FROM BibleBooks ORDER BY SortOrder, Name";
        using var reader = command.ExecuteReader();
        var books = new List<BibleBook>();
        while (reader.Read()) books.Add(new BibleBook(reader.GetInt64(0), reader.GetString(1), reader.GetInt32(2)));
        return books;
    }

    public BibleBook AddBook(string name)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO BibleBooks (Name, SortOrder) VALUES ($name, COALESCE((SELECT MAX(SortOrder) + 1 FROM BibleBooks), 1)); SELECT last_insert_rowid();";
        command.Parameters.AddWithValue("$name", name.Trim());
        var id = (long)command.ExecuteScalar()!;
        return new BibleBook(id, name.Trim(), 0);
    }

    public List<BibleVerse> GetVerses(long bookId, int chapter)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, VerseNumber, Text FROM BibleVerses WHERE BookId=$bookId AND ChapterNumber=$chapter ORDER BY VerseNumber";
        command.Parameters.AddWithValue("$bookId", bookId);
        command.Parameters.AddWithValue("$chapter", chapter);
        using var reader = command.ExecuteReader();
        var verses = new List<BibleVerse>();
        while (reader.Read()) verses.Add(new BibleVerse(reader.GetInt64(0), reader.GetInt32(1), reader.GetString(2)));
        return verses;
    }

    public void SaveVerse(long bookId, int chapter, int verse, string text)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = @"INSERT INTO BibleVerses (BookId, ChapterNumber, VerseNumber, Text) VALUES ($bookId,$chapter,$verse,$text)
ON CONFLICT(BookId, ChapterNumber, VerseNumber) DO UPDATE SET Text=excluded.Text;";
        command.Parameters.AddWithValue("$bookId", bookId); command.Parameters.AddWithValue("$chapter", chapter);
        command.Parameters.AddWithValue("$verse", verse); command.Parameters.AddWithValue("$text", text.Trim()); command.ExecuteNonQuery();
    }

    public void DeleteVerse(long verseId) { using var c = Open(); using var cmd = c.CreateCommand(); cmd.CommandText = "DELETE FROM BibleVerses WHERE Id=$id"; cmd.Parameters.AddWithValue("$id", verseId); cmd.ExecuteNonQuery(); }
    public string GetSetting(string key, string defaultValue) { using var c = Open(); using var cmd = c.CreateCommand(); cmd.CommandText = "SELECT Value FROM Settings WHERE Key=$key"; cmd.Parameters.AddWithValue("$key", key); return cmd.ExecuteScalar() as string ?? defaultValue; }
    public void SetSetting(string key, string value) { using var c = Open(); using var cmd = c.CreateCommand(); cmd.CommandText = "INSERT INTO Settings(Key,Value) VALUES($key,$value) ON CONFLICT(Key) DO UPDATE SET Value=excluded.Value"; cmd.Parameters.AddWithValue("$key", key); cmd.Parameters.AddWithValue("$value", value); cmd.ExecuteNonQuery(); }
    public List<ServicePlan> GetPlans()
    {
        using var c = Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT Id, Name FROM Plans ORDER BY Name";
        using var r = cmd.ExecuteReader();
        var plans = new List<ServicePlan>();
        while (r.Read()) plans.Add(new ServicePlan(r.GetInt64(0), r.GetString(1)));
        return plans;
    }

    public long SavePlan(long? planId, string name)
    {
        using var c = Open();
        using var cmd = c.CreateCommand();
        if (planId.HasValue)
        {
            cmd.CommandText = "UPDATE Plans SET Name=$name WHERE Id=$id; SELECT $id;";
            cmd.Parameters.AddWithValue("$id", planId.Value);
        }
        else
        {
            cmd.CommandText = "INSERT INTO Plans(Name) VALUES($name); SELECT last_insert_rowid();";
        }
        cmd.Parameters.AddWithValue("$name", name.Trim());
        return (long)cmd.ExecuteScalar()!;
    }

    public void DeletePlan(long planId)
    {
        using var c = Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "DELETE FROM PlannerItems WHERE PlanId=$planId; DELETE FROM Plans WHERE Id=$id;";
        cmd.Parameters.AddWithValue("$planId", planId);
        cmd.Parameters.AddWithValue("$id", planId);
        cmd.ExecuteNonQuery();
    }

    public long AddPlannerVerse(long planId, string reference, string content, int? fontSize = null) { using var c = Open(); using var cmd = c.CreateCommand(); cmd.CommandText = "INSERT INTO PlannerItems(PlanId,ItemType,Reference,Content,FontSize,SortOrder) VALUES($planId,'Bible',$reference,$content,$fontSize,COALESCE((SELECT MAX(SortOrder)+1 FROM PlannerItems WHERE PlanId=$planId),1)); SELECT last_insert_rowid();"; cmd.Parameters.AddWithValue("$planId", planId); cmd.Parameters.AddWithValue("$reference", reference); cmd.Parameters.AddWithValue("$content", content); cmd.Parameters.AddWithValue("$fontSize", (object?)fontSize ?? DBNull.Value); return (long)cmd.ExecuteScalar()!; }
    public void SetPlannerItemFontColor(long itemId, string color) { using var c=Open(); using var cmd=c.CreateCommand(); cmd.CommandText="UPDATE PlannerItems SET FontColor=$color WHERE Id=$id"; cmd.Parameters.AddWithValue("$id",itemId); cmd.Parameters.AddWithValue("$color",color); cmd.ExecuteNonQuery(); }
    public void SetPlannerItemFontSize(long itemId, int fontSize) { using var c=Open(); using var cmd=c.CreateCommand(); cmd.CommandText="UPDATE PlannerItems SET FontSize=$fontSize WHERE Id=$id"; cmd.Parameters.AddWithValue("$id",itemId); cmd.Parameters.AddWithValue("$fontSize",fontSize); cmd.ExecuteNonQuery(); }
    public long SavePlannerItem(long planId, long? itemId, string itemType, string reference, string content, string? sourceValue = null, int? fontSize = null)
    {
        using var c = Open();
        using var cmd = c.CreateCommand();
        if (itemId.HasValue)
        {
            cmd.CommandText = "UPDATE PlannerItems SET PlanId=$planId, ItemType=$type, Reference=$reference, Content=$content, SourceValue=$sourceValue, FontSize=$fontSize WHERE Id=$id; SELECT $id;";
            cmd.Parameters.AddWithValue("$id", itemId.Value);
        }
        else
        {
            cmd.CommandText = "INSERT INTO PlannerItems(PlanId, ItemType, Reference, Content, SourceValue, FontSize, SortOrder) VALUES($planId, $type, $reference, $content, $sourceValue, $fontSize, COALESCE((SELECT MAX(SortOrder)+1 FROM PlannerItems WHERE PlanId=$planId),1)); SELECT last_insert_rowid();";
        }
        cmd.Parameters.AddWithValue("$planId", planId);
        cmd.Parameters.AddWithValue("$type", itemType);
        cmd.Parameters.AddWithValue("$reference", reference);
        cmd.Parameters.AddWithValue("$content", content);
        cmd.Parameters.AddWithValue("$sourceValue", (object?)sourceValue ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$fontSize", (object?)fontSize ?? DBNull.Value);
        return (long)cmd.ExecuteScalar()!;
    }

    public void DeletePlannerItem(long itemId)
    {
        using var c = Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "DELETE FROM PlannerItems WHERE Id=$id";
        cmd.Parameters.AddWithValue("$id", itemId);
        cmd.ExecuteNonQuery();
    }

    public void ReorderPlannerItems(long planId, IReadOnlyList<long> orderedIds)
    {
        using var c = Open();
        using var tx = c.BeginTransaction();
        using var cmd = c.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "UPDATE PlannerItems SET SortOrder=$sortOrder WHERE Id=$id AND PlanId=$planId";
        var sortOrder = 1;
        foreach (var id in orderedIds)
        {
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("$id", id);
            cmd.Parameters.AddWithValue("$planId", planId);
            cmd.Parameters.AddWithValue("$sortOrder", sortOrder++);
            cmd.ExecuteNonQuery();
        }
        tx.Commit();
    }

    public List<PlannerItem> GetPlannerItems(long planId) { using var c=Open(); using var cmd=c.CreateCommand(); cmd.CommandText="SELECT Id, ItemType, Reference, Content, FontColor, SourceValue, FontSize FROM PlannerItems WHERE PlanId=$planId ORDER BY SortOrder"; cmd.Parameters.AddWithValue("$planId", planId); using var r=cmd.ExecuteReader(); var items=new List<PlannerItem>(); while(r.Read())items.Add(new PlannerItem(r.GetInt64(0),r.GetString(1),r.GetString(2),r.GetString(3),r.IsDBNull(4)?null:r.GetString(4),r.IsDBNull(5)?null:r.GetString(5),r.IsDBNull(6)?(int?)null:r.GetInt32(6))); return items; }
    public List<BibleVerse> GetVerseRange(long bookId, int chapter, int fromVerse, int toVerse)
    {
        using var c = Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT Id, VerseNumber, Text FROM BibleVerses WHERE BookId=$bookId AND ChapterNumber=$chapter AND VerseNumber BETWEEN $fromVerse AND $toVerse ORDER BY VerseNumber";
        cmd.Parameters.AddWithValue("$bookId", bookId);
        cmd.Parameters.AddWithValue("$chapter", chapter);
        cmd.Parameters.AddWithValue("$fromVerse", Math.Min(fromVerse, toVerse));
        cmd.Parameters.AddWithValue("$toVerse", Math.Max(fromVerse, toVerse));
        using var r = cmd.ExecuteReader();
        var verses = new List<BibleVerse>();
        while (r.Read()) verses.Add(new BibleVerse(r.GetInt64(0), r.GetInt32(1), r.GetString(2)));
        return verses;
    }
    public List<Song> GetSongs(string? search = null)
    {
        using var c = Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = string.IsNullOrWhiteSpace(search)
            ? "SELECT Id, Title, Lyrics FROM Songs ORDER BY Title"
            : "SELECT Id, Title, Lyrics FROM Songs WHERE Title LIKE $search OR Lyrics LIKE $search ORDER BY Title";
        if (!string.IsNullOrWhiteSpace(search))
            cmd.Parameters.AddWithValue("$search", $"%{search.Trim()}%");
        using var r = cmd.ExecuteReader();
        var songs = new List<Song>();
        while (r.Read()) songs.Add(new Song(r.GetInt64(0), r.GetString(1), r.GetString(2)));
        return songs;
    }

    public long SaveSong(long? songId, string title, string lyrics)
    {
        using var c = Open();
        using var cmd = c.CreateCommand();
        if (songId.HasValue)
        {
            cmd.CommandText = "UPDATE Songs SET Title=$title, Lyrics=$lyrics WHERE Id=$id; SELECT $id;";
            cmd.Parameters.AddWithValue("$id", songId.Value);
        }
        else
        {
            cmd.CommandText = "INSERT INTO Songs(Title, Lyrics) VALUES($title, $lyrics); SELECT last_insert_rowid();";
        }
        cmd.Parameters.AddWithValue("$title", title.Trim());
        cmd.Parameters.AddWithValue("$lyrics", lyrics.Trim());
        return (long)cmd.ExecuteScalar()!;
    }

    public void DeleteSong(long songId)
    {
        using var c = Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "DELETE FROM Songs WHERE Id=$id";
        cmd.Parameters.AddWithValue("$id", songId);
        cmd.ExecuteNonQuery();
    }

    public List<MediaItem> GetMediaItems(string? type = null, string? search = null)
    {
        using var c = Open();
        using var cmd = c.CreateCommand();
        var conditions = new List<string>();
        if (!string.IsNullOrWhiteSpace(type) && !type.Equals("All", StringComparison.OrdinalIgnoreCase))
            conditions.Add("MediaType = $type");
        if (!string.IsNullOrWhiteSpace(search))
            conditions.Add("(Title LIKE $search OR FilePath LIKE $search)");
        cmd.CommandText = $"SELECT Id, MediaType, Title, FilePath FROM MediaItems{(conditions.Count > 0 ? " WHERE " + string.Join(" AND ", conditions) : string.Empty)} ORDER BY Title";
        if (!string.IsNullOrWhiteSpace(type) && !type.Equals("All", StringComparison.OrdinalIgnoreCase))
            cmd.Parameters.AddWithValue("$type", type);
        if (!string.IsNullOrWhiteSpace(search))
            cmd.Parameters.AddWithValue("$search", $"%{search.Trim()}%");
        using var r = cmd.ExecuteReader();
        var items = new List<MediaItem>();
        while (r.Read()) items.Add(new MediaItem(r.GetInt64(0), r.GetString(1), r.GetString(2), r.GetString(3)));
        return items;
    }

    public long SaveMediaItem(long? mediaId, string mediaType, string title, string filePath)
    {
        using var c = Open();
        using var cmd = c.CreateCommand();
        if (mediaId.HasValue)
        {
            cmd.CommandText = "UPDATE MediaItems SET MediaType=$type, Title=$title, FilePath=$path WHERE Id=$id; SELECT $id;";
            cmd.Parameters.AddWithValue("$id", mediaId.Value);
        }
        else
        {
            cmd.CommandText = "INSERT INTO MediaItems(MediaType, Title, FilePath) VALUES($type, $title, $path); SELECT last_insert_rowid();";
        }
        cmd.Parameters.AddWithValue("$type", mediaType.Trim());
        cmd.Parameters.AddWithValue("$title", title.Trim());
        cmd.Parameters.AddWithValue("$path", filePath.Trim());
        return (long)cmd.ExecuteScalar()!;
    }

    public void DeleteMediaItem(long mediaId)
    {
        using var c = Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "DELETE FROM MediaItems WHERE Id=$id";
        cmd.Parameters.AddWithValue("$id", mediaId);
        cmd.ExecuteNonQuery();
    }
    private long EnsureDefaultPlan(SqliteConnection connection)
    {
        using var lookup = connection.CreateCommand();
        lookup.CommandText = "SELECT Id FROM Plans WHERE Name='Default Plan'";
        var existing = lookup.ExecuteScalar();
        if (existing is long existingId)
            return existingId;

        using var insert = connection.CreateCommand();
        insert.CommandText = "INSERT INTO Plans(Name) VALUES('Default Plan'); SELECT last_insert_rowid();";
        return (long)insert.ExecuteScalar()!;
    }

    private SqliteConnection Open() { var c = new SqliteConnection(ConnectionString); c.Open(); using var pragma=c.CreateCommand(); pragma.CommandText="PRAGMA foreign_keys = ON;"; pragma.ExecuteNonQuery(); return c; }
    private static void Execute(SqliteConnection c, string sql) { using var command=c.CreateCommand(); command.CommandText=sql; command.ExecuteNonQuery(); }
}

internal sealed record BibleBook(long Id, string Name, int SortOrder) { public override string ToString() => Name; }
internal sealed record BibleVerse(long Id, int Number, string Text);
internal sealed record ServicePlan(long Id, string Name) { public override string ToString() => Name; }
internal sealed record PlannerItem(long Id, string ItemType, string Reference, string Content, string? FontColor, string? SourceValue, int? FontSize) { public override string ToString() => $"{ItemType}: {Reference}"; }
internal sealed record Song(long Id, string Title, string Lyrics) { public override string ToString() => Title; }
internal sealed record MediaItem(long Id, string MediaType, string Title, string FilePath) { public override string ToString() => $"{MediaType}: {Title}"; }
