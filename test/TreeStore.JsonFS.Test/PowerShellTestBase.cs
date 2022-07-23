using System.Diagnostics.CodeAnalysis;

namespace TreeStore.JsonFS.Test;

public class PowerShellTestBase : IDisposable
{
    /// <summary>
    /// The <see cref="PowerShell"/> instance to be used in the test case.
    /// </summary>
    protected PowerShell PowerShell { get; }

    public PowerShellTestBase() => this.PowerShell = PowerShell.Create();

    public void Dispose()
    {
        this.PowerShell.Commands.Clear();
        this.PowerShell
            .AddCommand("Remove-PSDrive")
            .AddParameter("Name", "test")
            .Invoke();
        this.PowerShell.Dispose();

        if (File.Exists(this.JsonFilePath))
            File.Delete(this.JsonFilePath);
    }

    public string JsonFilePath = $"./{Guid.NewGuid()}.json";

    public void AssertJsonFileContent(Action<JObject> assertion) => assertion(JObject.Parse(File.ReadAllText(this.JsonFilePath)));

    /// <summary>
    /// Arranges a dictionary file system using the given data as root nodes payload.
    /// </summary>
    public virtual JObject ArrangeFileSystem(JObject payload)
    {
        File.WriteAllText(this.JsonFilePath, payload.ToString());

        JsonFsCmdletProvider.RootNodeProvider = JsonFsRootProvider.FromFile(this.JsonFilePath);

        this.ArrangeFileSystem();

        return payload;
    }

    /// <summary>
    /// Loads the module from the tests bin directory and creates a drive 'test'.
    /// </summary>
    protected void ArrangeFileSystem()
    {
        this.PowerShell.Commands.Clear();
        this.PowerShell
            .AddCommand("Import-Module")
            .AddArgument("./TreeStore.JsonFS.dll")
            .AddStatement()
            .AddCommand("New-PSDrive")
            .AddParameter("PSProvider", "JsonFS")
            .AddParameter("Name", "test")
            .AddParameter("Root", "")
            .Invoke();
        this.PowerShell.Commands.Clear();
    }

    protected static JObject DefaultRoot()
    {
        return new JObject()
        {
            ["value"] = new JValue(1),
            ["array"] = new JArray(new JValue(1), new JValue(2)),
            ["object"] = new JObject(),
        };
    }
}

public static class JObjectExtensions
{
    public static JObject ChildObject(this JObject parent, string name)
        => parent.Property(name)?.Value as JObject ?? throw new InvalidOperationException($"{name} isn't a JObject");

    public static bool TryGetJObject(this JObject parent, string name, [DoesNotReturnIf(true)] out JObject? jobject)
    {
        jobject = parent.Property(name)?.Value switch
        {
            JObject jo => jo,

            _ => null
        };

        return jobject is not null;
    }
}