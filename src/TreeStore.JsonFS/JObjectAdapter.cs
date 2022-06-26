using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Management.Automation;
using TreeStore.Core.Capabilities;
using TreeStore.Core.Nodes;

namespace TreeStore.JsonFS;

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

    #region IServiceProvider

    public object? GetService(Type serviceType)
    {
        if (this.GetType().IsAssignableTo(serviceType))
            return this;
        else return null;
    }

    #endregion IServiceProvider

    #region IGetItem

    PSObject? IGetItem.GetItem()
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

    void ISetItem.SetItem(object? value)
    {
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

    void IClearItem.ClearItem()
    {
        foreach (var p in this.ValueProperties(this.payload))
            p.Value = JValue.CreateNull();
    }

    #endregion IClearItem

    #region IGetChildItem

    bool IGetChildItem.HasChildItems()
    {
        return this.payload.Children().OfType<JProperty>().Any();
    }

    IEnumerable<ProviderNode> IGetChildItem.GetChildItems()
    {
        foreach (var property in this.payload.Children().OfType<JProperty>())
            if (property.Value is JObject jObject)
                yield return new ContainerNode(property.Name, new JObjectAdapter(jObject));
    }

    #endregion IGetChildItem

    #region IRemoveChildItem

    void IRemoveChildItem.RemoveChildItem(string childName, bool recurse)
    {
        if (this.payload.TryGetValue(childName, out var jtoken))
            if (jtoken is JObject)
                this.payload.Remove(childName);
    }

    #endregion IRemoveChildItem

    #region INewChildItem

    ProviderNode? INewChildItem.NewChildItem(string childName, string? itemTypeName, object? newItemValue)
    {
        if (this.payload.TryGetValue(childName, out var _))
            throw new InvalidOperationException($"A property(name='{childName}') already exists");

        if (newItemValue is null)
        {
            var emptyObject = new JObject();

            this.payload[childName] = emptyObject;

            return new ContainerNode(childName, new JObjectAdapter(emptyObject));
        }
        if (newItemValue is JObject jobject)
        {
            this.payload[childName] = jobject;

            return new ContainerNode(childName, new JObjectAdapter(jobject));
        }
        else if (newItemValue is string json)
        {
            var parsedObject = JObject.Parse(json);
            this.payload[childName] = parsedObject;

            return new ContainerNode(childName, new JObjectAdapter(parsedObject));
        }

        return null;
    }

    #endregion INewChildItem

    #region IRenameChildItem

    void IRenameChildItem.RenameChildItem(string childName, string newName)
    {
        var existingPropery = this.payload.Property(childName);
        if (existingPropery is not null)
            if (this.payload.TryAdd(newName, existingPropery.Value))
                existingPropery.Remove();
    }

    #endregion IRenameChildItem

    #region ICopyChildItem

    ProviderNode? ICopyChildItem.CopyChildItem(ProviderNode nodeToCopy, string[] destination)
    {
        if (nodeToCopy.Underlying is JObjectAdapter underlying)
        {
            return destination.Length switch
            {
                0 => this.CopyNodeUnderThisNode(nodeToCopy.Name, underlying, this.ShallowClone),
                1 => this.CopyNodeUnderThisNode(destination[0], underlying, this.ShallowClone),
                _ => this.CopyNodeUnderNewParent(destination[0], destination[1..], nodeToCopy)
            };
        }

        return null;
    }

    private ProviderNode? CopyNodeUnderNewParent(string parentName, string[] destination, ProviderNode nodeToCopy)
    {
        var newParent = new JObject();
        if (this.payload.TryAdd(parentName, newParent))
            return new JObjectAdapter(newParent).GetRequiredService<ICopyChildItem>().CopyChildItem(nodeToCopy, destination);

        return null;
    }

    private ProviderNode? CopyNodeUnderThisNode(string name, JObjectAdapter adapter, Func<JObject, JObject> clone)
    {
        if (this.payload.TryAdd(name, clone(adapter.payload)))
            if (this.payload.TryGetValue(name, out var jtoken))
                return new ContainerNode(name, new JObjectAdapter((JObject)jtoken));

        return null;
    }

    /// <summary>
    /// A shallow clone contains all properties except the objects.
    /// </summary>
    private JObject ShallowClone(JObject jobject) => new JObject(this.ValueProperties(jobject));

    #endregion ICopyChildItem

    #region ICopyChildItemRecursive

    ProviderNode? ICopyChildItemRecursive.CopyChildItemRecursive(ProviderNode nodeToCopy, string[] destination)
    {
        if (nodeToCopy.Underlying is JObjectAdapter underlying)
        {
            return destination.Length switch
            {
                0 => this.CopyNodeUnderThisNode(nodeToCopy.Name, underlying, jo => (JObject)jo.DeepClone()),
                1 => this.CopyNodeUnderThisNode(destination[0], underlying, jo => (JObject)jo.DeepClone()),
                _ => this.CopyNodeUnderNewParentRecursive(destination[0], destination[1..], nodeToCopy)
            };
        }

        return null;
    }

    private ProviderNode? CopyNodeUnderNewParentRecursive(string parentName, string[] destination, ProviderNode nodeToCopy)
    {
        var newParent = new JObject();
        if (this.payload.TryAdd(parentName, newParent))
            return new JObjectAdapter(newParent).GetRequiredService<ICopyChildItemRecursive>().CopyChildItemRecursive(nodeToCopy, destination);

        return null;
    }

    #endregion ICopyChildItemRecursive

    #region IMoveChildItem

    ProviderNode? IMoveChildItem.MoveChildItem(ContainerNode parentOfNodeToMove, ProviderNode nodeToMove, string[] destination)
    {
        if (nodeToMove.Underlying is JObjectAdapter underlying)
        {
            return destination.Length switch
            {
                0 => this.MoveNodeUnderThisNode(nodeToMove.Name, underlying, parentOfNodeToMove),
                1 => this.MoveNodeUnderThisNode(destination[0], underlying, parentOfNodeToMove),
                _ => this.MoveNodeUnderNewParentNode(destination[0], destination[1..], nodeToMove, parentOfNodeToMove)
            };
        }
        return null;
    }

    private ProviderNode? MoveNodeUnderNewParentNode(string parentName, string[] destination, ProviderNode nodeToMove, ContainerNode parentOfNodeToMove)
    {
        var parentJobject = new JObject();
        if (this.payload.TryAdd(parentName, parentJobject))
            return new JObjectAdapter(parentJobject).GetRequiredService<IMoveChildItem>().MoveChildItem(parentOfNodeToMove, nodeToMove, destination);

        return null;
    }

    private ProviderNode? MoveNodeUnderThisNode(string name, JObjectAdapter underlying, ContainerNode parentOfNodeToMove)
    {
        if (this.payload.TryAdd(name, underlying.payload.DeepClone()))
            underlying.payload.Parent!.Remove();

        return new ContainerNode(name, new JObjectAdapter((JObject)this.payload.Property(name)!.Value));
    }

    #endregion IMoveChildItem

    #region IClearItemProperty

    void IClearItemProperty.ClearItemProperty(IEnumerable<string> propertyToClear)
    {
        foreach (var propertyName in propertyToClear)
            if (this.payload.TryGetValue(propertyName, out var value))
                if (value.Type != JTokenType.Object)
                    this.payload[propertyName] = JValue.CreateNull();
    }

    #endregion IClearItemProperty

    #region ISetItemPorperty

    void ISetItemProperty.SetItemProperty(PSObject properties)
    {
        foreach (var p in properties.Properties)
            if (this.payload.TryGetValue(p.Name, out var value))
                if (value.Type != JTokenType.Object)
                    this.IfValueSemantic(p.Value, jt => this.payload[p.Name] = jt);
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

    void IRemoveItemProperty.RemoveItemProperty(string propertyName)
    {
        if (this.payload.TryGetValue(propertyName, out var value))
            if (value.Type != JTokenType.Object)
                value.Parent!.Remove();
    }

    #endregion IRemoveItemProperty

    #region ICopyItemProperty

    void ICopyItemProperty.CopyItemProperty(ProviderNode sourceNode, string sourceProperty, string destinationProperty)
    {
        if (sourceNode.Underlying is JObjectAdapter sourceJobject)
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

    void IMoveItemProperty.MoveItemProperty(ProviderNode sourceNode, string sourceProperty, string destinationProperty)
    {
        if (sourceNode.Underlying is JObjectAdapter sourceJobject)
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

    void INewItemProperty.NewItemProperty(string propertyName, string? propertyTypeName, object? value)
    {
        if (!this.payload.TryGetValue(propertyName, out var _))
            this.IfValueSemantic(value, jt => this.payload[propertyName] = jt);
    }

    #endregion INewItemProperty

    #region IRenameItemProperty

    void IRenameItemProperty.RenameItemProperty(string sourceProperty, string destinationProperty)
    {
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