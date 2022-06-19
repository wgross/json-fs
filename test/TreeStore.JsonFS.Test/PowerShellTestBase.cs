using Newtonsoft.Json.Linq;
using System;
using System.Management.Automation;

namespace TreeStore.JsonFS.Test;

public class PowerShellTestBase : IDisposable
{
    /// <summary>
    /// The <see cref="PowerShell"/> instance to be used in the test case.
    /// </summary>
    protected PowerShell PowerShell { get; }

    public PowerShellTestBase() => this.PowerShell = PowerShell.Create();

    public void Dispose() => this.PowerShell.Dispose();

    /// <summary>
    /// Arranges a dictionary file system using the given data as root nodes payload.
    /// </summary>
    public JObject ArrangeFileSystem(JObject payload)
    {
        JsonFsCmdletProvider.RootNodeProvider = _ => new JObjectAdapter(payload);

        this.ArrangeFileSystem();

        return payload;
    }

    /// <summary>
    /// Loads the module from the tests bin directory and creates a drive 'test'.
    /// </summary>
    protected void ArrangeFileSystem()
    {
        this.PowerShell.AddCommand("Import-Module")
            .AddArgument("./TreeStore.JsonFS.dll")
            .Invoke();
        this.PowerShell.Commands.Clear();
        this.PowerShell.AddCommand("New-PSDrive")
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
}