using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Management.Automation;
using TreeStore.Core;
using TreeStore.Core.Capabilities;
using TreeStore.Core.Nodes;
using Xunit;

namespace TreeStore.JsonFS.Test
{
    public class JsonDocumentAdapterTest
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
            Assert.Equal(1, result!.Property<long>("value"));
            Assert.Equal(new object[] { 1L, 2L }, result!.Property<object[]>("array"));
            Assert.Null(result!.Properties.FirstOrDefault(p => p.Name == "object"));
        }

        [Fact]
        public void GetItemParameters_is_null()
        {
            // ARRANGE
            //var data = new JsonNode();
            //var child = new JsonDocumentAdapter();
            var node = new JObjectAdapter(new JObject()
            {
                ["value"] = new JValue(1),
                ["array"] = new JArray(new JValue(1), new JValue(2)),
                ["object"] = new JObject(),
            });

            // ACT
            var result = ((IGetItem)node).GetItemParameters();

            // ASSERT
            Assert.IsType<RuntimeDefinedParameterDictionary>(result);
        }

        #endregion IGetItem

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
            Assert.NotNull(result);
            Assert.Equal("container1", result!.Name);
            Assert.True(result is ContainerNode);
            Assert.True(underlying.TryGetValue("container1", out var added));

            Assert.NotNull(added);
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
            var value = new JObject();
            var result = ((INewChildItem)node).NewChildItem("container1", "itemTypeValue", value);

            // ASSERT
            Assert.NotNull(result);
            Assert.Equal("container1", result!.Name);
            Assert.True(result is ContainerNode);
            Assert.True(underlying.TryGetValue("container1", out var added));

            Assert.Same(value, added);
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
            var value = new JObject { ["property"] = new JValue("text") }.ToString();

            var result = ((INewChildItem)node).NewChildItem("container1", "itemTypeValue", value);

            // ASSERT
            Assert.NotNull(result);
            Assert.Equal("container1", result!.Name);
            Assert.True(result is ContainerNode);
            Assert.True(underlying.TryGetValue("container1", out var added));

            Assert.Equal("text", added["property"]!.Value<string>());
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
            ((IRenameChildItem)node).RenameChildItem("container1", "newname");

            // ASSERT
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
            ((IRenameChildItem)node).RenameChildItem("missing", "newname");

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
            var result = ((ICopyChildItem)dst).CopyChildItem(
                nodeToCopy: rootNode.GetChildItems().Single(n => n.Name == "child1"),
                destination: Array.Empty<string>());

            // ASSERT
            Assert.IsType<ContainerNode>(result);
            Assert.NotNull(((JObject)root.Property("child2")!.Value).Property("child1"));
            Assert.NotSame(nodeToCopy, ((JObject)root.Property("child2")!.Value).Property("child1"));
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
            // copy child1 under child2 as 'child1'
            var result = ((ICopyChildItem)dst).CopyChildItem(
                nodeToCopy: rootNode.GetChildItems().Single(n => n.Name == "child1"),
                destination: new string[] { "newname" });

            // ASSERT
            Assert.IsType<ContainerNode>(result);
            Assert.NotNull(((JObject)root.Property("child2")!.Value).Property("newname"));
            Assert.NotSame(nodeToCopy, ((JObject)root.Property("child2")!.Value).Property("child1"));
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
            // copy child1 under child2 as 'child1'
            var result = ((ICopyChildItem)dst).CopyChildItem(
                nodeToCopy: rootNode.GetChildItems().Single(n => n.Name == "child1"),
                destination: new string[] { "newparent", "newname" });

            // ASSERT
            Assert.IsType<ContainerNode>(result);
            Assert.NotNull(((JObject)((JObject)root.Property("child2")!.Value).Property("newparent")!.Value).Property("newname"));
            Assert.NotSame(nodeToCopy, ((JObject)root.Property("child2")!.Value).Property("child1"));
        }

        //[Fact]
        //public void CopyChildItem_copies_underlying_shallow_with_new_parent_and_name()
        //{
        //    // ARRANGE
        //    var root = new JObject
        //    {
        //        ["child1"] = new JObject
        //        {
        //            ["data"] = 1,
        //            ["grandchild"] = new JObject()
        //        },
        //        ["child2"] = new JObject()
        //    };
        //    var nodeToCopy = root.AsDictionary("child1");
        //    var rootNode = new RootNode(ArrangeContainerAdapter(root));
        //    var dst = ArrangeContainerAdapter(root.AsDictionary("child2"));

        //    // ACT
        //    // copy child1 under child2 as 'child1'
        //    var result = ((ICopyChildItem)dst).CopyChildItem(rootNode.GetChildItems().Single(n => n.Name == "child1"), destination: new string[] { "newparent", "newname" });

        //    // ASSERT
        //    Assert.NotNull(root.AsDictionary("child2").AsDictionary("newparent").AsDictionary("newname"));
        //    Assert.NotSame(nodeToCopy, root.AsDictionary("child2").AsDictionary("newparent").AsDictionary("newname"));
        //    Assert.NotSame(nodeToCopy, root.AsDictionary("child2").AsDictionary("newparent").AsDictionary("newname"));
        //    Assert.Same(nodeToCopy, root.AsDictionary("child1"));
        //    Assert.Equal(1, root.AsDictionary("child2").AsDictionary("newparent").AsDictionary("newname")["data"]);
        //    Assert.False(root.AsDictionary("child2").AsDictionary("newparent").AsDictionary("newname").TryGetValue("grandchild", out var _));
        //}

        #endregion ICopyChildItem
    }
}