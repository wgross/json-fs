using Newtonsoft.Json.Schema;

namespace TreeStore.JsonFS;

public sealed class JsonFsRootProvider
{
    public static JsonFsRootProvider FromFile(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        var jsonFile = new FileInfo(Path.GetFullPath(path));

        var jsonFileWatcher = new FileSystemWatcher();
        jsonFileWatcher.Path = jsonFile.Directory!.FullName;
        jsonFileWatcher.Filter = jsonFile.Name;
        jsonFileWatcher.NotifyFilter = NotifyFilters.LastWrite;

        var provider = new JsonFsRootProvider(jsonFile, jsonSchemaFile: null, jsonFileWatcher);

        provider.CreateOrReadJsonFile();

        return provider;
    }

    public static JsonFsRootProvider FromFileAndSchema(string path, string schemaPath)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(schemaPath);

        var jsonFile = new FileInfo(Path.GetFullPath(path));
        var jsonSchema = new FileInfo(Path.GetFullPath(schemaPath));

        var jsonFileWatcher = new FileSystemWatcher();
        jsonFileWatcher.Path = jsonFile.Directory!.FullName;
        jsonFileWatcher.Filter = Path.GetFileName(jsonFile.Name);
        jsonFileWatcher.NotifyFilter = NotifyFilters.LastWrite;

        var provider = new JsonFsRootProvider(jsonFile, jsonSchema, jsonFileWatcher);

        provider.CreateOrReadJsonFile();

        return provider;
    }

    public JsonFsRootProvider(FileInfo jsonFile, FileInfo? jsonSchemaFile, FileSystemWatcher watcher)
    {
        ArgumentNullException.ThrowIfNull(jsonFile, nameof(jsonFile));
        ArgumentNullException.ThrowIfNull(watcher, nameof(watcher));

        this.jsonFile = jsonFile;
        this.jsonSchemaFile = jsonSchemaFile;
        this.watcher = watcher;
        this.watcher.Changed += this.FileHasChanged;
    }

    #region Provide the root node to the Cmdlet provider

    private JObject? rootNode;
    private JSchema? cachedJsonSchema;

    public JObject GetRootJObject()
    {
        if (this.rootNode is null)
            this.rootNode = this.ReadJsonFile(this.jsonFile);

        return this.rootNode;
    }

    public IServiceProvider GetRootNodeServieProvider() => new JObjectAdapter(this.GetRootJObject());

    #endregion Provide the root node to the Cmdlet provider

    private readonly FileInfo jsonFile;
    private readonly FileInfo? jsonSchemaFile;
    private readonly FileSystemWatcher watcher;

    private FileStream OpenOrCreateFileForWriting(FileInfo sourceFile) => File.Open(sourceFile.FullName, FileMode.OpenOrCreate | FileMode.Truncate);

    private FileStream OpenOrCreateFileForReading(FileInfo sourceFile) => File.Open(sourceFile.FullName, FileMode.OpenOrCreate);

    #region Mark JSON in memory as dirty on file system change

    private void FileHasChanged(object sender, FileSystemEventArgs e) => this.rootNode = null;

    #endregion Mark JSON in memory as dirty on file system change

    #region Create empty JSON file

    private JObject CreateOrReadJsonFile()
    {
        this.jsonFile.Refresh();
        if (this.jsonFile.Exists)
            return this.ReadJsonFile(this.jsonFile);
        else
            return this.CreateJsonFile(this.jsonFile);
    }

    private JObject CreateJsonFile(FileInfo sourceFile) => this.CreateJsonFile(this.OpenOrCreateFileForWriting(sourceFile));

    private JObject CreateJsonFile(FileStream sourceFile)
    {
        var newRoot = new JObject();

        try
        {
            this.watcher.EnableRaisingEvents = false;

            using var writer = new StreamWriter(sourceFile);

            writer.Write(newRoot.ToString());
        }
        finally
        {
            this.watcher.EnableRaisingEvents = true;
        }

        return newRoot;
    }

    #endregion Create empty JSON file

    #region Read JSON from file

    private JObject ReadJsonFile(FileInfo sourceFile) => this.ReadJsonFile(this.OpenOrCreateFileForReading(sourceFile));

    private JObject ReadJsonFile(FileStream sourceFile)
    {
        this.watcher.EnableRaisingEvents = false;

        try
        {
            using var streamReader = new StreamReader(sourceFile);
            using var jsonReader = new JsonTextReader(streamReader);

            return JObject.Load(jsonReader);
        }
        catch (JsonReaderException)
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

    #endregion Read JSON from file

    #region Follow pending modification

    private IDisposable? pendingSave;

    public IDisposable BeginModify()
    {
        if (this.pendingSave is null)
        {
            // switch the notifications off for now.
            // accessing the file raises a notification already.
            this.watcher.EnableRaisingEvents = false;

            this.pendingSave = Disposables.FromAction(this.EndModify);

            return this.pendingSave;
        }
        else return Disposables.Empty();
    }

    public void EndModify()
    {
        try
        {
            var latestJsonSchema = this.GetJsonSchema();

            if (latestJsonSchema is null)
            {
                this.WriteJsonFile(this.jsonFile);
            }
            else
            {
                IList<string> errorMessages = new List<string>();
                if (!this.GetRootJObject().IsValid(latestJsonSchema, out errorMessages))
                {
                    // ivalid json is removed from memory
                    this.rootNode = null;

                    throw new InvalidOperationException(string.Join(";", errorMessages));
                }
                else this.WriteJsonFile();
            }
        }
        finally
        {
            this.pendingSave = null;
        }
    }

    private JSchema? GetJsonSchema()
    {
        if (this.cachedJsonSchema is not null)
            return this.cachedJsonSchema;

        if (this.jsonSchemaFile is null)
            return null;

        this.jsonSchemaFile.Refresh();

        if (this.jsonSchemaFile.Exists)
            this.cachedJsonSchema = this.ReadJsonSchemaFile(this.jsonSchemaFile);

        return this.cachedJsonSchema;
    }

    //public JSchema? GetJsonSchema(JObject root) => root.Property("$schema", StringComparison.OrdinalIgnoreCase) switch
    //{
    //    JProperty { Value.Type: JTokenType.String } jproperty => this.ReadJsonSchema(jproperty.Value.ToString()),

    //    _ => null
    //};

    private JSchema? ReadJsonSchemaFile(FileInfo jsonSchemaFile)
    {
        using var schemaStream = new StreamReader(jsonSchemaFile.OpenRead());
        using var jsonSchemaReader = new JsonTextReader(schemaStream);

        return JSchema.Load(jsonSchemaReader);
    }

    #endregion Follow pending modification

    #region Save JSON to file

    private void WriteJsonFile() => this.WriteJsonFile(this.jsonFile);

    private void WriteJsonFile(FileInfo file) => this.WriteJsonFile(this.OpenOrCreateFileForWriting(file));

    private void WriteJsonFile(FileStream fileStream)
    {
        this.watcher.EnableRaisingEvents = false;
        try
        {
            var root = this.GetRootJObject();

            // verify the schema first

            using var streamWriter = new StreamWriter(fileStream);

            streamWriter.Write(root.ToString());
        }
        finally
        {
            fileStream.Dispose();

            this.watcher.EnableRaisingEvents = true;
        }
    }

    #endregion Save JSON to file
}