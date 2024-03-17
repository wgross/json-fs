using Moq;
using System.Collections.Generic;
using TreeStore.Core.Providers;

namespace TreeStore.JsonFS.Test;

public class JArrayContentWriterTest : IDisposable
{
    private readonly MockRepository mocks = new(MockBehavior.Strict);
    private readonly Mock<ICmdletProvider> providerMock;
    private readonly Mock<IDisposable> disopsableMock;

    public JArrayContentWriterTest()
    {
        this.providerMock = this.mocks.Create<ICmdletProvider>();
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

    [Fact]
    public void Write_JArray_Json_String()
    {
        // ARRANGE

        var content = new JArray(new JObject());

        var newContent = new JArray(
            new JObject
            {
                ["value"] = new JValue(1),
                ["valueArray"] = new JArray(new JValue(1), new JValue(2)),
            },
            new JObject
            {
                ["value"] = new JValue(1),
                ["valueArray"] = new JArray(new JValue(1), new JValue(2)),
            });

        this.ArrangeBeginModification();

        using (var writer = new JArrayContentWriter(this.providerMock.Object, new JArrayAdapter(content)))
        {
            // ACT & ASSERT
            // the block count has no meaning in this context
            Assert.Equal(newContent[0].ToString(), writer.Write(new List<string>() { newContent[0].ToString() })[0]);
            Assert.Equal(newContent[1].ToString(), writer.Write(new List<string>() { newContent[1].ToString() })[0]);
        }
        Assert.Equal(newContent.ToString(), content.ToString());
    }
}