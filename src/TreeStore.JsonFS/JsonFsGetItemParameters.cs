namespace TreeStore.JsonFS;

public sealed class JsonFsGetItemParameters
{
    /// <summary>
    /// Returned data structure is bases on a <see cref="Hastable"/> instead of a <see cref="JsonFsItem"/>.
    /// </summary>
    [Parameter]
    public SwitchParameter AsHashtable { get; set; }
}