namespace TreeStore.JsonFS;

public sealed partial class JsonFsCmdletProvider
{
    protected override void CopyItem(string path, string destination, bool recurse)
    {
        // check if the destination node is at the same provider
        var destinationSplitted = PathTool.Default.SplitProviderQualifiedPath(destination);

        if (string.IsNullOrEmpty(destinationSplitted.DriveName) || this.PSDriveInfo.Name.Equals(destinationSplitted.DriveName, StringComparison.OrdinalIgnoreCase))
        {
            // Yes, the copy operations remains within the source file system: just call the base provider
            base.CopyItem(path, destination, recurse);
        }
        else
        {
            // destination file system is different
            this.CopyItemToProvider(path, destination, destinationSplitted, recurse);
        }
    }

    private void CopyItemToProvider(string sourcePath, string destinationPath, ProviderQualifiedPath destinationPathSplitted, bool recurse)
    {
        // the destination provider is different from this provider.
        // this has to be implemented specifically: get the provider and the drive info that
        // matches the destination drive name.

        var destinationProviderInfo = this.SessionState.Provider
            .GetAll()
            .SelectMany(p => p.Drives.Select(d => (provider: p, drive: d)))
            .Where(pd => StringComparer.OrdinalIgnoreCase.Equals(pd.drive.Name, destinationPathSplitted.DriveName))
            .FirstOrDefault();

        if (destinationProviderInfo is not { provider: not null, drive: not null })
            throw new InvalidOperationException($"drive(name:{destinationPathSplitted.DriveName}) doesn't exists");

        var sourcePathSplitted = PathTool.Default.SplitProviderQualifiedPath(sourcePath);

        var sourceProviderInfo = this.SessionState.Provider
            .GetAll()
            .SelectMany(p => p.Drives.Select(d => (provider: p, drive: d)))
            .Where(pd => StringComparer.OrdinalIgnoreCase.Equals(pd.drive.Name, sourcePathSplitted.DriveName))
            .FirstOrDefault();

        if (destinationProviderInfo.drive is JsonFsDriveInfo jsonFsDestinationDrive && sourceProviderInfo.drive is JsonFsDriveInfo jsonFsSourceDrive)
        {
            var (sourceParentPath, sourceChildName) = sourcePathSplitted.ParentAndChild;

            this.InvokeContainerNodeOrDefault(
                driveInfo: this.GetTreeStoreDriveInfo<JsonFsDriveInfo>(sourcePathSplitted.DriveName),
                path: sourceParentPath,
                invoke: sourceParentNode =>
                {
                    // first check that node to copy exists then fetch the JObjectAdapter from the source node
                    // and copy its content to the destination

                    if (!sourceParentNode.TryGetChildNode(sourceChildName!, out var sourceNodeToCopy))
                        throw new InvalidOperationException($"Item '{sourcePath}' doesn't exist");

                    if (sourceNodeToCopy.NodeServiceProvider is JObjectAdapter sourceJobjectAdapter)
                    {
                        if (this.SessionState.InvokeProvider.Item.Exists(destinationPath))
                        {
                            this.CopyItemToProviderUnderExistingParent(destinationPath, recurse, sourceChildName, sourceJobjectAdapter);
                        }
                        else
                        {
                            // we have to create the parent path as the new destination path and use the last path item as the child name
                            var newParentPath = $@"{destinationPathSplitted.DriveName}:\{string.Join(@"\", destinationPathSplitted.Items[..^1])}";
                            var newChildName = destinationPathSplitted.Items.Last();

                            this.CopyItemToProviderUnderExistingParent(newParentPath, recurse, newChildName, sourceJobjectAdapter);
                        }
                    }
                    else throw new InvalidOperationException($"Item '{sourcePath}' must be a JObject or JArray");
                },
                fallback: () => base.CopyItem(sourcePath, destinationPath, recurse));
        }
    }

    private void CopyItemToProviderUnderExistingParent(string destination, bool recurse, string? childName, JObjectAdapter jobjectAdapter)
    {
        // just create a node under the existing aren't node. This creates also the value properties
        this.SessionState.InvokeProvider.Item.New(destination, childName, null, jobjectAdapter.payload);

        if (recurse)
        {
            // is the copy is recursive, the content is overwritten again from the serialized JToken.
            // containg the sub nodes as well.

            // This is clumsy: A helper to stringify an ProviderQualifiedPath would be nice
            this.SessionState.InvokeProvider.Content.GetWriter($@"{destination}\{childName}")
                .First()
                .Write(new List<string> { jobjectAdapter.payload!.ToString() });
        }
    }
}