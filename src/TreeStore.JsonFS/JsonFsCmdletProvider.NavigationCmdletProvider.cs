namespace TreeStore.JsonFS;

public partial class JsonFsCmdletProvider
{
    protected override void MoveItem(string path, string destination)
    {
        // check if the destination node is at the same provider
        var destinationSplitted = PathTool.Default.SplitProviderQualifiedPath(destination);

        if (string.IsNullOrEmpty(destinationSplitted.DriveName) || this.PSDriveInfo.Name.Equals(destinationSplitted.DriveName, StringComparison.OrdinalIgnoreCase))
        {
            // Yes, the move operation remains within the source file system: just call the base provider
            base.MoveItem(path, destination);
        }
        else
        {
            this.MoveItemToProvider(path, destination, destinationSplitted);
        }
    }

    private void MoveItemToProvider(string sourcePath, string destination, ProviderQualifiedPath destinationSplitted)
    {
        var splittedSourcePath = PathTool.Default.SplitProviderQualifiedPath(sourcePath);

        // the destination provider is different from this provider.
        // this has to be implemented specifically: get the drive info
        var destinationDriveInfo
            = this.GetTreeStoreDriveInfo<JsonFsDriveInfo>(destinationSplitted.DriveName)
            ?? throw new InvalidOperationException($"drive(name:{destinationSplitted.DriveName}) doesn't exists");

        var (parentPath, childName) = splittedSourcePath.ParentAndChild;

        var sourceDriveInfo = this.GetTreeStoreDriveInfo<JsonFsDriveInfo>(splittedSourcePath.DriveName);

        this.InvokeContainerNodeOrDefault(
            driveInfo: sourceDriveInfo,
            path: parentPath,
            invoke: sourceParentNode =>
            {
                // first check that node to move exists
                if (!sourceParentNode.TryGetChildNode(childName!, out var nodeToMove))
                    throw new InvalidOperationException($"Item '{sourcePath}' doesn't exist");

                if (nodeToMove.NodeServiceProvider is JObjectAdapter jobjectAdapter)
                {
                    if (this.SessionState.InvokeProvider.Item.Exists(destination))
                    {
                        this.MoveItemToProviderUnderExistingParent(sourcePath, destination, childName, jobjectAdapter);
                    }
                    else
                    {
                        // we have to create the parent path as the new destination path and use the last path item as the child name
                        var newParentPath = $@"{destinationSplitted.DriveName}:\{string.Join(@"\", destinationSplitted.Items[..^1])}";
                        var newChildName = destinationSplitted.Items.Last();

                        this.MoveItemToProviderUnderExistingParent(sourcePath, newParentPath, newChildName, jobjectAdapter);
                    }
                }
                else throw new InvalidOperationException($"Item '{sourcePath}' must be a JObject or JArray");
            },
            fallback: () => base.MoveItem(sourcePath, destination));
    }

    private void MoveItemToProviderUnderExistingParent(string source, string destination, string? childName, JObjectAdapter jobjectAdapter)
    {
        // just create a node under the existing arent node. This creates also the value properties
        this.SessionState.InvokeProvider.Item.New(destination, childName, null, jobjectAdapter.payload);

        // This is clumsy: A helper to stringify an ProviderQualifiedPath would be nice
        this.SessionState.InvokeProvider.Content.GetWriter($@"{destination}\{childName}")
            .First()
            .Write(new List<string> { jobjectAdapter.payload!.ToString() });

        // delete source after writing destination
        this.SessionState.InvokeProvider.Item.Remove(source, recurse: true);
    }
}