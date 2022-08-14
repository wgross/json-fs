namespace TreeStore.JsonFS;

[CmdletProvider(JsonFsCmdletProvider.Id, ProviderCapabilities.None)]
public sealed class JsonFsCmdletProvider : TreeStoreCmdletProviderBase, IJsonFsRootNodeModification
{
    public const string Id = "JsonFS";

    /// <summary>
    /// Creates a new drive from the given creation parameters in <paramref name="drive"/>.
    /// </summary>
    protected override PSDriveInfo NewDrive(PSDriveInfo drive)
    {
        // path may contain wild cards: take only the first path.
        // if the path couldn't be resolved throw
        var jsonFilePath = this.SessionState.Path.GetResolvedProviderPathFromPSPath(drive.Root, out var _).FirstOrDefault();
        if (jsonFilePath is null)
            throw new PSArgumentException($"Path: '{drive.Root}' couldn't be resolved");

        return new JsonFsDriveInfo(JsonFsRootProvider.FromFile(jsonFilePath), new PSDriveInfo(
           name: drive.Name,
           provider: drive.Provider,
           root: $@"{drive.Name}:\",
           description: drive.Description,
           credential: drive.Credential));
    }

    IDisposable IJsonFsRootNodeModification.BeginModify() => this.PSDriveInfo switch
    {
        JsonFsDriveInfo jsonFsDriveInfo => jsonFsDriveInfo.RootNodeProvider.BeginModify(),

        _ => throw new InvalidOperationException($"The PSDriveInfo(type='{this.PSDriveInfo.GetType()}') doesn't support modification.")
    };
}