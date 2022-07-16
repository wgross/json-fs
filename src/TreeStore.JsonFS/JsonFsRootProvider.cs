using Microsoft.Win32.SafeHandles;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TreeStore.JsonFS;

public record class Dispoable(SafeFileHandle handle) : IDisposable
{
    public void Dispose() => this.handle.Dispose();
}

public sealed class JsonFsRootProvider
{
    private readonly string path;
    private readonly FileSystemWatcher watcher;
    private JObject? rootNode;

    public JsonFsRootProvider(string path, FileSystemWatcher watcher)
    {
        ArgumentNullException.ThrowIfNull(path, nameof(path));
        ArgumentNullException.ThrowIfNull(watcher, nameof(watcher));

        this.path = path;
        this.watcher = watcher;
        this.watcher.Changed += this.ReadFile;
    }

    private void ReadFile(object sender, FileSystemEventArgs e) => this.rootNode = null;

    private void ReadFile()
    {
        this.watcher.EnableRaisingEvents = false;

        using var file = File.Open(this.path!, FileMode.Open);
        try
        {
            using var stream = new StreamReader(file);
            using var reader = new JsonTextReader(stream);

            this.rootNode = JObject.Load(reader);
        }
        finally
        {
            file.Close();
            this.watcher.EnableRaisingEvents = true;
        }
    }

    public static JsonFsRootProvider FromFile(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        var watcher = new FileSystemWatcher();
        watcher.Path = Path.GetDirectoryName(path);
        watcher.Filter = Path.GetFileName(path);
        watcher.NotifyFilter = NotifyFilters.LastWrite;

        var provider = new JsonFsRootProvider(path, watcher);

        provider.ReadFile();

        return provider;
    }

    public JObject? GetRoot()
    {
        if (this.rootNode is null)
            this.ReadFile();

        return this.rootNode;
    }

    public IDisposable Lock() => File.OpenHandle(path: this.path, mode: FileMode.OpenOrCreate, access: FileAccess.ReadWrite, share: FileShare.None);
}