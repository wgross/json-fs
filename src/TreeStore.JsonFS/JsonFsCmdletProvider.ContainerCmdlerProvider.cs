namespace TreeStore.JsonFS
{
    public sealed partial class JsonFsCmdletProvider
    {
        protected override void CopyItem(string path, string destination, bool recurse)
        {
            // check if the destination node is at the same provider
            var destinationSplitted = new PathTool().SplitProviderQualifiedPath(destination);

            if (string.IsNullOrEmpty(destinationSplitted.DriveName) || this.PSDriveInfo.Name.Equals(destinationSplitted.DriveName, StringComparison.OrdinalIgnoreCase))
            {
                // Yes, the copy operations remains within the source file system: just call the base provider
                base.CopyItem(path, destination, recurse);
            }
            else
            {
                this.CopyItemToProvider(path, destination, destinationSplitted, recurse);
            }
        }

        private void CopyItemToProvider(string path, string destination, ProviderQualifiedPath destinationSplitted, bool recurse)
        {
            // the destination provider is different from this provider.
            // this has to be implemented specifically: get the provider and the drive info
            var providerInfo = this.SessionState.Provider
                .GetAll()
                .SelectMany(p => p.Drives.Select(d => (provider: p, drive: d)))
                .Where(pd => StringComparer.OrdinalIgnoreCase.Equals(pd.drive.Name, destinationSplitted.DriveName))
                .FirstOrDefault();

            if (providerInfo.provider is null || providerInfo.drive is null)
                throw new InvalidOperationException($"drive(name:{destinationSplitted.DriveName}) doesn't exists");

            if (providerInfo.drive is JsonFsDriveInfo jsonFsDrive)
            {
                var (parentPath, childName) = new PathTool().SplitProviderQualifiedPath(path).ParentAndChild;

                this.InvokeContainerNodeOrDefault(
                    path: parentPath,
                    invoke: sourceParentNode =>
                    {
                        // first check that node to copy exists
                        if (!sourceParentNode.TryGetChildNode(childName!, out var nodeToCopy))
                            throw new InvalidOperationException($"Item '{path}' doesn't exist");

                        if (nodeToCopy.NodeServiceProvider is JObjectAdapter jobjectAdapter)
                        {
                            if (this.SessionState.InvokeProvider.Item.Exists(destination))
                            {
                                this.CopyItemToProviderUnderExistingParent(destination, recurse, childName, jobjectAdapter);
                            }
                            else
                            {
                                // we have to create the parent path as the new destinatin path and use the last path item as the child name
                                var newParentPath = $@"{destinationSplitted.DriveName}:\{string.Join(@"\", destinationSplitted.Items[..^1])}";
                                var newChildName = destinationSplitted.Items.Last();

                                this.CopyItemToProviderUnderExistingParent(newParentPath, recurse, newChildName, jobjectAdapter);
                            }
                        }
                        else throw new InvalidOperationException($"Item '{path}' must be a JObject or JArray");
                    },
                    fallback: () => base.CopyItem(path, destination, recurse));
            }
        }

        private void CopyItemToProviderUnderExistingParent(string destination, bool recurse, string? childName, JObjectAdapter jobjectAdapter)
        {
            // just create a node under the existing arent node. This creates also the value properties
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
}