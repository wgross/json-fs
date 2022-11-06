namespace TreeStore.JsonFS;

[CmdletProvider(JsonFsCmdletProvider.Id, ProviderCapabilities.None)]
public partial class JsonFsCmdletProvider : TreeStoreCmdletProviderBase, IJsonFsRootNodeModification
{
    public const string Id = "JsonFS";

    /// <summary>
    /// Creates a new drive from the given creation parameters in <paramref name="drive"/>.
    /// </summary>
    protected override PSDriveInfo NewDrive(PSDriveInfo drive)
    {
        // path may contain wild cards: take only the first path.
        // if the path couldn't be resolved throw
        var jsonFilePath = this.SessionState.Path.GetUnresolvedProviderPathFromPSPath(drive.Root);
        if (jsonFilePath is null)
            throw new PSArgumentException($"Path: '{drive.Root}' couldn't be resolved");

        // create a function to set the current location to the created drive (<name:> { Set-Location -Path $MyInvocation.MyCommand.Name }
        this.InvokeCommand.InvokeScript($"Set-Item -Path \"Function:\\global:{drive.Name}`:\" -Value ([scriptblock]::Create(\"Set-Location -Path `$MyInvocation.MyCommand.Name\"))");

        return new JsonFsDriveInfo(JsonFsRootProvider.FromFile(jsonFilePath), new PSDriveInfo(
           name: drive.Name,
           provider: drive.Provider,
           root: $@"{drive.Name}:\",
           description: drive.Description,
           credential: drive.Credential));
    }

    protected override PSDriveInfo RemoveDrive(PSDriveInfo drive)
    {
        // remove the set location function from this session
        this.InvokeCommand.InvokeScript($"Remove-Item -Path \"Function:\\{drive.Name}`:\"");

        return drive;
    }

    IDisposable IJsonFsRootNodeModification.BeginModify() => this.PSDriveInfo switch
    {
        JsonFsDriveInfo jsonFsDriveInfo => jsonFsDriveInfo.RootNodeProvider.BeginModify(),

        _ => throw new InvalidOperationException($"The PSDriveInfo(type='{this.PSDriveInfo.GetType()}') doesn't support modification.")
    };

    //public static CompletionResult[] CompleteArgument(string commandName, string parameterName, string wordToComplete, CommandAst commansAst, Hashtable boundArguments)
    //{
    //    if ("Get-ItemProperty".Equals(commandName, StringComparison.OrdinalIgnoreCase))
    //    {
    //        CompleteGetItemProperty(parameterName, wordToComplete, commansAst, boundArguments);
    //    }
    //    return Array.Empty<CompletionResult>();
    //}

    //private static CompletionResult[] CompleteGetItemProperty(string parameterName, string wordToComplete, CommandAst commansAst, Hashtable boundArguments)
    //{
    //    if (!"Name".Equals(parameterName, StringComparison.OrdinalIgnoreCase))
    //        return Array.Empty<CompletionResult>();

    //    if(!boundArguments.ContainsKey("Path"))
    //        return Array.Empty<CompletionResult>();

    //    return Array.Empty<CompletionResult>();

    //}
}