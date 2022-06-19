using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Management.Automation;
using TreeStore.Core;
using TreeStore.Core.Capabilities;
using TreeStore.Core.Nodes;
using Xunit;

namespace TreeStore.JsonFS.Test;

public class JObjectAdapterTest
{
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
        var result = ((IGetItem)node).GetItem();

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
        node.GetRequiredService<ISetItem>().SetItem(newData);

        // ASSERT
        // array was added, value overrides the old value
        var psobject = node.GetRequiredService<IGetItem>().GetItem();

        Assert.Equal(1, psobject!.Property<long>("value"));
        Assert.Equal(new object[] { 1, 2 }, psobject!.Property<object[]>("array"));

        // value2 was removed
        Assert.Null(psobject!.Properties.FirstOrDefault(p => p.Name == "value2"));

        // object was added
        Assert.Equal("object", node.GetRequiredService<IGetChildItem>().GetChildItems().Single().Name);
    }

    [Fact]
    public void SetItem_replaces_from_string()
    {
        // ARRANGE

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
        node.GetRequiredService<ISetItem>().SetItem(newData.ToString());

        // ASSERT
        // array was added, value overrides the old value
        var psobject = node.GetRequiredService<IGetItem>().GetItem();

        Assert.Equal(1, psobject!.Property<long>("value"));
        Assert.Equal(new object[] { (long)1, (long)2 }, psobject!.Property<object[]>("array"));

        // value2 was removed
        Assert.Null(psobject!.Properties.FirstOrDefault(p => p.Name == "value2"));

        // object was added
        Assert.Equal("object", node.GetRequiredService<IGetChildItem>().GetChildItems().Single().Name);
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
        node.GetRequiredService<IClearItem>().ClearItem();

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
        var psobject = node.GetRequiredService<IGetItem>().GetItem();

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
        var result = ((IGetChildItem)node).HasChildItems();

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
        var result = ((IGetChildItem)node).GetChildItems().ToArray();

        // ASSERT
        Assert.Single(result);

        var pso = result.Single().GetItem();

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
        var result = ((IGetChildItem)node).GetChildItems().ToArray();

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
        var underlying = new JObject
        {
            ["container1"] = new JObject(),
            ["property"] = new JValue("property"),
        };

        var node = new JObjectAdapter(underlying);

        // ACT
        ((IRemoveChildItem)node).RemoveChildItem("container1", recurse);

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
        var underlying = new JObject
        {
            ["container1"] = new JObject(),
            ["property"] = new JValue("property"),
        };

        var node = new JObjectAdapter(underlying);

        // ACT
        ((IRemoveChildItem)node).RemoveChildItem("property", recurse);

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
        var underlying = new JObject
        {
            { "property" , "text" },
        };

        var node = new JObjectAdapter(underlying);

        // ACT
        var value = new JObject();
        var result = ((INewChildItem)node).NewChildItem("container1", null, null);

        // ASSERT
        // the node was created as a container node
        Assert.NotNull(result);
        Assert.Equal("container1", result!.Name);
        Assert.True(result is ContainerNode);

        // A JObjet was added to the parent JObject
        Assert.True(underlying.TryGetValue("container1", out var added));
    }

    [Fact]
    public void NewChildItem_creates_JObject_item_from_JObject()
    {
        // ARRANGE
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
        var result = ((INewChildItem)node).NewChildItem("container1", "itemTypeValue", value);

        // ASSERT
        // the node was created as a container node
        Assert.NotNull(result);
        Assert.Equal("container1", result!.Name);
        Assert.True(result is ContainerNode);

        // a JObject was added to the parent node
        Assert.True(underlying.TryGetValue("container1", out var added));

        // the property was kept as well.
        Assert.Equal("text", added["property2"].Value<string>());
    }

    [Fact]
    public void NewChildItem_creates_JObject_item_from_Json()
    {
        // ARRANGE
        var underlying = new JObject
        {
            { "property" , "text" },
        };

        var node = new JObjectAdapter(underlying);

        // ACT
        var value = new JObject { ["property2"] = new JValue("text") }.ToString();

        var result = ((INewChildItem)node).NewChildItem("container1", "itemTypeValue", value);

        // ASSERT
        // the node was created as container
        Assert.NotNull(result);
        Assert.Equal("container1", result!.Name);
        Assert.True(result is ContainerNode);

        // the node was added as JObject to the parent node.
        Assert.True(underlying.TryGetValue("container1", out var added));

        // the property was kept too
        Assert.Equal("text", added!["property2"]!.Value<string>());
    }

    [Fact]
    public void NewChildItem_fails_for_existing_property()
    {
        // ARRANGE
        var underlying = new JObject
        {
            { "property" , "text" },
        };

        var node = new JObjectAdapter(underlying);

        // ACT & ASSERT
        // try overriding the existing property
        var value = new JObject();
        var result = Assert.Throws<InvalidOperationException>(() => ((INewChildItem)node).NewChildItem("property", null, null));

        // ASSERT
        Assert.Equal("A property(name='property') already exists", result.Message);
    }

    #endregion INewChildItem

    #region IRenameChildItem

    [Fact]
    public void RenameChildItem_renames_property()
    {
        var underlying = new JObject
        {
            ["container1"] = new JObject(),
        };

        var node = new JObjectAdapter(underlying);

        // ACT
        ((IRenameChildItem)node).RenameChildItem("container1", "newname");

        // ASSERT
        // newname is there, container1 isn't
        Assert.True(underlying.TryGetValue("newname", out var _));
        Assert.False(underlying.TryGetValue("container1", out var _));
    }

    [Fact]
    public void RenameChildItem_fails_for_existing_property()
    {
        var underlying = new JObject
        {
            ["container1"] = new JObject(),
            ["newname"] = new JValue(1),
        };

        var node = new JObjectAdapter(underlying);

        // ACT
        // try to rename a property to an existing name
        ((IRenameChildItem)node).RenameChildItem("container1", "newname");

        // ASSERT
        // both properties are untouched
        Assert.True(underlying.TryGetValue("newname", out var _));
        Assert.True(underlying.TryGetValue("container1", out var _));
    }

    [Fact]
    public void RenameChildItem_fails_for_missing_property()
    {
        var underlying = new JObject
        {
            ["container1"] = new JObject(),
        };

        var node = new JObjectAdapter(underlying);

        // ACT
        // try to rname a property name which doesn't exist
        ((IRenameChildItem)node).RenameChildItem("missing", "newname");

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

        var rootNode = new RootNode(new JObjectAdapter(root));
        var dst = new JObjectAdapter((JObject)root.Property("child2")!.Value);

        // ACT
        // copy child1 under child2 as 'child1'
        var result = dst.GetRequiredService<ICopyChildItem>().CopyChildItem(
            nodeToCopy: rootNode.GetChildItems().Single(n => n.Name == "child1"),
            destination: Array.Empty<string>());

        // ASSERT
        // source is still there
        Assert.NotNull(root.ChildObject("child1"));

        // child1 was created under child2
        Assert.IsType<ContainerNode>(result);
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

        var rootNode = new RootNode(new JObjectAdapter(root));
        var dst = new JObjectAdapter((JObject)root.Property("child2")!.Value);

        // ACT
        // copy child1 under child2 as 'newname'
        var result = dst.GetRequiredService<ICopyChildItem>().CopyChildItem(
            nodeToCopy: rootNode.GetChildItems().Single(n => n.Name == "child1"),
            destination: new string[] { "newname" });

        // ASSERT
        // new node was created
        Assert.IsType<ContainerNode>(result);
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

        var rootNode = new RootNode(new JObjectAdapter(root));
        var dst = new JObjectAdapter((JObject)root.Property("child2")!.Value);

        // ACT
        // copy child1 under child2 as 'newparent/newname'
        var result = dst.GetRequiredService<ICopyChildItem>().CopyChildItem(
            nodeToCopy: rootNode.GetChildItems().Single(n => n.Name == "child1"),
            destination: new string[] { "newparent", "newname" });

        // ASSERT
        // node was created
        Assert.IsType<ContainerNode>(result);
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

    #region CopyChildItemRecursive

    [Fact]
    public void CopyChildItem_copies_to_node_with_source_name_recursive()
    {
        // ARRANGE
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

        var rootNode = new RootNode(new JObjectAdapter(root));
        var dst = new JObjectAdapter((JObject)root.Property("child2")!.Value);

        // ACT
        // copy child1 under child2 as 'child1'
        var result = dst.GetRequiredService<ICopyChildItemRecursive>().CopyChildItemRecursive(
            nodeToCopy: rootNode.GetChildItems().Single(n => n.Name == "child1"),
            destination: Array.Empty<string>());

        // ASSERT
        // source is still there
        Assert.NotNull(root.ChildObject("child1"));

        // child1 was created under child2
        Assert.IsType<ContainerNode>(result);
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

        var rootNode = new RootNode(new JObjectAdapter(root));
        var dst = new JObjectAdapter((JObject)root.Property("child2")!.Value);

        // ACT
        // copy child1 under child2 as 'newname'
        var result = dst.GetRequiredService<ICopyChildItemRecursive>().CopyChildItemRecursive(
            nodeToCopy: rootNode.GetChildItems().Single(n => n.Name == "child1"),
            destination: new string[] { "newname" });

        // ASSERT
        // new node was created
        Assert.IsType<ContainerNode>(result);
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

        var rootNode = new RootNode(new JObjectAdapter(root));
        var dst = new JObjectAdapter((JObject)root.Property("child2")!.Value);

        // ACT
        // copy child1 under child2 as 'newparent/newname'
        var result = dst.GetRequiredService<ICopyChildItemRecursive>().CopyChildItemRecursive(
            nodeToCopy: rootNode.GetChildItems().Single(n => n.Name == "child1"),
            destination: new string[] { "newparent", "newname" });

        // ASSERT
        // node was created
        Assert.IsType<ContainerNode>(result);
        Assert.Equal("newname", result.Name);

        // parent node was created
        Assert.NotNull(root.ChildObject("child2").ChildObject("newparent"));
        // node was created under new parent
        Assert.NotNull(root.ChildObject("child2").ChildObject("newparent").ChildObject("newname"));

        // copy is recursive: grandchild is with property as well
        Assert.True(root.ChildObject("child2").ChildObject("newparent").ChildObject("newname").TryGetValue("grandchild", out var _));
        Assert.True(root.ChildObject("child2").ChildObject("newparent").ChildObject("newname").ChildObject("grandchild").TryGetValue("value", out var _));
    }

    #endregion CopyChildItemRecursive

    #region IMoveChildItem

    [Fact]
    public void MoveChildItem_moves_underlying()
    {
        // ARRANGE
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
        var rootNode = new RootNode(new JObjectAdapter(root));
        var dst = new JObjectAdapter(root.ChildObject("child2")!);

        // ACT
        // move child1 under child2 as child1
        dst.GetRequiredService<IMoveChildItem>().MoveChildItem(
            parentOfNodeToMove: rootNode,
            nodeToMove: rootNode.GetChildItems().Single(n => n.Name == "child1"),
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
        var rootNode = new RootNode(new JObjectAdapter(root));
        var dst = new JObjectAdapter(root.ChildObject("child2")!);

        // ACT
        // move child1 under child2 as newname
        dst.GetRequiredService<IMoveChildItem>().MoveChildItem(
            parentOfNodeToMove: rootNode,
            nodeToMove: rootNode.GetChildItems().Single(n => n.Name == "child1"),
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
        var rootNode = new RootNode(new JObjectAdapter(root));
        var dst = new JObjectAdapter(root.ChildObject("child2")!);

        // ACT
        // move child1 under child2 as newparent/newname
        dst.GetRequiredService<IMoveChildItem>().MoveChildItem(
            parentOfNodeToMove: rootNode,
            nodeToMove: rootNode.GetChildItems().Single(n => n.Name == "child1"),
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

        // ACT
        rootNode.GetRequiredService<IClearItemProperty>().ClearItemProperty(new[] { "data1", "data2" });

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

        // ACT
        rootNode.GetRequiredService<IClearItemProperty>().ClearItemProperty(new[] { "unkown" });

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

        // ACT
        rootNode.GetRequiredService<IClearItemProperty>().ClearItemProperty(new[] { "data" });

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

        // ACT
        rootNode.GetRequiredService<ISetItemProperty>().SetItemProperty(new PSObject(new
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

        // ACT
        rootNode.GetRequiredService<ISetItemProperty>().SetItemProperty(new PSObject(new
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

        // ACT
        rootNode.GetRequiredService<ISetItemProperty>().SetItemProperty(new PSObject(new
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

        // ACT
        rootNode.GetRequiredService<ISetItemProperty>().SetItemProperty(new PSObject(new
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

        // ACT
        rootAdapter.GetRequiredService<IRemoveItemProperty>().RemoveItemProperty("value");

        // ASSERT
        // not only the value but also the property is removed.
        Assert.False(root.TryGetValue("value", out var _));
    }

    [Fact]
    public void RemoveItemProperty_removes_array_property()
    {
        // ARRANGE
        var root = new JObject
        {
            ["array"] = new JArray(1, 2)
        };
        var rootAdapter = new JObjectAdapter(root);

        // ACT
        rootAdapter.GetRequiredService<IRemoveItemProperty>().RemoveItemProperty("array");

        // ASSERT
        // not only the value but also the property is removed.
        Assert.False(root.TryGetValue("array", out var _));
    }

    [Fact]
    public void RemoveItemProperty_removing_property_ignores_child_properties()
    {
        // ARRANGE
        var root = new JObject
        {
            ["child"] = new JObject(),
        };
        var rootAdapter = new JObjectAdapter(root);

        // ACT
        rootAdapter.GetRequiredService<IRemoveItemProperty>().RemoveItemProperty("child");

        // ASSERT
        // not only the value but also the property is removed.
        Assert.True(root.TryGetValue("child", out var _));
    }

    #endregion IRemoveItemProperty

    #region IMoveItemProperty

    [Fact]
    public void MoveItemProperty_moves_property_value()
    {
        // ARRANGE
        var child = new JObject();
        var childAdapter = new JObjectAdapter(child);
        var childNode = new LeafNode("child1", childAdapter);

        var root = new JObject
        {
            ["data1"] = "text",
            ["child"] = child
        };
        var rootNode = new RootNode(new JObjectAdapter(root));

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
    public void MoveItemProperty_moving_ignores_object_properties()
    {
        // ARRANGE
        var child = new JObject();
        var childAdapter = new JObjectAdapter(child);
        var childNode = new LeafNode("child1", childAdapter);

        var root = new JObject
        {
            ["object"] = new JObject(),
            ["child"] = child
        };
        var rootNode = new RootNode(new JObjectAdapter(root));

        // ACT
        childNode.MoveItemProperty(rootNode, sourcePropertyName: "object", destinationPropertyName: "data1");

        // ASSERT
        // root still hast the objectc property
        Assert.True(root.TryGetValue("object", out var _));
        // child hasn't got a new object property
        Assert.False(child.TryGetValue("data1", out var _));
    }

    [Fact]
    public void MoveItemProperty_moving_ignores_missing_source_property()
    {
        // ARRANGE
        var child = new JObject();
        var childAdapter = new JObjectAdapter(child);
        var childNode = new LeafNode("child1", childAdapter);

        var root = new JObject
        {
            ["data1"] = "text",
            ["child"] = child
        };
        var rootNode = new RootNode(new JObjectAdapter(root));

        // ACT
        childNode.MoveItemProperty(rootNode, sourcePropertyName: "missing", destinationPropertyName: "data1");

        // ASSERT
        // child hasn't got a property
        Assert.False(child.TryGetValue("data1", out var value));
    }

    #endregion IMoveItemProperty
}