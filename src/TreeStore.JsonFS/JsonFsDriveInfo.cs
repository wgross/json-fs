namespace TreeStore.JsonFS;

public sealed class JsonFsDriveInfo(JsonFsRootProvider rootNodeProvider, PSDriveInfo driveInfo) : TreeStoreDriveInfoBase(driveInfo)
{
    internal JsonFsRootProvider RootNodeProvider { get; } = rootNodeProvider;

    protected override IServiceProvider GetRootNodeProvider() => this.RootNodeProvider.GetRootNodeServieProvider();
}