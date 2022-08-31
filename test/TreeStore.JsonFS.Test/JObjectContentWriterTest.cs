using Moq;
using System.Collections.Generic;
using TreeStore.Core.Providers;

namespace TreeStore.JsonFS.Test
{
    public class JObjectContentWriterTest : IDisposable
    {
        private readonly MockRepository mocks = new(MockBehavior.Strict);
        private readonly Mock<ICmdletProvider> providerMock;
        private readonly Mock<IDisposable> disopsableMock;

        public JObjectContentWriterTest()
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
        public void Write_JObject_json()
        {
            // ARRANGE
            var content = new JObject
            {
                ["property"] = 1
            };

            var newContent = new JObject
            {
                ["value"] = new JValue(1),
                ["valueArray"] = new JArray(new JValue(1), new JValue(2)),
                ["emptyArray"] = new JArray(),
                ["objectArray"] = new JArray(new JObject(), new JObject()),
                ["object"] = new JObject(),
            };

            this.ArrangeBeginModification();

            var writer = new JObjectContentWriter(this.providerMock.Object, new JObjectAdapter(content));

            // ACT
            // the block count has no meaning in this context
            var result = writer.Write(new List<string>() { newContent.ToString() });
            writer.Dispose();

            // ASSERT
            Assert.Equal(content.ToString(), newContent.ToString());
            Assert.Equal(result[0], new List<string>() { newContent.ToString() }[0]);
        }
    }
}