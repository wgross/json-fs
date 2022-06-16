using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using System.Management.Automation;
using TreeStore.Core.Capabilities;
using TreeStore.Core.Nodes;

namespace TreeStore.JsonFS;

public sealed class JObjectAdapter : IServiceProvider,
    // ItemCmdletProvider
    IGetItem,
    // ContainerCmdletProvider
    IGetChildItem, IRemoveChildItem, INewChildItem, IRenameChildItem, ICopyChildItem
{
    private readonly JObject payload;

    public JObjectAdapter(JObject payload) => this.payload = payload;

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
                0 => this.CopyNodeUnderThisNode(nodeToCopy.Name, underlying),
                1 => this.CopyNodeUnderThisNode(destination[0], underlying),
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

    private ProviderNode? CopyNodeUnderThisNode(string name, JObjectAdapter adapter)
    {
        if (this.payload.TryAdd(name, adapter.payload.DeepClone()))
            if (this.payload.TryGetValue(name, out var jtoken))
                return new ContainerNode(name, new JObjectAdapter((JObject)jtoken));

        return null;
    }

    #endregion ICopyChildItem
}