namespace TreeStore.JsonFS.Test
{
    public class JObjectContentReaderTest
    {
        [Fact]
        public void Read_JObject_Json_String()
        {
            // ARRANGE
            var content = new JObject
            {
                ["value"] = new JValue(1),
                ["valueArray"] = new JArray(new JValue(1), new JValue(2)),
                ["emptyArray"] = new JArray(),
                ["objectArray"] = new JArray(new JObject(), new JObject()),
                ["object"] = new JObject(),
            };

            var reader = new JObjectContentReader(content);

            // ACT
            // the block count has no meaning in this context
            var result1 = reader.Read(1);
            var result2 = reader.Read(1);

            // ASSERT
            Assert.Equal(content.ToString(), result1.Cast<string>().Single());
            Assert.Empty(result2);
        }
    }
}