namespace TreeStore.JsonFS;

/// <summary>
/// Implements an adapter between TreeStores ProviderNode and <see cref="Newtonsoft.Json.Linq.JObject"/>.
/// It implements <see cref="IServiceProvider"/> as a generic interface to provider the TreeStore node capabilities.
/// </summary>
public sealed class JObjectAdapter : JAdapterBase,
    // ItemCmdletProvider
    IGetItem, ISetItem, IClearItem,
    // ContainerCmdletProvider
    IGetChildItem, IRemoveChildItem, INewChildItem, IRenameChildItem, ICopyChildItem, ICopyChildItemRecursive,
    // NavigationCmdletProvider
    IMoveChildItem,
    // IPropertyCmdletProvider
    IClearItemProperty, ISetItemProperty, IRemoveItemProperty, ICopyItemProperty, IMoveItemProperty, INewItemProperty, IRenameItemProperty,
    // IContentProvider
    IGetItemContent, ISetChildItemContent, IClearItemContent
{
    internal readonly JObject payload;

    public JObjectAdapter(JObject payload) => this.payload = payload;

    #region Query properties holding value semantics

    private IEnumerable<JProperty> ValueProperties() => ValueProperties(this.payload);

    private JProperty? ValueProperty(string name)
    {
        var property = this.payload.Property(name);
        if (property is null)
            return null;

        return IsValueProperty(property) ? property : null;
    }

    #endregion Query properties holding value semantics

    #region Query properties holding child node semantics

    private IEnumerable<JProperty> ChildProperties() => ChildProperties(this.payload);

    private JProperty? ChildProperty(string name)
    {
        var property = this.payload.Property(name);
        if (property is null)
            return null;

        return IsChildProperty(property) ? property : null;
    }

    #endregion Query properties holding child node semantics

    #region IGetItem

    object? IGetItem.GetItemParameters() => new JsonFsGetItemParameters();

    PSObject IGetItem.GetItem(ICmdletProvider provider)
    {
        return provider.DynamicParameters is JsonFsGetItemParameters dynamicParameters
            ? dynamicParameters.AsHashtable.IsPresent
                ? this.ItemAsHashtable(provider)
                : this.ItemAsJsonFsItem(provider)
            : this.ItemAsJsonFsItem(provider);
    }

    private PSObject ItemAsJsonFsItem(ICmdletProvider provider)
    {
        var pso = PSObject.AsPSObject(new JsonFsItem(this.GetNameFromParent(this.payload), this.ValueProperties().Select(p => p.Name).ToArray()));

        foreach (var property in this.ValueProperties())
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

    private PSObject ItemAsHashtable(ICmdletProvider provider)
    {
        return PSObject.AsPSObject(this.ValueProperties()
            .Aggregate(new Hashtable(), (hs, property) =>
            {
                switch (property.Value)
                {
                    case JValue value:
                        hs[property.Name] = value.Value;
                        break;

                    case JArray array:
                        hs[property.Name] = array.Children().OfType<JValue>().Select(v => v.Value).ToArray();
                        break;
                }
                return hs;
            }));
    }

    #endregion IGetItem

    #region ISetItem

    void ISetItem.SetItem(ICmdletProvider provider, object? value)
    {
        switch (value)
        {
            case JObject jobject:
                this.SetItemFromJObject(provider, jobject);
                break;

            case string json:
                this.SetItemFromString(provider, json);
                break;

            case PSObject psobject:
                this.SetItemFromPSObject(provider, psobject);
                break;

            case Hashtable htable:
                this.SetItemFromHashtable(provider, htable);
                break;

            default:
                break;
        }
    }

    private void SetItemFromPSObject(ICmdletProvider provider, PSObject psobject)
    {
        using var handle = this.BeginModify(provider);

        this.RemoveValueProperies();

        foreach (var p in psobject.Properties)
            IfValueSemantic(p.Value, then: jt => this.payload[p.Name] = jt);
    }

    private void SetItemFromHashtable(ICmdletProvider provider, Hashtable hashTable)
    {
        using var handle = this.BeginModify(provider);

        this.RemoveValueProperies();

        foreach (var kv in hashTable)
            if (kv is DictionaryEntry de)
                IfValueSemantic(de.Value, then: jt => this.payload[de.Key] = jt);
    }

    private void SetItemFromJObject(ICmdletProvider provider, JObject jobject)
    {
        using var handle = this.BeginModify(provider);

        this.RemoveValueProperies();

        foreach (var p in jobject.Properties().Where(IsValueProperty))
            this.payload[p.Name] = p.Value;
    }

    private void SetItemFromString(ICmdletProvider provider, string json)
        => this.SetItemFromJObject(provider, JObject.Parse(json));

    private void RemoveValueProperies()
    {
        foreach (var vp in this.ValueProperties().ToArray())
            this.payload.Remove(vp.Name);
    }

    #endregion ISetItem

    #region IClearItem

    void IClearItem.ClearItem(ICmdletProvider provider)
    {
        using var handle = this.BeginModify(provider);

        foreach (var p in this.ValueProperties())
            p.Value = JValue.CreateNull();
    }

    #endregion IClearItem

    #region IGetChildItem

    bool IGetChildItem.HasChildItems(ICmdletProvider provider) => this.ChildProperties().Any();

    IEnumerable<ProviderNode> IGetChildItem.GetChildItems(ICmdletProvider provider)
    {
        foreach (var property in this.ChildProperties())
        {
            switch (property.Value)
            {
                case JObject jObject:
                    yield return new ContainerNode(provider, property.Name, new JObjectAdapter(jObject));
                    break;

                case JArray jArray:
                    yield return new ContainerNode(provider, property.Name, new JArrayAdapter(jArray));
                    break;
            }
        }
    }

    #endregion IGetChildItem

    #region IRemoveChildItem

    void IRemoveChildItem.RemoveChildItem(ICmdletProvider provider, string childName, bool recurse)
    {
        using var handle = this.BeginModify(provider);

        if (!this.payload.TryGetValue(childName, out var jtoken) || IsValueToken(jtoken))
            return;

        if (!recurse)
        {
            // remove empty children only

            if (jtoken is JArray jarray)
                if (jarray.Any())
                    return; // don't remove non empty array w/o recurse=true

            if (jtoken is JObject jobject)
                if (ChildProperties(jobject).Any())
                    return; // don't remove non empty object w/o recurse=true
        }

        this.payload.Remove(childName);
    }

    #endregion IRemoveChildItem

    #region INewChildItem

    NewChildItemResult INewChildItem.NewChildItem(ICmdletProvider provider, string? childName, string? itemTypeName, object? newItemValue)
    {
        ArgumentNullException.ThrowIfNull(childName, nameof(childName));

        if (this.payload.TryGetValue(childName, out var _))
            throw new InvalidOperationException($"A property(name='{childName}') already exists");

        return newItemValue switch
        {
            JObject jobject => this.NewChildItemFromJObject(provider, childName, itemTypeName, jobject),

            PSObject pso => this.NewChildItemFromPSObject(provider, childName, itemTypeName, pso),

            Hashtable htable => this.NewChildItemFromHashTable(provider, childName, itemTypeName, htable),

            string json => this.NewChildItemFromJson(provider, childName, itemTypeName, json),

            null => this.NewChildItemEmpty(provider, childName, itemTypeName),

            _ => new(Created: false, Name: childName, null)
        };
    }

    private NewChildItemResult NewChildItemFromHashTable(ICmdletProvider provider, string childName, string? itemTypeName, Hashtable newItemValue)
    {
        ArgumentNullException.ThrowIfNull(childName, nameof(childName));

        JObject childObject = new();
        JObjectAdapter jobjectAdapter = new(childObject);

        using var handle = this.BeginModify(provider);

        jobjectAdapter.SetItemFromHashtable(provider, newItemValue);

        this.payload[childName] = childObject;

        return new(Created: true, Name: childName, NodeServices: jobjectAdapter);
    }

    private NewChildItemResult NewChildItemFromJson(ICmdletProvider provider, string childName, string? itemTypeName, string json)
        => this.NewChildItemFromJObject(provider, childName, itemTypeName, JObject.Parse(json));

    private NewChildItemResult NewChildItemFromJObject(ICmdletProvider provider, string childName, string? itemTypeName, JObject jobject)
    {
        JObject newChildNode = new();
        JObjectAdapter newChildAdpater = new(newChildNode);

        using var handle = this.BeginModify(provider);

        newChildAdpater.SetItemFromJObject(provider, jobject);

        this.payload[childName] = newChildNode;

        return new(Created: true, Name: childName, NodeServices: newChildAdpater);
    }

    private NewChildItemResult NewChildItemEmpty(ICmdletProvider provider, string childName, string? itemTypeName)
        => this.NewChildItemFromJObject(provider, childName, itemTypeName, new JObject());

    private NewChildItemResult NewChildItemFromPSObject(ICmdletProvider provider, string? childName, string? itemTypeName, PSObject newItemValue)
    {
        ArgumentNullException.ThrowIfNull(childName, nameof(childName));

        JObject childObject = new();
        JObjectAdapter jobjectAdapter = new(childObject);

        using var handle = this.BeginModify(provider);

        jobjectAdapter.SetItemFromPSObject(provider, newItemValue);

        this.payload[childName] = childObject;

        return new(Created: true, Name: childName, NodeServices: jobjectAdapter);
    }

    #endregion INewChildItem

    #region IRenameChildItem

    void IRenameChildItem.RenameChildItem(ICmdletProvider provider, string childName, string newName)
    {
        using var handle = this.BeginModify(provider);

        var existingPropery = this.ChildProperty(childName);
        if (existingPropery is not null)
            if (this.payload.TryAdd(newName, existingPropery.Value))
                existingPropery.Remove();
    }

    #endregion IRenameChildItem

    #region ICopyChildItem

    CopyChildItemResult ICopyChildItem.CopyChildItem(ICmdletProvider provider, ProviderNode nodeToCopy, string[] destination)
    {
        return nodeToCopy.NodeServiceProvider switch
        {
            JObjectAdapter jobjectToCopy => this.CopyObjectToThisNode(provider, nodeToCopy, jobjectToCopy, destination),

            JArrayAdapter jarrayToCopy => this.CopyArrayToThisNode(provider, nodeToCopy, jarrayToCopy, destination),

            _ => new(false, default, default)
        };
    }

    private CopyChildItemResult CopyArrayToThisNode(ICmdletProvider provider, ProviderNode nodeToCopy, JArrayAdapter nodeToCopyAdapter, string[] destination)
    {
        return destination.Length switch
        {
            0 => this.CopyArrayUnderThisNode(provider, nodeToCopy.Name, nodeToCopyAdapter, CreateShallowArrayClone),
            1 => this.CopyArrayUnderThisNode(provider, destination[0], nodeToCopyAdapter, CreateShallowArrayClone),
            _ => this.CopyNodeUnderNewParent(provider, destination[0], destination[1..], nodeToCopy)
        };
    }

    private CopyChildItemResult CopyArrayUnderThisNode(ICmdletProvider provider, string name, JArrayAdapter nodeToCopyAdapter, Func<JArray, JArray> clone)
    {
        using var handle = this.BeginModify(provider);

        if (this.payload.TryAdd(name, clone(nodeToCopyAdapter.payload)))
            if (this.payload.TryGetValue(name, out var jtoken))
                return new(Created: true, Name: name, NodeServices: new JArrayAdapter((JArray)jtoken));

        return new(Created: false, Name: name, NodeServices: null);
    }

    private CopyChildItemResult CopyObjectToThisNode(ICmdletProvider provider, ProviderNode nodeToCopy, JObjectAdapter nodeToCopyAdapter, string[] destination)
    {
        return destination.Length switch
        {
            0 => this.CopyObjectUnderThisNode(provider, nodeToCopy.Name, nodeToCopyAdapter, CreateShallowObjectClone),
            1 => this.CopyObjectUnderThisNode(provider, destination[0], nodeToCopyAdapter, CreateShallowObjectClone),
            _ => this.CopyNodeUnderNewParent(provider, destination[0], destination[1..], nodeToCopy)
        };
    }

    private CopyChildItemResult CopyNodeUnderNewParent(ICmdletProvider provider, string parentName, string[] destination, ProviderNode nodeToCopy)
    {
        using var handle = this.BeginModify(provider);

        var newParent = new JObject();
        if (this.payload.TryAdd(parentName, newParent))
            return new JObjectAdapter(newParent).GetRequiredService<ICopyChildItem>().CopyChildItem(provider, nodeToCopy, destination);

        return new(Created: false, Name: parentName, NodeServices: null);
    }

    private CopyChildItemResult CopyObjectUnderThisNode(ICmdletProvider provider, string name, JObjectAdapter nodeToCopyAdapter, Func<JObject, JObject> clone)
    {
        using var handle = this.BeginModify(provider);

        if (this.payload.TryAdd(name, clone(nodeToCopyAdapter.payload)))
            if (this.payload.TryGetValue(name, out var jtoken))
                return new(Created: true, Name: name, NodeServices: new JObjectAdapter((JObject)jtoken));

        return new(Created: false, Name: name, NodeServices: null);
    }

    #endregion ICopyChildItem

    #region ICopyChildItemRecursive

    CopyChildItemResult ICopyChildItemRecursive.CopyChildItemRecursive(ICmdletProvider provider, ProviderNode nodeToCopy, string[] destination)
    {
        return nodeToCopy.NodeServiceProvider switch
        {
            JObjectAdapter jobjectToCopy => this.CopyObjectToThisNodeRecursively(provider, nodeToCopy, jobjectToCopy, destination),
            JArrayAdapter jarrayToCopy => this.CopyArrayToThisNodeRecursively(provider, nodeToCopy, jarrayToCopy, destination),
            _ => new(Created: false, Name: nodeToCopy.Name, NodeServices: null)
        };
    }

    private CopyChildItemResult CopyArrayToThisNodeRecursively(ICmdletProvider provider, ProviderNode nodeToCopy, JArrayAdapter nodeToCopyAdapter, string[] destination)
    {
        return destination.Length switch
        {
            // creating recursive copies follows the same logic as the non-recursive copy. It simple uses the deep
            // clone strategy to replicate the JSON

            0 => this.CopyArrayUnderThisNode(provider, nodeToCopy.Name, nodeToCopyAdapter, CreateDeepArrayClone),
            1 => this.CopyArrayUnderThisNode(provider, destination[0], nodeToCopyAdapter, CreateDeepArrayClone),
            _ => this.CopyNodeUnderNewParent(provider, destination[0], destination[1..], nodeToCopy)
        };
    }

    private CopyChildItemResult CopyObjectToThisNodeRecursively(ICmdletProvider provider, ProviderNode nodeToCopy, JObjectAdapter nodeToCopyAdapter, string[] destination)
    {
        using var handle = this.BeginModify(provider);

        return destination.Length switch
        {
            // creating recursive copies follows the same logic as the non-recursive copy. It simple uses the deep
            // clone strategy to replicate the JSON

            0 => this.CopyObjectUnderThisNode(provider, nodeToCopy.Name, nodeToCopyAdapter, CreateDeepObjectClone),
            1 => this.CopyObjectUnderThisNode(provider, destination[0], nodeToCopyAdapter, CreateDeepObjectClone),
            _ => this.CopyNodeUnderNewParentRecursive(provider, destination[0], destination[1..], nodeToCopy)
        };
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
        return nodeToMove.NodeServiceProvider switch
        {
            JObjectAdapter jobjectToMove => this.MoveObjectToThisNode(provider, parentOfNodeToMove, nodeToMove, jobjectToMove, destination),
            JArrayAdapter jarrayToMove => this.MoveArrayToThisNode(provider, parentOfNodeToMove, nodeToMove, jarrayToMove, destination),
            _ => new(Created: false, Name: nodeToMove.Name, NodeServices: null)
        };
    }

    private MoveChildItemResult MoveArrayToThisNode(ICmdletProvider provider, ContainerNode parentOfNodeToMove, ProviderNode nodeToMove, JArrayAdapter nodeToMoveAdapter, string[] destination)
    {
        return destination.Length switch
        {
            0 => this.MoveArrayUnderThisNode(provider, nodeToMove.Name, nodeToMoveAdapter, parentOfNodeToMove),
            1 => this.MoveArrayUnderThisNode(provider, destination[0], nodeToMoveAdapter, parentOfNodeToMove),
            _ => this.MoveNodeUnderNewParentNode(provider, destination[0], nodeToMove, parentOfNodeToMove, destination[1..])
        };
    }

    private MoveChildItemResult MoveObjectToThisNode(ICmdletProvider provider, ContainerNode parentOfNodeToMove, ProviderNode nodeToMove, JObjectAdapter nodeToMoveAdapter, string[] destination)
    {
        return destination.Length switch
        {
            0 => this.MoveObjectUnderThisNode(provider, nodeToMove.Name, nodeToMoveAdapter, parentOfNodeToMove),
            1 => this.MoveObjectUnderThisNode(provider, destination[0], nodeToMoveAdapter, parentOfNodeToMove),
            _ => this.MoveNodeUnderNewParentNode(provider, destination[0], nodeToMove, parentOfNodeToMove, destination[1..])
        };
    }

    private MoveChildItemResult MoveNodeUnderNewParentNode(ICmdletProvider provider, string parentName, ProviderNode nodeToMove, ContainerNode parentOfNodeToMove, string[] destination)
    {
        using var handle = this.BeginModify(provider);

        var parentJobject = new JObject();
        if (this.payload.TryAdd(parentName, parentJobject))
            return new JObjectAdapter(parentJobject).GetRequiredService<IMoveChildItem>().MoveChildItem(provider, parentOfNodeToMove, nodeToMove, destination);

        return new(Created: false, Name: parentName, NodeServices: null);
    }

    private MoveChildItemResult MoveObjectUnderThisNode(ICmdletProvider provider, string name, JObjectAdapter nodeToMoveAdapter, ContainerNode parentOfNodeToMove)
    {
        using var handle = this.BeginModify(provider);

        if (this.payload.TryAdd(name, CreateDeepObjectClone(nodeToMoveAdapter.payload)))
            nodeToMoveAdapter.payload.Parent!.Remove();

        return new(Created: true, Name: name, NodeServices: new JObjectAdapter((JObject)this.payload.Property(name)!.Value));
    }

    private MoveChildItemResult MoveArrayUnderThisNode(ICmdletProvider provider, string name, JArrayAdapter nodeToMoveAdapter, ContainerNode parentOfNodeToMove)
    {
        using var handle = this.BeginModify(provider);

        if (this.payload.TryAdd(name, CreateDeepArrayClone(nodeToMoveAdapter.payload)))
            nodeToMoveAdapter.payload.Parent!.Remove();

        return new(Created: true, Name: name, NodeServices: new JArrayAdapter((JArray)this.payload.Property(name)!.Value));
    }

    #endregion IMoveChildItem

    #region IClearItemProperty

    void IClearItemProperty.ClearItemProperty(ICmdletProvider provider, IEnumerable<string> propertyToClear)
    {
        using var handle = this.BeginModify(provider);

        foreach (var propertyName in propertyToClear)
        {
            var valueProperty = this.ValueProperty(propertyName);
            if (valueProperty is not null)
                this.payload[propertyName] = JValue.CreateNull();
        }
    }

    #endregion IClearItemProperty

    #region ISetItemProperty

    void ISetItemProperty.SetItemProperty(ICmdletProvider provider, PSObject properties)
    {
        foreach (var p in properties.Properties)
        {
            // check fo all value properties if the< alread exist.
            // if not throw an error if the creation isn't forced.
            IfValueSemantic(p.Value, then: _ =>
            {
                if (this.ChildProperty(p.Name) is not null)
                    return; // the propert is already a child property: ignore

                if (this.ValueProperty(p.Name) is null)
                    if (!provider.Force)
                        throw new InvalidOperationException($"Can't set property(name='{p.Name}'): it doesn't exist");
            });
        }

        using var handle = this.BeginModify(provider);

        foreach (var p in properties.Properties)
        {
            var valueProperty = this.ValueProperty(p.Name);
            if (valueProperty is not null)
            {
                IfValueSemantic(p.Value, then: jt => valueProperty.Value = jt);
            }
            else if (this.ChildProperty(p.Name) is not null)
            {
                // ignore child properties here.
            }
            else if (provider.Force.ToBool())
            {
                // force parameter is given create the property if possible
                // CreateItemProperty doesn't overwrite existing properties
                this.CreateItemProperty(p.Name, p.Value);
            }
        }
    }

    #endregion ISetItemProperty

    #region IRemoveItemProperty

    void IRemoveItemProperty.RemoveItemProperty(ICmdletProvider provider, string propertyName)
    {
        var valueProperty = this.ValueProperty(propertyName);
        if (valueProperty is null)
            return;

        using var handle = this.BeginModify(provider);

        this.payload.Remove(propertyName);
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
            IfValueSemantic(value, jt => this.payload[propertyName] = jt);
    }

    #endregion INewItemProperty

    #region IRenameItemProperty

    void IRenameItemProperty.RenameItemProperty(ICmdletProvider provider, string sourceProperty, string destinationProperty)
    {
        using var handle = this.BeginModify(provider);

        if (this.payload.TryGetValue(sourceProperty, out var value))
            if (!this.payload.TryGetValue(destinationProperty, out var _))
                IfValueSemantic(value, jt =>
                {
                    if (this.payload.Remove(sourceProperty))
                        this.payload[destinationProperty] = jt;
                });
    }

    #endregion IRenameItemProperty

    #region IGetItemContent

    /// <inheritdoc/>
    IContentReader? IGetItemContent.GetItemContentReader(ICmdletProvider provider) => new JObjectContentReader(this.payload);

    #endregion IGetItemContent

    #region ISetChildItemContent

    IContentWriter? ISetChildItemContent.GetChildItemContentWriter(ICmdletProvider provider, string childName)
    {
        if (string.IsNullOrEmpty(childName))
            return this.GetJObjectContentWriter(provider, this);

        if (!this.payload.TryGetValue(childName, out var jtoken))
        {
            using var handle = this.BeginModify(provider);

            var newChildItem = this.NewChildItemEmpty(provider, childName, itemTypeName: null);
            if (newChildItem.Created)
                return ((ISetChildItemContent)this).GetChildItemContentWriter(provider, childName);
            else return null;
        }

        if (IsValueToken(jtoken))
            throw new InvalidOperationException("Can't set content of value property");

        return jtoken switch
        {
            JObject jobject => this.GetJObjectContentWriter(provider, jobject),
            JArray jarray => this.GetJArrayContentWriter(provider, jarray),

            _ => throw new InvalidOperationException("")
        };
    }

    private IContentWriter? GetJObjectContentWriter(ICmdletProvider provider, JObjectAdapter jobjectAdapter)
    {
        using var handle = this.BeginModify(provider);

        return new JObjectContentWriter(provider, jobjectAdapter);
    }

    private IContentWriter? GetJObjectContentWriter(ICmdletProvider provider, JObject jobject)
        => this.GetJObjectContentWriter(provider, new JObjectAdapter(jobject));

    private IContentWriter? GetJArrayContentWriter(ICmdletProvider provider, JArray jarray)
    {
        using var handle = this.BeginModify(provider);

        return new JArrayContentWriter(provider, new JArrayAdapter(jarray));
    }

    #endregion ISetChildItemContent

    #region IClearItemContent

    void IClearItemContent.ClearItemContent(ICmdletProvider provider)
    {
        using var handle = this.BeginModify(provider);

        this.payload.RemoveAll();
    }

    #endregion IClearItemContent
}