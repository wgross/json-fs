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
            this.rootNode = this.ReadFile(new FileInfo(this.path));

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
        var sourceFile = new FileInfo(this.path);
        if (sourceFile.Exists)
            return this.ReadFile(sourceFile);
        else
            return this.CreateFile(sourceFile);
    }

    private JObject CreateFile(FileInfo sourceFile)
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

    private JObject ReadFile(FileInfo sourceFile)
    {
        this.watcher.EnableRaisingEvents = false;

        try
        {
            using var streamReader = sourceFile.OpenText();
            using var jsonReader = new JsonTextReader(streamReader);

            return JObject.Load(jsonReader);
        }
        catch (JsonReaderException ex)
        {
            if (sourceFile.Length > 0)
            {
                // there is something different in the file.
                throw;
            }
            else
            {
                // just return the JObject and overwrite whats in the file on change.
                return new JObject();
            }
        }
        finally
        {
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