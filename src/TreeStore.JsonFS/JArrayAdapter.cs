using System.Globalization;

namespace TreeStore.JsonFS;

public sealed class JArrayAdapter : JAdapterBase,
    // ItemCmdletProvider
    IGetItem, ISetItem,
    // ContainerCmdletProvider
    IGetChildItem, IRemoveChildItem, INewChildItem,
    // NOT SUPPORTED YET
    // ICopyChildItem
    IGetItemContent, ISetChildItemContent, IClearItemContent
{
    internal readonly JArray payload;

    public JArrayAdapter(JArray payload) => this.payload = payload;

    #region IGetItem

    PSObject IGetItem.GetItem(ICmdletProvider provider) => PSObject.AsPSObject(new JsonFsItem(this.GetNameFromParent(this.payload), Array.Empty<string>()));

    #endregion IGetItem

    #region ISetItem

    void ISetItem.SetItem(ICmdletProvider provider, object? value)
    {
        using var handle = this.BeginModify(provider);

        if (value is JArray jarray && IsChildToken(jarray))
        {
            this.payload.Clear();

            foreach (var child in jarray)
                this.payload.Add(child);
        }
        else if (value is string json)
        {
            var jarrayFromString = JArray.Parse(json);

            this.payload.Clear();

            foreach (var child in jarrayFromString)
                this.payload.Add(child);
        }
    }

    internal void ClearItemContent(ICmdletProvider provider)
    {
        throw new NotImplementedException();
    }

    #endregion ISetItem

    #region IGetChildItem

    bool IGetChildItem.HasChildItems(ICmdletProvider provider)
        => this.payload.Where(jt => IsChildToken(jt)).Any();

    IEnumerable<ProviderNode> IGetChildItem.GetChildItems(ICmdletProvider provider)
        => this.payload.Select((jt, idx) => this.CreateChildNode(provider, jt, idx));

    private ProviderNode CreateChildNode(ICmdletProvider provider, JToken value, int index)
        => new ContainerNode(provider, index.ToString(CultureInfo.InvariantCulture), this.CreateChildAdapter(value));

    private IServiceProvider CreateChildAdapter(JToken value) => value switch
    {
        JArray jarray => new JArrayAdapter(jarray),

        JObject jobject => new JObjectAdapter(jobject),

        _ => throw new NotImplementedException($"Can't create adapter for token(type='{value.GetType()}')")
    };

    #endregion IGetChildItem

    #region IRemoveChildItem

    void IRemoveChildItem.RemoveChildItem(ICmdletProvider provider, string childName, bool recurse)
    {
        if (!int.TryParse(childName, out var index))
            return;

        if (index >= this.payload.Count)
            return;

        var childNode = this.CreateChildAdapter(this.payload[index]);

        if (!recurse && (childNode.GetRequiredService<IGetChildItem>()?.HasChildItems(provider) ?? false))
            return;

        var childItem = this.payload[index];

        using var handle = this.BeginModify(provider);

        this.payload.RemoveAt(index);
    }

    #endregion IRemoveChildItem

    #region INewChildItem

    /// <inheritdoc/>
    object? INewChildItem.NewChildItemParameters(string? childName, string? itemTypeName, object? newItemValue)
        => new JsonFsNewChildItemParameters();

    /// <inheritdoc/>
    NewChildItemResult INewChildItem.NewChildItem(ICmdletProvider provider, string? childName, string? itemTypeName, object? newItemValue)
    {
        var newValue = this.EvaluateNewItemValue(itemTypeName, newItemValue);

        if (string.IsNullOrEmpty(childName))
        {
            return this.AppendNewArrrayItem(provider, newValue);
        }

        if (!int.TryParse(childName, out var index))
        {
            return new NewChildItemResult(false, null, null);
        }

        if (provider.DynamicParameters is JsonFsNewChildItemParameters parameters)
        {
            if (index < this.payload.Count && parameters.Insert.ToBool())
            {
                return this.InsertNewArrayItem(provider, index, newValue);
            }
        }

        return new NewChildItemResult(false, null, null);
    }

    private JToken EvaluateNewItemValue(string? itemTypeName, object? newItemValue)
    {
        return newItemValue switch
        {
            string stringValue => JObject.Parse(stringValue),

            _ => new JObject()
        };
    }

    internal NewChildItemResult AppendNewArrrayItem(ICmdletProvider provider, JToken newItemValue)
    {
        using var handle = this.BeginModify(provider);

        this.payload.Add(newItemValue);

        return new NewChildItemResult(true, (this.payload.Count - 1).ToString(CultureInfo.InvariantCulture), this.CreateChildAdapter(newItemValue));
    }

    private NewChildItemResult InsertNewArrayItem(ICmdletProvider provider, int index, JToken newItemValue)
    {
        using var handle = this.BeginModify(provider);

        this.payload.Insert(index, newItemValue);

        return new NewChildItemResult(true, index.ToString(CultureInfo.InvariantCulture), this.CreateChildAdapter(newItemValue));
    }

    #endregion INewChildItem

    #region IGetItemContent

    /// <inheritdoc/>
    IContentReader? IGetItemContent.GetItemContentReader(ICmdletProvider provider) => new JArrayContentReader(this.payload);

    #endregion IGetItemContent

    #region ISetItemCOntent

    /// <inheritdoc/>
    IContentWriter? ISetChildItemContent.GetChildItemContentWriter(ICmdletProvider provider, string childName) => new JArrayContentWriter(provider, this);

    #endregion ISetItemCOntent

    #region IClearItemContent

    void IClearItemContent.ClearItemContent(ICmdletProvider provider)
    {
        using var handle = this.BeginModify(provider);

        this.payload.RemoveAll();
    }

    #endregion IClearItemContent

    #region // NOT SUPPORTED YET // ICopyChildItem

    //CopyChildItemResult ICopyChildItem.CopyChildItem(ICmdletProvider provider, ProviderNode nodeToCopy, string[] destination)
    //{
    //    return nodeToCopy.NodeServiceProvider switch
    //    {
    //        JObjectAdapter jobjectToCopy => this.CopyObjectToThisNode(provider, nodeToCopy, jobjectToCopy, destination),

    //        JArrayAdapter jarrayToCopy => this.CopyArrayToThisNode(provider, nodeToCopy, jarrayToCopy, destination),

    //        _ => new(false, default, default)
    //    };
    //}

    //private CopyChildItemResult CopyArrayToThisNode(ICmdletProvider provider, ProviderNode nodeToCopy, JArrayAdapter jarrayToCopy, string[] destination)
    //{
    //    throw new NotImplementedException();
    //}

    //private CopyChildItemResult CopyObjectToThisNode(ICmdletProvider provider, ProviderNode nodeToCopy, JObjectAdapter jobjectToCopy, string[] destination)
    //{
    //    if (!int.TryParse(nodeToCopy.Name, out var index))
    //        throw new ArgumentException($"Node(name='{nodeToCopy.Name}') can't be created: name must be a number");

    //    if (this.payload.Count > index)
    //    {
    //        using var handle = this.BeginModify(provider);

    //        this.payload[index] = CreateShallowObjectClone(jobjectToCopy.payload);
    //        return new(Created: true, index.ToString(CultureInfo.InvariantCulture), new JObjectAdapter((JObject)this.payload[index]));
    //    }
    //    else return new(Created: false, default, default);
    //}

    #endregion // NOT SUPPORTED YET // ICopyChildItem
}