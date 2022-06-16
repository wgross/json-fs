using System.Management.Automation;
using System.Management.Automation.Provider;
using TreeStore.Core.Providers;

namespace TreeStore.JsonFS;

[CmdletProvider(JsonFsCmdletProvider.Id, ProviderCapabilities.None)]
public sealed class JsonFsCmdletProvider : TreeStoreCmdletProviderBase
{
    public const string Id = "JsonFS";

    /// <summary>
    /// Creates the root node. The input string is the drive name.
    /// </summary>
    public static Func<string, IServiceProvider>? RootNodeProvider { get; set; }

    /// <summary>
    /// Creates a new drive from the given creation parameters in <paramref name="drive"/>.
    /// </summary>
    protected override PSDriveInfo NewDrive(PSDriveInfo drive)
    {
        if (RootNodeProvider is null)
            throw new InvalidOperationException(nameof(RootNodeProvider));

        return new JsonFsDriveInfo(RootNodeProvider, new PSDriveInfo(
           name: drive.Name,
           provider: drive.Provider,
           root: $@"{drive.Name}:\",
           description: drive.Description,
           credential: drive.Credential));
    }
}