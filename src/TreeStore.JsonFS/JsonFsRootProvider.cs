using NJsonSchema;

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
    private JsonSchema? cachedJsonSchema;

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
                this.WriteJsonFile(this.GetRootJObject().ToString());
            }
            else
            {
                // is ist recommended to match the JSON content as string b/c date/time formats
                // https://github.com/RicoSuter/NJsonSchema/wiki/JsonSchema#validatestring-vs-validatejtoken

                var jsonContent = this.GetRootJObject().ToString();

                var errorMessages = latestJsonSchema.Validate(jsonContent);
                if (errorMessages.Count > 0)
                {
                    // invalid json is removed from memory
                    this.rootNode = null;

                    throw new InvalidOperationException(string.Join(";", errorMessages));
                }
                else this.WriteJsonFile(jsonContent);
            }
        }
        finally
        {
            this.pendingSave = null;
        }
    }

    private JsonSchema? GetJsonSchema()
    {
        if (this.cachedJsonSchema is not null)
            return this.cachedJsonSchema;

        // the JSON scahme was given as a parameter to the drive
        // try this first

        if (this.jsonSchemaFile is not null)
        {
            this.jsonSchemaFile.Refresh();

            if (this.jsonSchemaFile.Exists)
                this.cachedJsonSchema = this.ReadJsonSchemaFile(this.jsonSchemaFile);
        }

        // the JSON schame might by a property in the root object
        // thsi may ba an http or file scheme.

        var root = this.GetRootJObject();

        if (root.TryGetValue("$schema", out var jsonSchameUrl))
        {
            this.cachedJsonSchema = this.ReadJsonSchemaUrl(jsonSchameUrl.Value<string>());
        }

        return this.cachedJsonSchema;
    }

    private JsonSchema? ReadJsonSchemaFile(FileInfo jsonSchemaFile) => Await(JsonSchema.FromFileAsync(jsonSchemaFile.FullName));

    private JsonSchema? ReadJsonSchemaUrl(string? uri) => uri is null ? null : this.ReadJsonSchemaUrl(new Uri(uri));

    private JsonSchema? ReadJsonSchemaUrl(Uri uri)
    {
        // NJsonSchema supports reading from http(s): scheme but not from file: scheme
        return uri switch
        {
            { Scheme: "http" } => this.ReadJsonSchemaUrlOverHttp(uri),
            { Scheme: "https" } => this.ReadJsonSchemaUrlOverHttp(uri),
            { Scheme: "file" } => this.ReadJsonSchemaUrlFromFile(uri),

            _ => null,
        };
    }

    private JsonSchema? ReadJsonSchemaUrlFromFile(Uri uri) => Await(JsonSchema.FromFileAsync(uri.LocalPath));

    private JsonSchema? ReadJsonSchemaUrlOverHttp(Uri uri) => Await(JsonSchema.FromUrlAsync(uri.AbsoluteUri));

    #endregion Follow pending modification

    #region Save JSON to file

    private void WriteJsonFile(string jsonContent) => this.WriteJsonFile(this.jsonFile, jsonContent);

    private void WriteJsonFile(FileInfo file, string jsonContent) => this.WriteJsonFile(this.OpenOrCreateFileForWriting(file), jsonContent);

    private void WriteJsonFile(FileStream fileStream, string jsonContent)
    {
        this.watcher.EnableRaisingEvents = false;
        try
        {
            using var streamWriter = new StreamWriter(fileStream);

            streamWriter.Write(jsonContent);
        }
        finally
        {
            fileStream.Dispose();

            this.watcher.EnableRaisingEvents = true;
        }
    }

    #endregion Save JSON to file

    private static T Await<T>(Task<T> task) => task.GetAwaiter().GetResult();
}