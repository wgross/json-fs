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

    private const FileMode ReadJsonFileMode = FileMode.OpenOrCreate;

    public static JsonFsRootProvider FromFile(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        var fullPath = Path.GetFullPath(path);

        var watcher = new FileSystemWatcher();
        watcher.Path = Path.GetDirectoryName(fullPath)!;
        watcher.Filter = Path.GetFileName(fullPath);
        watcher.NotifyFilter = NotifyFilters.LastWrite;

        var provider = new JsonFsRootProvider(fullPath, watcher);

        provider.CreateOrReadFile();

        return provider;
    }

    public JsonFsRootProvider(string path, FileSystemWatcher watcher)
    {
        ArgumentNullException.ThrowIfNull(path, nameof(path));
        ArgumentNullException.ThrowIfNull(watcher, nameof(watcher));

        this.path = path;
        this.watcher = watcher;
        this.watcher.Changed += this.FileHasChanged;
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

    private void FileHasChanged(object sender, FileSystemEventArgs e) => this.rootNode = null;

    private JObject CreateOrReadFile()
    {
        if (File.Exists(this.path))
            return this.ReadFile();
        else
            return this.CreateFile();
    }

    private JObject CreateFile()
    {
        var newRoot = new JObject();

        try
        {
            this.watcher.EnableRaisingEvents = false;
            File.WriteAllText(this.path, newRoot.ToString());
        }
        finally
        {
            this.watcher.EnableRaisingEvents = true;
        }

        return newRoot;
    }

    private JObject ReadFile()
    {
        this.watcher.EnableRaisingEvents = false;

        using var file = File.Open(this.path, ReadJsonFileMode);

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