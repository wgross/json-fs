namespace TreeStore.JsonFS.Test;

public class JArrayContentReaderTest
{
    [Fact]
    public void Read_JArray_Json_String()
    {
        // ARRANGE
        var content = new JArray(
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

        var reader = new JArrayContentReader(content);

        // ACT
        // the block count has no meaning in this context
        var result1 = reader.Read(1);
        var result2 = reader.Read(1);
        var result3 = reader.Read(1);

        // ASSERT
        Assert.Equal(content[0].ToString(), result1.Cast<string>().Single());
        Assert.Equal(content[1].ToString(), result2.Cast<string>().Single());
        Assert.Empty(result3);
    }
}