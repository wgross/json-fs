using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Management.Automation.Provider;
using TreeStore.Core.Capabilities;
using TreeStore.Core.Nodes;

namespace TreeStore.JsonFS.Test;

public class JObjectAdapterTest
{
    private readonly MockRepository mocks = new MockRepository(MockBehavior.Strict);
    private readonly Mock<CmdletProvider> providerMock;
    private readonly Mock<IDisposable> disopsableMock;

    public JObjectAdapterTest()
    {
        this.providerMock = this.mocks.Create<CmdletProvider>();
        this.disopsableMock = this.mocks.Create<IDisposable>();
    }

    public void Dispose() => this.mocks.VerifyAll();

    private void ArrangeBeginModification()
    {
        this.providerMock
          .As<IJsonFsRootNodeModification>()
          .Setup(p => p.BeginModify())
          .Returns(this.disopsableMock.Object);

        this.disopsableMock
            .Setup(d => d.Dispose());
    }

    #region IGetItem

    [Fact]
    public void GetItem_creates_PSObject_of_JObject()
    {
        // ARRANGE
        var node = new JObjectAdapter(new JObject()
        {
            ["value"] = new JValue(1),
            ["array"] = new JArray(new JValue(1), new JValue(2)),
            ["object"] = new JObject(),
        });

        // ACT
        var result = ((IGetItem)node).GetItem(provider: this.providerMock.Object);

        // ASSERT
        // value properties are note properties in the PSObject
        Assert.Equal(1, result!.Property<long>("value"));
        Assert.Equal(new object[] { 1L, 2L }, result!.Property<object[]>("array"));

        // object properties are skipped
        Assert.Null(result!.Properties.FirstOrDefault(p => p.Name == "object"));
    }

    #endregion IGetItem

    #region ISetItem

    [Fact]
    public void SetItem_replaces_from_JObject()
    {
        // ARRANGE
        this.ArrangeBeginModification();

        var node = new JObjectAdapter(new JObject
        {
            ["value"] = new JValue("text"),
            ["value2"] = new JValue(2)
        });

        var newData = new JObject()
        {
            ["object"] = new JObject(),
            ["value"] = new JValue(1),
            ["array"] = new JArray(1, 2)
        };

        // ACT
        // override node with new data
        node.GetRequiredService<ISetItem>().SetItem(this.providerMock.Object, newData);

        // ASSERT
        // array was added, value overrides the old value
        var psobject = node.GetRequiredService<IGetItem>().GetItem(this.providerMock.Object);

        Assert.Equal(1, psobject!.Property<long>("value"));
        Assert.Equal(new object[] { 1, 2 }, psobject!.Property<object[]>("array"));

        // value2 was removed
        Assert.Null(psobject!.Properties.FirstOrDefault(p => p.Name == "value2"));

        // object was added
        Assert.Equal("object", node.GetRequiredService<IGetChildItem>().GetChildItems(this.providerMock.Object).Single().Name);
    }

    [Fact]
    public void SetItem_replaces_from_string()
    {
        // ARRANGE
        this.ArrangeBeginModification();

        var node = new JObjectAdapter(new JObject
        {
            ["value"] = new JValue("text"),
            ["value2"] = new JValue(2)
        });

        var newData = new JObject()
        {
            ["object"] = new JObject(),
            ["value"] = new JValue(1),
            ["array"] = new JArray(1, 2)
        };

        // ACT
        // override node with new data
        node.GetRequiredService<ISetItem>().SetItem(this.providerMock.Object, newData.ToString());

        // ASSERT
        // array was added, value overrides the old value
        var psobject = node.GetRequiredService<IGetItem>().GetItem(this.providerMock.Object);

        Assert.Equal(1, psobject!.Property<long>("value"));
        Assert.Equal(new object[] { (long)1, (long)2 }, psobject!.Property<object[]>("array"));

        // value2 was removed
        Assert.Null(psobject!.Properties.FirstOrDefault(p => p.Name == "value2"));

        // object was added
        Assert.Equal("object", node.GetRequiredService<IGetChildItem>().GetChildItems(provider: this.providerMock.Object).Single().Name);
    }

    //[Fact]
    //public void SetItem_rejects_unkown_type()
    //{
    //    // ARRANGE

    //    var node = ArrangeContainerAdapter(new JObject
    //    {
    //        ["child"] = new JObject()
    //    });

    //    // ACT
    //    var result = Assert.Throws<InvalidOperationException>(() => node.GetRequiredService<ISetItem>().SetItem(new object()));

    //    // ASSERT
    //    Assert.Equal("Data of type 'System.Object' can't be assigned", result.Message);
    //}

    //[Fact]
    //public void SetItem_rejects_null()
    //{
    //    // ARRANGE
    //    var node = ArrangeContainerAdapter(new JObject
    //    {
    //        ["child"] = new JObject()
    //    });

    //    // ACT
    //    var result = Assert.Throws<ArgumentNullException>(() => node.GetRequiredService<ISetItem>().SetItem(null));

    //    // ASSERT
    //    Assert.Equal("value", result.ParamName);
    //}

    #endregion ISetItem

    #region IClearItem

    [Fact]
    public void ClearItem_nulls_value_properties()
    {
        // ARRANGE
        this.ArrangeBeginModification();

        var jnode = new JObject()
        {
            ["value"] = new JValue(1),
            ["array"] = new JArray(new JValue(1), new JValue(2)),
            ["object"] = new JObject
            {
                ["value"] = new JValue(2),
                ["array"] = new JArray(new JValue(3), new JValue(4))
            }
        };

        var node = new JObjectAdapter(jnode);

        // ACT
        node.GetRequiredService<IClearItem>().ClearItem(this.providerMock.Object);

        // ASSERT
        // property and array are null
        Assert.True(jnode.TryGetValue("value", out var property));
        Assert.Equal(JTokenType.Null, property!.Type);
        Assert.True(jnode.TryGetValue("array", out var array));
        Assert.Equal(JTokenType.Null, array!.Type);

        // object is still there
        Assert.True(jnode.TryGetValue("object", out var jobject));
        Assert.NotNull(jobject);

        // item properties are there but 'null'
        var psobject = node.GetRequiredService<IGetItem>().GetItem(this.providerMock.Object);

        Assert.Null(psobject!.Property<object>("value"));
        Assert.Null(psobject!.Property<object>("array"));
    }

    #endregion IClearItem

    #region IGetChildItem

    [Fact]
    public void HasChildItems_is_true_for_JObject()
    {
        // ARRANGE
        var node = new JObjectAdapter(new JObject()
        {
            ["value"] = new JValue(1),
            ["array"] = new JArray(new JValue(1), new JValue(2)),
            ["object"] = new JObject
            {
                ["value"] = new JValue(2),
                ["array"] = new JArray(new JValue(3), new JValue(4))
            }
        });

        // ACT
        var result = ((IGetChildItem)node).HasChildItems(this.providerMock.Object);

        // ASSERT
        // the node has child nodes.
        Assert.True(result);
    }

    [Fact]
    public void GetChildItem_creates_PSObject_from_JObject()
    {
        // ARRANGE
        var node = new JObjectAdapter(new JObject()
        {
            ["value"] = new JValue(1),
            ["array"] = new JArray(new JValue(1), new JValue(2)),
            ["object"] = new JObject
            {
                ["value"] = new JValue(2),
                ["array"] = new JArray(new JValue(3), new JValue(4))
            }
        });

        // ACT
        var result = ((IGetChildItem)node).GetChildItems(this.providerMock.Object).ToArray();

        // ASSERT
        Assert.Single(result);

        var pso = result.Single().GetItem(this.providerMock.Object);

        // the child properties are note properties of the PSObject
        Assert.Equal(2, pso.Property<long>("value"));
        Assert.Equal(new object[] { 3L, 4L }, pso.Property<object[]>("array"));
    }

    [Fact]
    public void GetChildItems_from_empty_returns_empty()
    {
        // ARRANGE
        var node = new JObjectAdapter(new JObject
        {
            ["value"] = new JValue(1),
            ["array"] = new JArray(new JValue(1), new JValue(2)),
        });

        // ACT
        var result = ((IGetChildItem)node).GetChildItems(this.providerMock.Object).ToArray();

        // ASSERT
        // an empty JObject has no properties
        Assert.Empty(result);
    }

    #endregion IGetChildItem

    #region IRemoveChildItem

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void RemoveChildItem_removes_JObject_item(bool recurse)
    {
        // ARRANGE
        this.ArrangeBeginModification();

        var underlying = new JObject
        {
            ["container1"] = new JObject(),
            ["property"] = new JValue("property"),
        };

        var node = new JObjectAdapter(underlying);

        // ACT
        ((IRemoveChildItem)node).RemoveChildItem(provider: this.providerMock.Object, "container1", recurse);

        // ASSERT
        // the child node is gone
        Assert.False(underlying.TryGetValue("container1", out var _));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void RemoveChildItem_ignores_removing_JValue_item(bool recurse)
    {
        // ARRANGE
        this.ArrangeBeginModification();

        var underlying = new JObject
        {
            ["container1"] = new JObject(),
            ["property"] = new JValue("property"),
        };

        var node = new JObjectAdapter(underlying);

        // ACT
        ((IRemoveChildItem)node).RemoveChildItem(provider: this.providerMock.Object, "property", recurse);

        // ASSERT
        // the value property is untouched
        Assert.True(underlying.TryGetValue("property", out var _));
    }

    #endregion IRemoveChildItem

    #region INewChildItem

    [Fact]
    public void NewChildItem_creates_JObject_item()
    {
        // ARRANGE
        this.ArrangeBeginModification();

        var underlying = new JObject
        {
            { "property" , "text" },
        };

        var node = new JObjectAdapter(underlying);

        // ACT
        var value = new JObject();
        var result = ((INewChildItem)node).NewChildItem(this.providerMock.Object, "container1", null, null);

        // ASSERT
        // the node was created as a container node
        Assert.True(result.Created);
        Assert.Equal("container1", result!.Name);

        // A JObjet was added to the parent JObject
        Assert.True(underlying.TryGetValue("container1", out var added));
    }

    [Fact]
    public void NewChildItem_creates_JObject_item_from_JObject()
    {
        // ARRANGE
        this.ArrangeBeginModification();

        var underlying = new JObject
        {
            { "property" , "text" },
        };

        var node = new JObjectAdapter(underlying);

        // ACT
        var value = new JObject()
        {
            { "property2" , "text" },
        };
        var result = ((INewChildItem)node).NewChildItem(this.providerMock.Object, "container1", "itemTypeValue", value);

        // ASSERT
        // the node was created as a container node
        Assert.True(result.Created);
        Assert.Equal("container1", result!.Name);

        // a JObject was added to the parent node
        Assert.True(underlying.TryGetValue("container1", out var added));

        // the property was kept as well.
        Assert.Equal("text", added["property2"].Value<string>());
    }

    [Fact]
    public void NewChildItem_creates_JObject_item_from_Json()
    {
        // ARRANGE
        this.ArrangeBeginModification();

        var underlying = new JObject
        {
            { "property" , "text" },
        };

        var node = new JObjectAdapter(underlying);

        // ACT
        var value = new JObject { ["property2"] = new JValue("text") }.ToString();

        var result = ((INewChildItem)node).NewChildItem(this.providerMock.Object, "container1", "itemTypeValue", value);

        // ASSERT
        // the node was created as container
        Assert.True(result.Created);
        Assert.Equal("container1", result!.Name);

        // the node was added as JObject to the parent node.
        Assert.True(underlying.TryGetValue("container1", out var added));

        // the property was kept too
        Assert.Equal("text", added!["property2"]!.Value<string>());
    }

    [Fact]
    public void NewChildItem_fails_for_existing_property()
    {
        // ARRANGE
        this.ArrangeBeginModification();

        var underlying = new JObject
        {
            { "property" , "text" },
        };

        var node = new JObjectAdapter(underlying);

        // ACT & ASSERT
        // try overriding the existing property
        var value = new JObject();
        var result = Assert.Throws<InvalidOperationException>(() => ((INewChildItem)node).NewChildItem(this.providerMock.Object, "property", null, null));

        // ASSERT
        Assert.Equal("A property(name='property') already exists", result.Message);
    }

    #endregion INewChildItem

    #region IRenameChildItem

    [Fact]
    public void RenameChildItem_renames_property()
    {
        // ARRANGE
        this.ArrangeBeginModification();

        var underlying = new JObject
        {
            ["container1"] = new JObject(),
        };

        var node = new JObjectAdapter(underlying);

        // ACT
        ((IRenameChildItem)node).RenameChildItem(this.providerMock.Object, "container1", "newname");

        // ASSERT
        // newname is there, container1 isn't
        Assert.True(underlying.TryGetValue("newname", out var _));
        Assert.False(underlying.TryGetValue("container1", out var _));
    }

    [Fact]
    public void RenameChildItem_fails_for_existing_property()
    {
        // ARRANGE
        this.ArrangeBeginModification();

        var underlying = new JObject
        {
            ["container1"] = new JObject(),
            ["newname"] = new JValue(1),
        };

        var node = new JObjectAdapter(underlying);

        // ACT
        // try to rename a property to an existing name
        ((IRenameChildItem)node).RenameChildItem(provider: this.providerMock.Object, "container1", "newname");

        // ASSERT
        // both properties are untouched
        Assert.True(underlying.TryGetValue("newname", out var _));
        Assert.True(underlying.TryGetValue("container1", out var _));
    }

    [Fact]
    public void RenameChildItem_fails_for_missing_property()
    {
        // ARRANGE
        this.ArrangeBeginModification();

        var underlying = new JObject
        {
            ["container1"] = new JObject(),
        };

        var node = new JObjectAdapter(underlying);

        // ACT
        // try to rname a property name which doesn't exist
        ((IRenameChildItem)node).RenameChildItem(this.providerMock.Object, "missing", "newname");

        // no property was added none was removed.
        Assert.False(underlying.TryGetValue("newname", out var _));
        Assert.True(underlying.TryGetValue("container1", out var _));
    }

    #endregion IRenameChildItem

    #region ICopyChildItem

    [Fact]
    public void CopyChildItem_copies_to_node_with_source_name()
    {
        // ARRANGE
        this.ArrangeBeginModification();

        var root = new JObject
        {
            ["child1"] = new JObject
            {
                ["data"] = 1,
                ["grandchild"] = new JObject()
            },
            ["child2"] = new JObject()
        };
        var nodeToCopy = root.Property("child1")!.Value as JObject;

        var rootNode = new RootNode(this.providerMock.Object, new JObjectAdapter(root));
        var dst = new JObjectAdapter((JObject)root.Property("child2")!.Value);

        // ACT
        // copy child1 under child2 as 'child1'
        var result = dst.GetRequiredService<ICopyChildItem>().CopyChildItem(
            provider: this.providerMock.Object,
            nodeToCopy: rootNode.GetChildItems(this.providerMock.Object).Single(n => n.Name == "child1"),
            destination: Array.Empty<string>());

        // ASSERT
        // source is still there
        Assert.NotNull(root.ChildObject("child1"));

        // child1 was created under child2
        Assert.True(result.Created);
        Assert.NotNull(root.ChildObject("child2")!.ChildObject("child1"));
        Assert.NotSame(nodeToCopy, root.ChildObject("child2").ChildObject("child1"));

        // property was copied
        Assert.Equal(1, root.ChildObject("child2")!.ChildObject("child1")!["data"]!.Value<int>());

        // copy is shallow: grandchild is missing
        Assert.False(root.ChildObject("child2").ChildObject("child1").TryGetValue("grandchild", out var _));
    }

    [Fact]
    public void CopyChildItem_copies_to_node_with_new_name()
    {
        // ARRANGE
        this.ArrangeBeginModification();

        var root = new JObject
        {
            ["child1"] = new JObject
            {
                ["data"] = 1,
                ["grandchild"] = new JObject()
            },
            ["child2"] = new JObject()
        };
        var nodeToCopy = root.Property("child1")!.Value as JObject;

        var rootNode = new RootNode(this.providerMock.Object, new JObjectAdapter(root));
        var dst = new JObjectAdapter((JObject)root.Property("child2")!.Value);

        // ACT
        // copy child1 under child2 as 'newname'
        var result = dst.GetRequiredService<ICopyChildItem>().CopyChildItem(
            provider: providerMock.Object,
            nodeToCopy: rootNode.GetChildItems(this.providerMock.Object).Single(n => n.Name == "child1"),
            destination: new string[] { "newname" });

        // ASSERT
        // new node was created
        Assert.True(result.Created);
        Assert.Equal("newname", result.Name);
        Assert.NotNull(root.ChildObject("child2").ChildObject("newname"));

        // copy is shallow: data is there but not grandchild
        Assert.True(root.ChildObject("child2").ChildObject("newname").TryGetValue("data", out var _));
        Assert.False(root.ChildObject("child2").ChildObject("newname").TryGetValue("gradchild", out var _));
    }

    [Fact]
    public void CopyChildItem_copies_to_node_with_new_parent_and_name()
    {
        // ARRANGE
        this.ArrangeBeginModification();

        var root = new JObject
        {
            ["child1"] = new JObject
            {
                ["data"] = 1,
                ["grandchild"] = new JObject()
            },
            ["child2"] = new JObject()
        };
        var nodeToCopy = root.Property("child1")!.Value as JObject;

        var rootNode = new RootNode(this.providerMock.Object, new JObjectAdapter(root));
        var dst = new JObjectAdapter((JObject)root.Property("child2")!.Value);

        // ACT
        // copy child1 under child2 as 'newparent/newname'
        var result = dst.GetRequiredService<ICopyChildItem>().CopyChildItem(
            provider: this.providerMock.Object,
            nodeToCopy: rootNode.GetChildItems(this.providerMock.Object).Single(n => n.Name == "child1"),
            destination: new string[] { "newparent", "newname" });

        // ASSERT
        // node was created
        Assert.True(result.Created);
        Assert.Equal("newname", result.Name);

        // parent node was created
        Assert.NotNull(root.ChildObject("child2").ChildObject("newparent"));
        // node was created under new parent
        Assert.NotNull(root.ChildObject("child2").ChildObject("newparent").ChildObject("newname"));

        // copy was shallow: grandchild is missing
        Assert.True(root.ChildObject("child2").ChildObject("newparent").ChildObject("newname").TryGetValue("data", out var _));
        Assert.False(root.ChildObject("child2").ChildObject("newparent").ChildObject("newname").TryGetValue("grandchild", out var _));
    }

    #endregion ICopyChildItem

    #region ICopyChildItemRecursive

    [Fact]
    public void CopyChildItem_copies_to_node_with_source_name_recursive()
    {
        // ARRANGE
        this.ArrangeBeginModification();

        var root = new JObject
        {
            ["child1"] = new JObject
            {
                ["data"] = 1,
                ["grandchild"] = new JObject
                {
                    ["value"] = 2
                }
            },
            ["child2"] = new JObject()
        };
        var nodeToCopy = root.Property("child1")!.Value as JObject;

        var rootNode = new RootNode(this.providerMock.Object, new JObjectAdapter(root));
        var dst = new JObjectAdapter((JObject)root.Property("child2")!.Value);

        // ACT
        // copy child1 under child2 as 'child1'
        var result = dst.GetRequiredService<ICopyChildItemRecursive>().CopyChildItemRecursive(
            provider: this.providerMock.Object,
            nodeToCopy: rootNode.GetChildItems(this.providerMock.Object).Single(n => n.Name == "child1"),
            destination: Array.Empty<string>());

        // ASSERT
        // source is still there
        Assert.NotNull(root.ChildObject("child1"));

        // child1 was created under child2
        Assert.True(result.Created);
        Assert.NotNull(root.ChildObject("child2")!.ChildObject("child1"));
        Assert.NotSame(nodeToCopy, root.ChildObject("child2").ChildObject("child1"));

        // property was copied
        Assert.Equal(1, root.ChildObject("child2")!.ChildObject("child1")!["data"]!.Value<int>());

        // copy is recursive: grandchild is with property as well
        Assert.True(root.ChildObject("child2").ChildObject("child1").TryGetValue("grandchild", out var _));
        Assert.True(root.ChildObject("child2").ChildObject("child1").ChildObject("grandchild").TryGetValue("value", out var _));
    }

    [Fact]
    public void CopyChildItem_copies_to_node_with_new_name_recursive()
    {
        // ARRANGE
        this.ArrangeBeginModification();

        var root = new JObject
        {
            ["child1"] = new JObject
            {
                ["data"] = 1,
                ["grandchild"] = new JObject
                {
                    ["value"] = 2
                }
            },
            ["child2"] = new JObject()
        };
        var nodeToCopy = root.Property("child1")!.Value as JObject;

        var rootNode = new RootNode(this.providerMock.Object, new JObjectAdapter(root));
        var dst = new JObjectAdapter((JObject)root.Property("child2")!.Value);

        // ACT
        // copy child1 under child2 as 'newname'
        var result = dst.GetRequiredService<ICopyChildItemRecursive>().CopyChildItemRecursive(
            provider: this.providerMock.Object,
            nodeToCopy: rootNode.GetChildItems(this.providerMock.Object).Single(n => n.Name == "child1"),
            destination: new string[] { "newname" });

        // ASSERT
        // new node was created
        Assert.True(result.Created);
        Assert.Equal("newname", result.Name);
        Assert.NotNull(root.ChildObject("child2").ChildObject("newname"));

        // copy is recursive: grandchild is with property as well
        Assert.True(root.ChildObject("child2").ChildObject("newname").TryGetValue("grandchild", out var _));
        Assert.True(root.ChildObject("child2").ChildObject("newname").ChildObject("grandchild").TryGetValue("value", out var _));
    }

    [Fact]
    public void CopyChildItem_copies_to_node_with_new_parent_and_name_recursive()
    {
        // ARRANGE
        this.ArrangeBeginModification();

        var root = new JObject
        {
            ["child1"] = new JObject
            {
                ["data"] = 1,
                ["grandchild"] = new JObject
                {
                    ["value"] = 2
                }
            },
            ["child2"] = new JObject()
        };
        var nodeToCopy = root.Property("child1")!.Value as JObject;

        var rootNode = new RootNode(this.providerMock.Object, new JObjectAdapter(root));
        var dst = new JObjectAdapter((JObject)root.Property("child2")!.Value);

        // ACT
        // copy child1 under child2 as 'newparent/newname'
        var result = dst.GetRequiredService<ICopyChildItemRecursive>().CopyChildItemRecursive(
            provider: this.providerMock.Object,
            nodeToCopy: rootNode.GetChildItems(this.providerMock.Object).Single(n => n.Name == "child1"),
            destination: new string[] { "newparent", "newname" });

        // ASSERT
        // node was created
        Assert.True(result.Created);
        Assert.Equal("newname", result.Name);

        // parent node was created
        Assert.NotNull(root.ChildObject("child2").ChildObject("newparent"));
        // node was created under new parent
        Assert.NotNull(root.ChildObject("child2").ChildObject("newparent").ChildObject("newname"));

        // copy is recursive: grandchild is with property as well
        Assert.True(root.ChildObject("child2").ChildObject("newparent").ChildObject("newname").TryGetValue("grandchild", out var _));
        Assert.True(root.ChildObject("child2").ChildObject("newparent").ChildObject("newname").ChildObject("grandchild").TryGetValue("value", out var _));
    }

    #endregion ICopyChildItemRecursive

    #region IMoveChildItem

    [Fact]
    public void MoveChildItem_moves_underlying()
    {
        // ARRANGE
        this.ArrangeBeginModification();

        var root = new JObject()
        {
            ["child1"] = new JObject
            {
                ["property"] = new JValue(1),
                ["grandchild"] = new JObject()
            },
            ["child2"] = new JObject()
        };

        var nodetoMove = root.ChildObject("child1");
        var rootNode = new RootNode(this.providerMock.Object, new JObjectAdapter(root));
        var dst = new JObjectAdapter(root.ChildObject("child2")!);

        // ACT
        // move child1 under child2 as child1
        dst.GetRequiredService<IMoveChildItem>().MoveChildItem(
            provider: this.providerMock.Object,
            parentOfNodeToMove: rootNode,
            nodeToMove: rootNode.GetChildItems(this.providerMock.Object).Single(n => n.Name == "child1"),
            destination: Array.Empty<string>());

        // ASSERT
        // child1 was created under child2
        Assert.NotNull(root.ChildObject("child2").ChildObject("child1"));

        // child1 under root is gone
        Assert.False(root.TryGetValue("child1", out var _));

        // copy was deep: value and object property are there
        Assert.True(root.ChildObject("child2").ChildObject("child1").TryGetValue("property", out var _));
        Assert.True(root.ChildObject("child2").ChildObject("child1").TryGetValue("grandchild", out var _));
    }

    [Fact]
    public void MoveChildItem_moves_underlying_with_new_name()
    {
        // ARRANGE
        this.ArrangeBeginModification();

        var root = new JObject()
        {
            ["child1"] = new JObject
            {
                ["property"] = new JValue(1),
                ["grandchild"] = new JObject()
            },
            ["child2"] = new JObject()
        };

        var nodetoMove = root.ChildObject("child1");
        var rootNode = new RootNode(this.providerMock.Object, new JObjectAdapter(root));
        var dst = new JObjectAdapter(root.ChildObject("child2")!);

        // ACT
        // move child1 under child2 as newname
        dst.GetRequiredService<IMoveChildItem>().MoveChildItem(
            provider: this.providerMock.Object,
            parentOfNodeToMove: rootNode,
            nodeToMove: rootNode.GetChildItems(this.providerMock.Object).Single(n => n.Name == "child1"),
            destination: new[] { "newname" });

        // ASSERT
        // child1 was created under child2
        Assert.NotNull(root.ChildObject("child2").ChildObject("newname"));

        // copy was deep: value and object property are there
        Assert.True(root.ChildObject("child2").ChildObject("newname").TryGetValue("property", out var _));
        Assert.True(root.ChildObject("child2").ChildObject("newname").TryGetValue("grandchild", out var _));
    }

    [Fact]
    public void MoveChildItem_moves_underlying_with_new_parent_and_name()
    {
        // ARRANGE
        this.ArrangeBeginModification();

        var root = new JObject()
        {
            ["child1"] = new JObject
            {
                ["property"] = new JValue(1),
                ["grandchild"] = new JObject()
            },
            ["child2"] = new JObject()
        };

        var nodetoMove = root.ChildObject("child1");
        var rootNode = new RootNode(this.providerMock.Object, new JObjectAdapter(root));
        var dst = new JObjectAdapter(root.ChildObject("child2")!);

        // ACT
        // move child1 under child2 as newparent/newname
        dst.GetRequiredService<IMoveChildItem>().MoveChildItem(
            provider: this.providerMock.Object,
            parentOfNodeToMove: rootNode,
            nodeToMove: rootNode.GetChildItems(this.providerMock.Object).Single(n => n.Name == "child1"),
            destination: new[] { "newparent", "newname" });

        // ASSERT
        // newparent was created under child2
        Assert.NotNull(root.ChildObject("child2").ChildObject("newparent"));

        // child1 was created under newparent
        Assert.NotNull(root.ChildObject("child2").ChildObject("newparent").ChildObject("newname"));

        // copy was deep: value and object property are there
        Assert.True(root.ChildObject("child2").ChildObject("newparent").ChildObject("newname").TryGetValue("property", out var _));
        Assert.True(root.ChildObject("child2").ChildObject("newparent").ChildObject("newname").TryGetValue("grandchild", out var _));
    }

    #endregion IMoveChildItem

    #region IClearItemProperty

    [Fact]
    public void ClearItemProperty_nulls_value_property()
    {
        // ARRANGE
        var root = new JObject
        {
            ["data1"] = "text",
            ["data2"] = 1
        };
        var rootNode = new JObjectAdapter(root);

        this.ArrangeBeginModification();

        // ACT
        rootNode.GetRequiredService<IClearItemProperty>().ClearItemProperty(this.providerMock.Object, new[] { "data1", "data2" });

        // ASSERT
        // properties still exist but are nulled
        Assert.True(root.TryGetValue("data1", out var v1));
        Assert.Equal(JTokenType.Null, v1.Type);
        Assert.True(root.TryGetValue("data2", out var v2));
        Assert.Equal(JTokenType.Null, v2.Type);
    }

    [Fact]
    public void ClearItemProperty_ignores_unknown_property()
    {
        // ARRANGE
        var root = new JObject
        {
            ["data"] = "text"
        };
        var rootNode = new JObjectAdapter(root);

        this.ArrangeBeginModification();

        // ACT
        rootNode.GetRequiredService<IClearItemProperty>().ClearItemProperty(this.providerMock.Object, new[] { "unkown" });

        // ASSERT
        // property wasn't created
        Assert.False(root.TryGetValue("unknown", out var _));
        // other properties are untouched
        Assert.True(root.TryGetValue("data", out var value));
        Assert.Equal("text", value);
    }

    [Fact]
    public void ClearItemProperty_ignores_child_node()
    {
        // ARRANGE
        var root = new JObject
        {
            ["data"] = new JObject()
        };
        var rootNode = new JObjectAdapter(root);

        this.ArrangeBeginModification();

        // ACT
        rootNode.GetRequiredService<IClearItemProperty>().ClearItemProperty(this.providerMock.Object, new[] { "data" });

        // ASSERT
        // the child node is untouched
        Assert.True(root.TryGetValue("data", out var value));
        Assert.IsType<JObject>(value);
    }

    #endregion IClearItemProperty

    #region ISetItemProperty

    [Fact]
    public void SetItemProperty_sets_property_value()
    {
        // ARRANGE
        var root = new JObject
        {
            ["data1"] = "text",
            ["data2"] = 1
        };
        var rootNode = new JObjectAdapter(root);

        this.ArrangeBeginModification();

        // ACT
        rootNode.GetRequiredService<ISetItemProperty>().SetItemProperty(this.providerMock.Object, new PSObject(new
        {
            data1 = "changed",
            data2 = new int[] { 1, 2 }
        }));

        // ASSERT
        Assert.True(root.TryGetValue("data1", out var v1));
        Assert.Equal("changed", v1);
        Assert.True(root.TryGetValue("data2", out var v2));
        Assert.Equal(new JArray(1, 2), v2);
    }

    [Fact]
    public void SetItemProperty_sets_property_null()
    {
        // ARRANGE
        var root = new JObject
        {
            ["data1"] = "text",
            ["data2"] = 1
        };
        var rootNode = new JObjectAdapter(root);

        this.ArrangeBeginModification();

        // ACT
        rootNode.GetRequiredService<ISetItemProperty>().SetItemProperty(provider: this.providerMock.Object, new PSObject(new
        {
            data1 = (object)null,
        }));

        // ASSERT
        // property has value of null
        Assert.True(root.TryGetValue("data1", out var v1));
        Assert.Equal(JTokenType.Null, v1!.Type);
    }

    [Fact]
    public void SetItemProperty_ignores_child_nodes()
    {
        // ARRANGE
        var root = new JObject
        {
            ["data1"] = "text",
            ["data2"] = new JObject
            {
                ["data3"] = "data3"
            }
        };
        var rootNode = new JObjectAdapter(root);

        this.ArrangeBeginModification();

        // ACT
        rootNode.GetRequiredService<ISetItemProperty>().SetItemProperty(this.providerMock.Object, new PSObject(new
        {
            data1 = "changed",
            data2 = 3
        }));

        // ASSERT
        // the values were set
        Assert.True(root.TryGetValue("data1", out var v1));
        Assert.Equal("changed", v1);
        Assert.True(root.TryGetValue("data2", out var v2));
        Assert.IsType<JObject>(v2);
    }

    [Fact]
    public void SetItemProperty_ignores_unknown_property()
    {
        // ARRANGE
        var root = new JObject
        {
            ["data1"] = "text",
        };
        var rootNode = new JObjectAdapter(root);

        this.ArrangeBeginModification();

        // ACT
        rootNode.GetRequiredService<ISetItemProperty>().SetItemProperty(this.providerMock.Object, new PSObject(new
        {
            unkown = "changed",
        }));

        // ASSERT
        Assert.True(root.TryGetValue("data1", out var v1));
        Assert.Equal("text", v1);
        Assert.False(root.TryGetValue("unknown", out var _));
    }

    [Fact]
    public void SetItemProperty_ignores_non_scalar_value()
    {
        // ARRANGE
        var root = new JObject
        {
            ["data1"] = "text",
        };
        var rootNode = new JObjectAdapter(root);

        this.ArrangeBeginModification();

        // ACT
        rootNode.GetRequiredService<ISetItemProperty>().SetItemProperty(this.providerMock.Object, new PSObject(new
        {
            data1 = new { property = "changed" },
        }));

        // ASSERT
        // data1 still has its old value, a child wasn't created
        Assert.True(root.TryGetValue("data1", out var v1));
        Assert.Equal("text", v1);
    }

    #endregion ISetItemProperty

    #region IRemoveItemProperty

    [Fact]
    public void RemoveItemProperty_removes_data_property()
    {
        // ARRANGE
        var root = new JObject
        {
            ["value"] = "text",
        };
        var rootAdapter = new JObjectAdapter(root);

        this.ArrangeBeginModification();

        // ACT
        rootAdapter.GetRequiredService<IRemoveItemProperty>().RemoveItemProperty(this.providerMock.Object, "value");

        // ASSERT
        // not only the value but also the property is removed.
        Assert.False(root.TryGetValue("value", out var _));
    }

    [Fact]
    public void RemoveItemProperty_removes_array_property()
    {
        // ARRANGE
        this.ArrangeBeginModification();

        var root = new JObject
        {
            ["array"] = new JArray(1, 2)
        };
        var rootAdapter = new JObjectAdapter(root);

        // ACT
        rootAdapter.GetRequiredService<IRemoveItemProperty>().RemoveItemProperty(provider: this.providerMock.Object, "array");

        // ASSERT
        // not only the value but also the property is removed.
        Assert.False(root.TryGetValue("array", out var _));
    }

    [Fact]
    public void RemoveItemProperty_removing_property_ignores_child_properties()
    {
        // ARRANGE
        this.ArrangeBeginModification();

        var root = new JObject
        {
            ["child"] = new JObject(),
        };
        var rootAdapter = new JObjectAdapter(root);

        // ACT
        rootAdapter.GetRequiredService<IRemoveItemProperty>().RemoveItemProperty(provider: this.providerMock.Object, "child");

        // ASSERT
        // not only the value but also the property is removed.
        Assert.True(root.TryGetValue("child", out var _));
    }

    #endregion IRemoveItemProperty

    #region ICopyItemProperty

    [Fact]
    public void CopyItemProperty_set_new_properties_value()
    {
        // ARRANGE
        this.ArrangeBeginModification();

        var child = new JObject();
        var childAdapter = new JObjectAdapter(child);
        var childNode = new ContainerNode(this.providerMock.Object, "child1", childAdapter);

        var root = new JObject
        {
            ["data1"] = "text",
            ["child"] = child
        };
        var rootNode = new RootNode(this.providerMock.Object, new JObjectAdapter(root));

        // ACT
        childNode.CopyItemProperty(rootNode, "data1", "data1");

        // ASSERT
        Assert.True(child.TryGetValue("data1", out var value));
        Assert.Equal("text", value);
    }

    [Fact]
    public void CopyItemProperty_ignores_object_property()
    {
        // ARRANGE
        this.ArrangeBeginModification();

        var child = new JObject();
        var childAdapter = new JObjectAdapter(child);
        var childNode = new ContainerNode(this.providerMock.Object, "child1", childAdapter);

        var root = new JObject
        {
            ["data1"] = new JObject(),
            ["child"] = child
        };
        var rootNode = new RootNode(this.providerMock.Object, new JObjectAdapter(root));

        // ACT
        childNode.CopyItemProperty(rootNode, "data1", "data1");

        // ASSERT
        // property wasn't copied
        Assert.False(child.TryGetValue("data1", out var value));
    }

    [Fact]
    public void CopyItemProperty_ignores_missing_source_property()
    {
        // ARRANGE
        this.ArrangeBeginModification();

        var child = new JObject();
        var childAdapter = new JObjectAdapter(child);
        var childNode = new ContainerNode(this.providerMock.Object, "child1", childAdapter);

        var root = new JObject
        {
            ["child"] = child
        };
        var rootNode = new RootNode(this.providerMock.Object, new JObjectAdapter(root));

        // ACT
        childNode.CopyItemProperty(rootNode, "data1", "data1");

        // ASSERT
        // property wasn't copied
        Assert.False(child.TryGetValue("data1", out var value));
    }

    [Fact]
    public void CopyItemProperty_ignores_duplicate_property()
    {
        // ARRANGE
        this.ArrangeBeginModification();

        var child = new JObject
        {
            ["data1"] = 1
        };

        var childAdapter = new JObjectAdapter(child);
        var childNode = new ContainerNode(this.providerMock.Object, "child1", childAdapter);

        var root = new JObject
        {
            ["data1"] = "text",
            ["child"] = child
        };
        var rootNode = new RootNode(this.providerMock.Object, new JObjectAdapter(root));

        // ACT
        childNode.CopyItemProperty(rootNode, "data1", "data1");

        // ASSERT
        Assert.True(child.TryGetValue("data1", out var value));
        Assert.Equal(1, value);
    }

    #endregion ICopyItemProperty

    #region IMoveItemProperty

    [Fact]
    public void MoveItemProperty_moves_property_value()
    {
        // ARRANGE
        this.ArrangeBeginModification();

        var child = new JObject();
        var childAdapter = new JObjectAdapter(child);
        var childNode = new LeafNode(this.providerMock.Object, "child1", childAdapter);

        var root = new JObject
        {
            ["data1"] = "text",
            ["child"] = child
        };
        var rootNode = new RootNode(this.providerMock.Object, new JObjectAdapter(root));

        // ACT
        childNode.MoveItemProperty(rootNode, sourcePropertyName: "data1", destinationPropertyName: "data1");

        // ASSERT
        // root has property removed
        Assert.False(root.TryGetValue("data1", out var _));

        // child has property added
        Assert.True(child.TryGetValue("data1", out var value));
        Assert.Equal("text", value);
    }

    [Fact]
    public void MoveItemProperty_ignores_object_properties()
    {
        // ARRANGE
        this.ArrangeBeginModification();

        var child = new JObject();
        var childAdapter = new JObjectAdapter(child);
        var childNode = new LeafNode(this.providerMock.Object, "child1", childAdapter);

        var root = new JObject
        {
            ["object"] = new JObject(),
            ["child"] = child
        };
        var rootNode = new RootNode(this.providerMock.Object, new JObjectAdapter(root));

        // ACT
        childNode.MoveItemProperty(rootNode, sourcePropertyName: "object", destinationPropertyName: "data1");

        // ASSERT
        // root still hast the object property
        Assert.True(root.TryGetValue("object", out var _));
        // child hasn't got a new object property
        Assert.False(child.TryGetValue("data1", out var _));
    }

    [Fact]
    public void MoveItemProperty_ignores_missing_source_property()
    {
        // ARRANGE
        this.ArrangeBeginModification();

        var child = new JObject();
        var childAdapter = new JObjectAdapter(child);
        var childNode = new LeafNode(this.providerMock.Object, "child1", childAdapter);

        var root = new JObject
        {
            ["data1"] = "text",
            ["child"] = child
        };
        var rootNode = new RootNode(this.providerMock.Object, new JObjectAdapter(root));

        // ACT
        childNode.MoveItemProperty(rootNode, sourcePropertyName: "missing", destinationPropertyName: "data1");

        // ASSERT
        // child hasn't got a property
        Assert.False(child.TryGetValue("data1", out var value));
    }

    [Fact]
    public void MoveItemPropertyignores_duplicate_property()
    {
        // ARRANGE
        this.ArrangeBeginModification();

        var child = new JObject
        {
            ["data1"] = 1
        };

        var childAdapter = new JObjectAdapter(child);
        var childNode = new LeafNode(this.providerMock.Object, "child1", childAdapter);

        var root = new JObject
        {
            ["data1"] = "text",
            ["child"] = child
        };
        var rootNode = new RootNode(this.providerMock.Object, new JObjectAdapter(root));

        // ACT
        childNode.MoveItemProperty(rootNode, sourcePropertyName: "data1", destinationPropertyName: "data1");

        // ASSERT
        // root has property removed
        Assert.False(root.TryGetValue("data1", out var _));

        // child has property added
        Assert.True(child.TryGetValue("data1", out var value));
        Assert.Equal(1, value);
    }

    #endregion IMoveItemProperty

    #region INewItemProperty

    [Fact]
    public void NewItemProperty_creates_data_property()
    {
        // ARRANGE
        var root = new JObject();
        var rootAdapter = new JObjectAdapter(root);

        this.ArrangeBeginModification();

        // ACT
        rootAdapter.GetRequiredService<INewItemProperty>().NewItemProperty(this.providerMock.Object, "data1", null, 1);

        // ASSERT
        // new property was created
        Assert.True(root.TryGetValue("data1", out var value));
        Assert.Equal(1, value);
    }

    [Fact]
    public void NewItemProperty_fails_on_duplicate_name()
    {
        // ARRANGE
        var root = new JObject
        {
            ["data1"] = "text"
        };

        var rootAdapter = new JObjectAdapter(root);

        this.ArrangeBeginModification();

        // ACT
        rootAdapter.GetRequiredService<INewItemProperty>().NewItemProperty(this.providerMock.Object, "data1", null, 1);

        // ASSERT
        // old property is still there
        Assert.True(root.TryGetValue("data1", out var value));
        Assert.Equal("text", value);
    }

    [Fact]
    public void NewItemProperty_fails_on_object_value()
    {
        // ARRANGE
        var root = new JObject();
        var rootAdapter = new JObjectAdapter(root);

        this.ArrangeBeginModification();

        // ACT
        rootAdapter.GetRequiredService<INewItemProperty>().NewItemProperty(this.providerMock.Object, "data1", null, new { data = 1 });

        // ASSERT
        // property wasn't created
        Assert.False(root.TryGetValue("data1", out var _));
    }

    [Fact]
    public void NewItemProperty_set_property_as_null()
    {
        // ARRANGE
        var root = new JObject();
        var rootAdapter = new JObjectAdapter(root);

        this.ArrangeBeginModification();

        // ACT
        rootAdapter.GetRequiredService<INewItemProperty>().NewItemProperty(this.providerMock.Object, "data1", null, null);

        // ASSERT
        // property wasn't created
        Assert.True(root.TryGetValue("data1", out var value));
        Assert.Equal(JTokenType.Null, value.Type);
    }

    #endregion INewItemProperty

    #region IRenameItemProperty

    [Fact]
    public void RenameItemProperty_renames_data_property()
    {
        // ARRANGE
        const string? data = "text";
        var root = new JObject
        {
            ["data"] = data,
        };

        var rootAdapter = new JObjectAdapter(root);

        this.ArrangeBeginModification();

        // ACT
        rootAdapter.GetRequiredService<IRenameItemProperty>().RenameItemProperty(this.providerMock.Object, "data", "newname");

        // ASSERT
        Assert.True(root.TryGetValue("newname", out var value));
        Assert.Same(data, ((JValue)value).Value);
    }

    [Fact]
    public void RenameItemProperty_ignores_object_property()
    {
        // ARRANGE
        var root = new JObject
        {
            ["data"] = new JObject(),
        };

        var rootAdapter = new JObjectAdapter(root);

        this.ArrangeBeginModification();

        // ACT
        rootAdapter.GetRequiredService<IRenameItemProperty>().RenameItemProperty(this.providerMock.Object, "data", "newname");

        // ASSERT
        // property wasn't renamed
        Assert.False(root.TryGetValue("newname", out var value));
    }

    [Fact]
    public void RenameItemProperty_ignores_duplicate_property()
    {
        // ARRANGE
        const string? data = "text";
        var root = new JObject
        {
            ["data"] = data,
            ["newname"] = 1,
        };

        var rootAdapter = new JObjectAdapter(root);

        this.ArrangeBeginModification();

        // ACT
        rootAdapter.GetRequiredService<IRenameItemProperty>().RenameItemProperty(this.providerMock.Object, "data", "newname");

        // ASSERT
        // properties are unchanged
        Assert.True(root.TryGetValue("data", out var dataValue));
        Assert.Equal(data, ((JValue)dataValue).Value);
        Assert.True(root.TryGetValue("newname", out var newnameValue));
        Assert.Equal(1L, ((JValue)newnameValue).Value);
    }

    #endregion IRenameItemProperty
}