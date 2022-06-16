using System.Management.Automation;

namespace TreeStore.JsonFS;

public sealed class JsonFsDriveInfo : TreeStore.Core.Providers.TreeStoreDriveInfoBase
{
    public JsonFsDriveInfo(Func<string, IServiceProvider> rootNodeProvider, PSDriveInfo driveInfo)
        : base(driveInfo, rootNodeProvider)
    {
    }
}