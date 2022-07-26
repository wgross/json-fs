namespace TreeStore.JsonFS;

/// <summary>
/// Implements an adapter between TreeStores ProviderNode and <see cref="Newtonsoft.Json.Linq.JObject"/>.
/// It implements <see cref="IServiceProvider"/> as a generic interface to provider the TreeStore node capabilities.
/// </summary>
public sealed class JObjectAdapter : IServiceProvider,
    // ItemCmdletProvider
    IGetItem, ISetItem, IClearItem,
    // ContainerCmdletProvider
    IGetChildItem, IRemoveChildItem, INewChildItem, IRenameChildItem, ICopyChildItem, ICopyChildItemRecursive,
    // NavigationCmdletProvider
    IMoveChildItem,
    // IPropertyCmdletProvider
    IClearItemProperty, ISetItemProperty, IRemoveItemProperty, ICopyItemProperty, IMoveItemProperty, INewItemProperty, IRenameItemProperty
{
    private readonly JObject payload;

    public JObjectAdapter(JObject payload) => this.payload = payload;

    private IEnumerable<JProperty> ValueProperties(JObject jobject) => jobject.Properties().Where(p => p.Value.Type != JTokenType.Object);

    #region Write the JSON file after modification

    private IDisposable BeginModify(ICmdletProvider cmdletProvider)
    {
        if (cmdletProvider is IJsonFsRootNodeModification jsonFsRootNodeModification)
            return jsonFsRootNodeModification.BeginModify();

        throw new InvalidOperationException($"Provider(type='{cmdletProvider.GetType()}' doesn't support modification");
    }

    #endregion Write the JSON file after modification

    #region IServiceProvider

    public object? GetService(Type serviceType)
    {
        if (this.GetType().IsAssignableTo(serviceType))
            return this;
        else return null;
    }

    #endregion IServiceProvider

    #region IGetItem

    PSObject IGetItem.GetItem(ICmdletProvider provider)
    {
        var pso = new PSObject();
        foreach (var property in this.payload.Children().OfType<JProperty>())
        {
            switch (property.Value)
            {
                case JValue value:
                    pso.Properties.Add(new PSNoteProperty(property.Name, value.Value));
                    break;

                case JArray array:
                    pso.Properties.Add(new PSNoteProperty(property.Name, array.Children().OfType<JValue>().Select(v => v.Value).ToArray()));
                    break;
            }
        }
        return pso;
    }

    #endregion IGetItem

    #region ISetItem

    void ISetItem.SetItem(ICmdletProvider provider, object? value)
    {
        using var handle = this.BeginModify(provider);

        if (value is JObject jobject)
        {
            ((IList)this.payload).Clear();
            foreach (var p in jobject.Properties())
                this.payload[p.Name] = p.Value;
        }
        else if (value is string json)
        {
            var jobjectFromString = JObject.Parse(json);

            ((IList)this.payload).Clear();
            foreach (var p in jobjectFromString.Properties())
                this.payload[p.Name] = p.Value;
        }
    }

    #endregion ISetItem

    #region IClearItem

    void IClearItem.ClearItem(ICmdletProvider provider)
    {
        using var handle = this.BeginModify(provider);

        foreach (var p in this.ValueProperties(this.payload))
            p.Value = JValue.CreateNull();
    }

    #endregion IClearItem

    #region IGetChildItem

    bool IGetChildItem.HasChildItems(ICmdletProvider provider)
    {
        return this.payload.Children().OfType<JProperty>().Any();
    }

    IEnumerable<ProviderNode> IGetChildItem.GetChildItems(ICmdletProvider provider)
    {
        foreach (var property in this.payload.Children().OfType<JProperty>())
            if (property.Value is JObject jObject)
                yield return new ContainerNode(provider, property.Name, new JObjectAdapter(jObject));
    }

    #endregion IGetChildItem

    #region IRemoveChildItem

    void IRemoveChildItem.RemoveChildItem(ICmdletProvider provider, string childName, bool recurse)
    {
        using var handle = this.BeginModify(provider);

        if (this.payload.TryGetValue(childName, out var jtoken))
            if (jtoken is JObject)
                this.payload.Remove(childName);
    }

    #endregion IRemoveChildItem

    #region INewChildItem

    NewChildItemResult INewChildItem.NewChildItem(ICmdletProvider provider, string childName, string? itemTypeName, object? newItemValue)
    {
        if (this.payload.TryGetValue(childName, out var _))
            throw new InvalidOperationException($"A property(name='{childName}') already exists");

        using var handle = this.BeginModify(provider);

        if (newItemValue is null)
        {
            var emptyObject = new JObject();

            this.payload[childName] = emptyObject;

            return new(Created: true, Name: childName, NodeServices: new JObjectAdapter(emptyObject));
        }
        if (newItemValue is JObject jobject)
        {
            this.payload[childName] = jobject;

            return new(Created: true, Name: childName, NodeServices: new JObjectAdapter(jobject));
        }
        else if (newItemValue is string json)
        {
            var parsedObject = JObject.Parse(json);
            this.payload[childName] = parsedObject;

            return new(Created: true, Name: childName, NodeServices: new JObjectAdapter(parsedObject));
        }

        return new(Created: false, Name: childName, null);
    }

    #endregion INewChildItem

    #region IRenameChildItem

    void IRenameChildItem.RenameChildItem(ICmdletProvider provider, string childName, string newName)
    {
        using var handle = this.BeginModify(provider);

        var existingPropery = this.payload.Property(childName);
        if (existingPropery is not null)
            if (this.payload.TryAdd(newName, existingPropery.Value))
                existingPropery.Remove();
    }

    #endregion IRenameChildItem

    #region ICopyChildItem

    CopyChildItemResult ICopyChildItem.CopyChildItem(ICmdletProvider provider, ProviderNode nodeToCopy, string[] destination)
    {
        using var handle = this.BeginModify(provider);

        if (nodeToCopy.NodeServiceProvider is JObjectAdapter underlying)
        {
            return destination.Length switch
            {
                0 => this.CopyNodeUnderThisNode(nodeToCopy.Name, underlying, this.ShallowClone),
                1 => this.CopyNodeUnderThisNode(destination[0], underlying, this.ShallowClone),
                _ => this.CopyNodeUnderNewParent(provider, destination[0], destination[1..], nodeToCopy)
            };
        }

        return null;
    }

    private CopyChildItemResult CopyNodeUnderNewParent(ICmdletProvider provider, string parentName, string[] destination, ProviderNode nodeToCopy)
    {
        var newParent = new JObject();
        if (this.payload.TryAdd(parentName, newParent))
            return new JObjectAdapter(newParent).GetRequiredService<ICopyChildItem>().CopyChildItem(provider, nodeToCopy, destination);

        return new(Created: false, Name: parentName, NodeServices: null);
    }

    private CopyChildItemResult CopyNodeUnderThisNode(string name, JObjectAdapter adapter, Func<JObject, JObject> clone)
    {
        if (this.payload.TryAdd(name, clone(adapter.payload)))
            if (this.payload.TryGetValue(name, out var jtoken))
                return new(Created: true, Name: name, NodeServices: new JObjectAdapter((JObject)jtoken));

        return new(Created: false, Name: name, NodeServices: null);
    }

    /// <summary>
    /// A shallow clone contains all properties except the objects.
    /// </summary>
    private JObject ShallowClone(JObject jobject) => new JObject(this.ValueProperties(jobject));

    #endregion ICopyChildItem

    #region ICopyChildItemRecursive

    CopyChildItemResult ICopyChildItemRecursive.CopyChildItemRecursive(ICmdletProvider provider, ProviderNode nodeToCopy, string[] destination)
    {
        using var handle = this.BeginModify(provider);

        if (nodeToCopy.NodeServiceProvider is JObjectAdapter underlying)
        {
            return destination.Length switch
            {
                0 => this.CopyNodeUnderThisNode(nodeToCopy.Name, underlying, jo => (JObject)jo.DeepClone()),
                1 => this.CopyNodeUnderThisNode(destination[0], underlying, jo => (JObject)jo.DeepClone()),
                _ => this.CopyNodeUnderNewParentRecursive(provider, destination[0], destination[1..], nodeToCopy)
            };
        }

        return new(Created: false, Name: nodeToCopy.Name, NodeServices: null);
    }

    private CopyChildItemResult CopyNodeUnderNewParentRecursive(ICmdletProvider provider, string parentName, string[] destination, ProviderNode nodeToCopy)
    {
        var newParent = new JObject();
        if (this.payload.TryAdd(parentName, newParent))
            return new JObjectAdapter(newParent).GetRequiredService<ICopyChildItemRecursive>().CopyChildItemRecursive(provider, nodeToCopy, destination);

        return new(Created: false, Name: parentName, NodeServices: null);
    }

    #endregion ICopyChildItemRecursive

    #region IMoveChildItem

    MoveChildItemResult IMoveChildItem.MoveChildItem(ICmdletProvider provider, ContainerNode parentOfNodeToMove, ProviderNode nodeToMove, string[] destination)
    {
        using var handle = this.BeginModify(provider);

        if (nodeToMove.NodeServiceProvider is JObjectAdapter underlying)
        {
            return destination.Length switch
            {
                0 => this.MoveNodeUnderThisNode(nodeToMove.Name, underlying, parentOfNodeToMove),
                1 => this.MoveNodeUnderThisNode(destination[0], underlying, parentOfNodeToMove),
                _ => this.MoveNodeUnderNewParentNode(provider, destination[0], destination[1..], nodeToMove, parentOfNodeToMove)
            };
        }
        return new(Created: false, Name: nodeToMove.Name, NodeServices: null);
    }

    private MoveChildItemResult MoveNodeUnderNewParentNode(ICmdletProvider provider, string parentName, string[] destination, ProviderNode nodeToMove, ContainerNode parentOfNodeToMove)
    {
        var parentJobject = new JObject();
        if (this.payload.TryAdd(parentName, parentJobject))
            return new JObjectAdapter(parentJobject).GetRequiredService<IMoveChildItem>().MoveChildItem(provider, parentOfNodeToMove, nodeToMove, destination);

        return new(Created: false, Name: parentName, NodeServices: null);
    }

    private MoveChildItemResult MoveNodeUnderThisNode(string name, JObjectAdapter underlying, ContainerNode parentOfNodeToMove)
    {
        if (this.payload.TryAdd(name, underlying.payload.DeepClone()))
            underlying.payload.Parent!.Remove();

        return new(Created: true, Name: name, NodeServices: new JObjectAdapter((JObject)this.payload.Property(name)!.Value));
    }

    #endregion IMoveChildItem

    #region IClearItemProperty

    void IClearItemProperty.ClearItemProperty(ICmdletProvider provider, IEnumerable<string> propertyToClear)
    {
        using var handle = this.BeginModify(provider);

        foreach (var propertyName in propertyToClear)
            if (this.payload.TryGetValue(propertyName, out var value))
                if (value.Type != JTokenType.Object)
                    this.payload[propertyName] = JValue.CreateNull();
    }

    #endregion IClearItemProperty

    #region ISetItemPorperty

    void ISetItemProperty.SetItemProperty(ICmdletProvider provider, PSObject properties)
    {
        using var handle = this.BeginModify(provider);

        foreach (var p in properties.Properties)
        {
            if (this.payload.TryGetValue(p.Name, out var value))
            {
                if (value.Type != JTokenType.Object)
                    this.IfValueSemantic(p.Value, jt => this.payload[p.Name] = jt);
            }
            else if (provider.Force.ToBool())
            {
                // force parameter is given create the property if possible
                this.CreateItemProperty(p.Name, p.Value);
            }
        }
    }

    private void IfValueSemantic(object? value, Action<JToken> then)
    {
        if (value is null)
            then(JValue.CreateNull());
        else if (value is string str)
            then(new JValue(value));
        else if (value.GetType().IsArray)
            then(new JArray(value));
        else if (value.GetType().IsClass)
            return;
        else
            then(new JValue(value));
    }

    private void IfValueSemantic(JToken token, Action<JToken> then)
    {
        switch (token.Type)
        {
            case JTokenType.Null:
                this.IfValueSemantic(null, then);
                return;

            case JTokenType.Object:
                return;

            case JTokenType.Array:
            case JTokenType.String:
            default:
                this.IfValueSemantic(((JValue)token).Value, then);
                return;
        }
    }

    #endregion ISetItemPorperty

    #region IRemoveItemProperty

    void IRemoveItemProperty.RemoveItemProperty(ICmdletProvider provider, string propertyName)
    {
        using var handle = this.BeginModify(provider);

        if (this.payload.TryGetValue(propertyName, out var value))
            if (value.Type != JTokenType.Object)
                value.Parent!.Remove();
    }

    #endregion IRemoveItemProperty

    #region ICopyItemProperty

    void ICopyItemProperty.CopyItemProperty(ICmdletProvider provider, ProviderNode sourceNode, string sourceProperty, string destinationProperty)
    {
        using var handle = this.BeginModify(provider);

        if (sourceNode.NodeServiceProvider is JObjectAdapter sourceJobject)
        {
            if (sourceJobject.payload.TryGetValue(sourceProperty, out var value))
            {
                if (value.Type != JTokenType.Object)
                {
                    this.payload[destinationProperty] = value;
                }
            }
        }
    }

    #endregion ICopyItemProperty

    #region IMoveItemProperty

    void IMoveItemProperty.MoveItemProperty(ICmdletProvider provider, ProviderNode sourceNode, string sourceProperty, string destinationProperty)
    {
        using var handle = this.BeginModify(provider);

        if (sourceNode.NodeServiceProvider is JObjectAdapter sourceJobject)
        {
            if (sourceJobject.payload.TryGetValue(sourceProperty, out var value))
            {
                if (value.Type != JTokenType.Object)
                {
                    this.payload[destinationProperty] = value;

                    sourceJobject.payload.Property(sourceProperty)!.Remove();
                }
            }
        }
    }

    #endregion IMoveItemProperty

    #region INewItemProperty

    void INewItemProperty.NewItemProperty(ICmdletProvider provider, string propertyName, string? propertyTypeName, object? value)
    {
        using var handle = this.BeginModify(provider);

        this.CreateItemProperty(propertyName, value);
    }

    private void CreateItemProperty(string propertyName, object? value)
    {
        if (!this.payload.TryGetValue(propertyName, out var _))
            this.IfValueSemantic(value, jt => this.payload[propertyName] = jt);
    }

    #endregion INewItemProperty

    #region IRenameItemProperty

    void IRenameItemProperty.RenameItemProperty(ICmdletProvider provider, string sourceProperty, string destinationProperty)
    {
        using var handle = this.BeginModify(provider);

        if (this.payload.TryGetValue(sourceProperty, out var value))
            if (!this.payload.TryGetValue(destinationProperty, out var _))
                this.IfValueSemantic(value, jt =>
                {
                    if (this.payload.Remove(sourceProperty))
                        this.payload[destinationProperty] = jt;
                });
    }

    #endregion IRenameItemProperty
}