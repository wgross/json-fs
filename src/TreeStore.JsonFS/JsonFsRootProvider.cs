namespace TreeStore.JsonFS;

public sealed class JsonFsRootProvider
{
    public record class DisposableAutoSave(JsonFsRootProvider jsonFsProvider, FileStream fileStream) : IDisposable
    {
        public void Dispose() => this.jsonFsProvider.WriteFile(this.fileStream);
    }

    public record class DisposableDummy() : IDisposable
    {
        public void Dispose() { }
    }

    private const FileMode WriteJsonFileMode = FileMode.OpenOrCreate | FileMode.Truncate;

    public static JsonFsRootProvider FromFile(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        var watcher = new FileSystemWatcher();
        watcher.Path = Path.GetDirectoryName(path)!;
        watcher.Filter = Path.GetFileName(path);
        watcher.NotifyFilter = NotifyFilters.LastWrite;

        var provider = new JsonFsRootProvider(path, watcher);

        provider.ReadFile();

        return provider;
    }

    public JsonFsRootProvider(string path, FileSystemWatcher watcher)
    {
        ArgumentNullException.ThrowIfNull(path, nameof(path));
        ArgumentNullException.ThrowIfNull(watcher, nameof(watcher));

        this.path = path;
        this.watcher = watcher;
        this.watcher.Changed += this.ReadFile;
    }

    #region Provide the root node to the Cmdlet provider

    private JObject? rootNode;

    public JObject GetRootJObject()
    {
        if (this.rootNode is null)
            this.rootNode = this.ReadFile();

        return this.rootNode;
    }

    public IServiceProvider GetRootNodeServieProvider() => new JObjectAdapter(this.GetRootJObject());

    #endregion Provide the root node to the Cmdlet provider

    #region Read JSON from file and mark dirty if file has been changed

    private readonly string path;
    private readonly FileSystemWatcher watcher;

    private void ReadFile(object sender, FileSystemEventArgs e) => this.rootNode = null;

    private JObject ReadFile()
    {
        this.watcher.EnableRaisingEvents = false;

        using var file = File.Open(this.path, FileMode.Open);
        try
        {
            using var stream = new StreamReader(file);
            using var reader = new JsonTextReader(stream);

            return JObject.Load(reader);
        }
        finally
        {
            file.Close();
            this.watcher.EnableRaisingEvents = true;
        }
    }

    #endregion Read JSON from file and mark dirty if file has been changed

    #region Save JSON to file

    private IDisposable? pendingSave;

    public IDisposable BeginModify()
    {
        if (this.pendingSave is null)
        {
            // switch the notifications off for now.
            // accessing the file raises a notification already.
            // maybe exclude this  case in the future. Might be too much b/c just reading
            // it is not an change event
            this.watcher.EnableRaisingEvents = false;

            return (this.pendingSave = new DisposableAutoSave(
                jsonFsProvider: this,
                fileStream: File.Open(path: this.path, WriteJsonFileMode)));
        }
        else return new DisposableDummy();
    }

    internal void WriteFile(FileStream fileStream)
    {
        this.watcher.EnableRaisingEvents = false;
        try
        {
            using (fileStream)
            {
                using var streamWriter = new StreamWriter(fileStream);

                streamWriter.Write(this.GetRootJObject()!.ToString());
            }
        }
        finally
        {
            this.pendingSave = null;
            this.watcher.EnableRaisingEvents = true;
        }
    }

    #endregion Save JSON to file
}