using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Collections.Generic;
using TreeStore.Core.Capabilities;
using TreeStore.Core.Providers;

namespace TreeStore.JsonFS.Test
{
    public class JArrayAdapterTest : IDisposable
    {
        private readonly MockRepository mocks = new MockRepository(MockBehavior.Strict);
        private readonly Mock<ICmdletProvider> providerMock;
        private readonly Mock<IDisposable> disopsableMock;

        public JArrayAdapterTest()
        {
            this.providerMock = this.mocks.Create<ICmdletProvider>();
            this.disopsableMock = this.mocks.Create<IDisposable>();
        }

        public JArray ArrangeDefaultArray(params Action<JArray>[] setup)
        {
            var tmp = new JArray(new JObject(), new JArray(new JObject()));

            foreach (var s in setup)
                s(tmp);

            return tmp;
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
        public void GetItem_creates_PSObject_of_JArray()
        {
            // ARRANGE
            var node = new JArrayAdapter(this.ArrangeDefaultArray());

            // ACT
            var result = ((IGetItem)node).GetItem(provider: this.providerMock.Object);

            // ASSERT
            Assert.IsType<JsonFsItem>(result.BaseObject);
            Assert.Equal("", result.Property<string>("Name"));
            Assert.Equal(Array.Empty<string>(), result.Property<string[]>("PropertyNames"));
        }

        [Fact]
        public void GetItem_creates_PSObject_of_JObject_in_JArray()
        {
            // ARRANGE
            var parent = this.ArrangeDefaultArray();

            var node = new JObjectAdapter((JObject)parent[0]!);

            // ACT
            var result = ((IGetItem)node).GetItem(provider: this.providerMock.Object);

            // ASSERT
            Assert.IsType<JsonFsItem>(result.BaseObject);
            Assert.Equal("0", result.Property<string>("Name"));
            Assert.Equal(Array.Empty<string>(), result.Property<string[]>("PropertyNames"));
        }

        [Fact]
        public void GetItem_creates_PSObject_of_JArray_in_JArray()
        {
            // ARRANGE
            var parent = this.ArrangeDefaultArray();

            var node = new JArrayAdapter((JArray)parent[1]!);

            // ACT
            var result = ((IGetItem)node).GetItem(provider: this.providerMock.Object);

            // ASSERT
            Assert.IsType<JsonFsItem>(result.BaseObject);
            Assert.Equal("1", result.Property<string>("Name"));
            Assert.Equal(Array.Empty<string>(), result.Property<string[]>("PropertyNames"));
        }

        #endregion IGetItem

        #region ISetItem

        [Fact]
        public void SetItem_replaces_from_JArray()
        {
            // ARRANGE
            this.ArrangeBeginModification();

            var node = new JArrayAdapter(this.ArrangeDefaultArray());

            var newData = new JArray(
                new JObject(new JProperty("value", 1)),
                new JObject(new JProperty("value", 2)));

            // ACT
            // override node with new data
            node.GetRequiredService<ISetItem>().SetItem(this.providerMock.Object, newData);

            // ASSERT
            // array items are overwritten with new values
            var childItems = node.GetRequiredService<IGetChildItem>().GetChildItems(this.providerMock.Object).ToArray();

            Assert.Equal(2, childItems.Length);

            var psobject = childItems[0].GetItem();

            Assert.Equal(1, psobject.Property<int>("value"));

            psobject = childItems[1].GetItem();

            Assert.Equal(2, psobject.Property<int>("value"));
        }

        [Fact]
        public void SetItem_replaces_from_string()
        {
            // ARRANGE
            this.ArrangeBeginModification();

            var node = new JArrayAdapter(this.ArrangeDefaultArray());

            var newData = new JArray(
                new JObject(new JProperty("value", 1)),
                new JObject(new JProperty("value", 2)));

            // ACT
            // override node with new data
            node.GetRequiredService<ISetItem>().SetItem(this.providerMock.Object, newData.ToString());

            // ASSERT
            // array items are overwritten with new values
            var childItems = node.GetRequiredService<IGetChildItem>().GetChildItems(this.providerMock.Object).ToArray();

            Assert.Equal(2, childItems.Length);

            var psobject = childItems[0].GetItem();

            Assert.Equal(1, psobject.Property<long>("value"));

            psobject = childItems[1].GetItem();

            Assert.Equal(2, psobject.Property<long>("value"));
        }

        #endregion ISetItem

        #region IGetChildItem

        [Fact]
        public void HasChildItems_is_true_for_JArray()
        {
            // ARRANGE
            var node = new JArrayAdapter(this.ArrangeDefaultArray());

            // ACT
            var result = ((IGetChildItem)node).HasChildItems(this.providerMock.Object);

            // ASSERT
            // the node has child nodes.
            Assert.True(result);
        }

        [Fact]
        public void GetChildItem_creates_PSObject()
        {
            // ARRANGE
            var node = new JArrayAdapter(this.ArrangeDefaultArray());

            // ACT
            var result = ((IGetChildItem)node).GetChildItems(this.providerMock.Object).ToArray();

            // ASSERT
            Assert.Equal(2, result.Length);

            var pso = result.ElementAt(0).GetItem();
            Assert.Equal("0", pso.Property<string>("PSChildName"));

            pso = result.ElementAt(1).GetItem();
            Assert.Equal("1", pso.Property<string>("PSChildName"));
        }

        [Fact]
        public void GetChildItems_from_empty_returns_empty()
        {
            // ARRANGE
            var node = new JArrayAdapter(new JArray());

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
        public void RemoveChildItem_removes_empty_JObject_item(bool recurse)
        {
            // ARRANGE
            this.ArrangeBeginModification();

            var node = new JArrayAdapter(this.ArrangeDefaultArray());

            // ACT
            node.GetRequiredService<IRemoveChildItem>().RemoveChildItem(provider: this.providerMock.Object, "0", recurse);

            // ASSERT
            // the child node is gone
            var childItems = node.GetRequiredService<IGetChildItem>().GetChildItems(this.providerMock.Object);

            Assert.Single(childItems);
        }

        [Fact]
        public void RemoveChildItem_removes_non_empty_JObject_with_recurse()
        {
            // ARRANGE
            this.ArrangeBeginModification();

            var node = new JArrayAdapter(this.ArrangeDefaultArray(ja => ((JObject)ja[0])["child"] = new JObject()));

            // ACT
            node.GetRequiredService<IRemoveChildItem>().RemoveChildItem(provider: this.providerMock.Object, "0", recurse: true);

            // ASSERT
            // the child node is gone
            var childItems = node.GetRequiredService<IGetChildItem>().GetChildItems(this.providerMock.Object);

            Assert.Single(childItems);
        }

        [Fact]
        public void RemoveChildItem_ignores_removing_non_empty_JObject()
        {
            // ARRANGE
            var node = new JArrayAdapter(this.ArrangeDefaultArray(ja => ((JObject)ja[0])["child"] = new JObject()));

            // ACT
            node.GetRequiredService<IRemoveChildItem>().RemoveChildItem(provider: this.providerMock.Object, "0", recurse: false);

            // ASSERT
            // the child item wasn't deleted.
            var childItems = node.GetRequiredService<IGetChildItem>().GetChildItems(this.providerMock.Object).ToArray();

            Assert.Equal(2, childItems.Length);
        }

        #endregion IRemoveChildItem

        #region INewChildItem

        [Fact]
        public void NewChildItem_provides_Parameters()
        {
            // ARRANGE
            var node = new JArrayAdapter(this.ArrangeDefaultArray());

            // ACT
            var result = node.GetRequiredService<INewChildItem>().NewChildItemParameters(null, null, null);

            // ASSERT
            // the node was created as last node of the array
            Assert.IsType<NewChildItemParameters>(result);
        }

        [Fact]
        public void NewChildItem_appends_JObject_item()
        {
            // ARRANGE
            this.ArrangeBeginModification();

            var node = new JArrayAdapter(this.ArrangeDefaultArray());

            // ACT
            var result = node.GetRequiredService<INewChildItem>().NewChildItem(this.providerMock.Object, null, null, null);

            // ASSERT
            // the node was created as last node of the array
            Assert.True(result.Created);
            Assert.Equal("2", result!.Name);

            var childItems = node.GetRequiredService<IGetChildItem>().GetChildItems(this.providerMock.Object).ToArray();

            Assert.Equal(3, childItems.Length);
        }

        [Fact]
        public void NewChildItem_appends_JObject_item_from_Json()
        {
            // ARRANGE
            this.ArrangeBeginModification();

            var node = new JArrayAdapter(this.ArrangeDefaultArray());

            // ACT
            var value = new JObject { ["property2"] = new JValue("text") }.ToString();
            var result = node.GetRequiredService<INewChildItem>().NewChildItem(this.providerMock.Object, null, null, newItemValue: value);

            // ASSERT
            // the node was created as last node of the array
            Assert.True(result.Created);
            Assert.Equal("2", result!.Name);

            var childItems = node.GetRequiredService<IGetChildItem>().GetChildItems(this.providerMock.Object).ToArray();

            Assert.Equal(3, childItems.Length);

            var childItem = childItems[2].GetItem();

            Assert.Equal("text", childItem.Property<string>("property2"));
        }

        [Fact]
        public void NewChildItem_inserts_JObject_item_at_position()
        {
            // ARRANGE
            this.ArrangeBeginModification();

            var parameters = new NewChildItemParameters { Insert = new SwitchParameter(true) };

            this.providerMock
                .Setup(p => p.DynamicParameters)
                .Returns(parameters);

            var node = new JArrayAdapter(this.ArrangeDefaultArray());

            // ACT
            var result = node.GetRequiredService<INewChildItem>().NewChildItem(this.providerMock.Object, "0", null, null);

            // ASSERT
            // the node was created as last node of the array
            Assert.True(result.Created);
            Assert.Equal("0", result!.Name);

            var childItems = node.GetRequiredService<IGetChildItem>().GetChildItems(this.providerMock.Object).ToArray();

            Assert.Equal(3, childItems.Length);
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

        #endregion INewChildItem

        #region IGetItemContent

        [Fact]
        public void GetItemContent_returens_JArray_items()
        {
            // ARRANGE
            var content = new JArray(
                new JObject()
                {
                    ["value1"] = 1
                },
                new JObject()
                {
                    ["value2"] = 2
                });

            var adapter = new JArrayAdapter(content);

            // ACT & ASSERT
            // reads single array item from the reader
            using var reader = adapter.GetRequiredService<IGetItemContent>().GetItemContentReader(this.providerMock.Object);

            Assert.Equal(content[0].ToString(), reader!.Read(1)[0] as string);
            Assert.Equal(content[1].ToString(), reader!.Read(1)[0] as string);
        }

        #endregion IGetItemContent

        #region ISetItemContent

        [Fact]
        public void SetItemContent_returns_JArray_items()
        {
            // ARRANGE
            var json = new JArray();

            var content = new JArray(
                new JObject()
                {
                    ["value1"] = 1
                },
                new JObject()
                {
                    ["value2"] = 2
                });

            var adapter = new JArrayAdapter(json);

            this.ArrangeBeginModification();

            // ACT & ASSERT
            // write array item by item
            using (var writer = adapter.GetRequiredService<ISetChildItemContent>().GetChildItemContentWriter(this.providerMock.Object, "childName"))
            {
                writer!.Write(new List<string> { content[0].ToString() });
                writer!.Write(new List<string> { content[1].ToString() });
            }

            // ASSERT
            using var reader = adapter.GetRequiredService<IGetItemContent>().GetItemContentReader(this.providerMock.Object);

            Assert.Equal(content[0].ToString(), reader!.Read(1)[0] as string);
            Assert.Equal(content[1].ToString(), reader!.Read(1)[0] as string);
        }

        #endregion ISetItemContent
    }
}