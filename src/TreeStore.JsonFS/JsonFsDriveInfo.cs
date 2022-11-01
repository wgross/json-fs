namespace TreeStore.JsonFS;

public sealed class JsonFsDriveInfo : Core.Providers.TreeStoreDriveInfoBase
{
    public JsonFsDriveInfo(JsonFsRootProvider rootNodeProvider, PSDriveInfo driveInfo)
        : base(driveInfo)
    {
        this.RootNodeProvider = rootNodeProvider;
    }

    internal JsonFsRootProvider RootNodeProvider { get; }

    protected override IServiceProvider GetRootNodeProvider() => this.RootNodeProvider.GetRootNodeServieProvider();

}