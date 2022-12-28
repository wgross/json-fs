namespace TreeStore.JsonFS.Test;

[Collection(nameof(File))]
public class JsonFsRootProviderTest
{
    private readonly string? directory;
    private readonly string jsonFilePath;

    public JsonFsRootProviderTest()
    {
        this.directory = Path.GetDirectoryName(typeof(JsonFsRootProviderTest).Assembly.Location);
        this.jsonFilePath = Path.Combine(directory!, "example-data.json");

        var data = new JObject
        {
            ["data1"] = "data1",
            ["child1"] = new JObject
            {
                ["data2"] = "data2",
            }
        };
        File.WriteAllText(this.jsonFilePath, data.ToString());
    }

    [Fact]
    public void Create_root_node_from_js_file()
    {
        // ARRANGE
        var rootProvider = JsonFsRootProvider.FromFile(path: this.jsonFilePath);

        // ACT
        var result = rootProvider.GetRootJObject();

        // ASSERT
        // an Object was created
        Assert.NotNull(result);
        Assert.True(result.TryGetValue("data1", out var value));
        Assert.Equal("data1", ((JValue)value).Value);
    }

    [Fact]
    public void Create_root_node_from_js_file_and_schema()
    {
        // ARRANGE
        var rootProvider = JsonFsRootProvider.FromFile(path: this.jsonFilePath);

        // ACT
        var result = rootProvider.GetRootJObject();

        // ASSERT
        // an Object was created
        Assert.NotNull(result);
        Assert.True(result.TryGetValue("data1", out var value));
        Assert.Equal("data1", ((JValue)value).Value);
    }

    [Fact]
    public async Task Reread_after_file_has_changed()
    {
        // ARRANGE
        var rootProvider = JsonFsRootProvider.FromFile(path: this.jsonFilePath);
        var firstRead = rootProvider.GetRootJObject();

        var data = new JObject
        {
            ["data1"] = "data1-changed",
            ["child1"] = new JObject
            {
                ["data2"] = "data2",
            }
        };
        await File.WriteAllTextAsync(this.jsonFilePath, data.ToString());

        await Task.Delay(TimeSpan.FromMilliseconds(100));

        // ACT
        var result = rootProvider.GetRootJObject();

        // ASSERT
        // an Object was created
        Assert.True(result.TryGetValue("data1", out var value));
        Assert.Equal("data1-changed", ((JValue)value).Value);
    }

    [Fact]
    public async Task Write_after_autosave_disposed()
    {
        // ARRANGE

        var data = new JObject
        {
            ["data1"] = "data1-changed",
            ["child1"] = new JObject
            {
                ["data2"] = "data2",
            }
        };
        await File.WriteAllTextAsync(this.jsonFilePath, data.ToString());

        var rootProvider = JsonFsRootProvider.FromFile(path: this.jsonFilePath);

        var rootNode = rootProvider.GetRootJObject();

        // ACT
        using (var autoSave = rootProvider.BeginModify())
        {
            rootNode.Add("data2", (JToken)"value");
        }

        // ASSERT
        // file has been modified
        var afterWrite = JObject.Parse(await File.ReadAllTextAsync(this.jsonFilePath));

        Assert.Equal("value", afterWrite["data2"]!.Value<string>());
    }

    [Fact]
    public async Task Write_after_last_autosave_disposed()
    {
        // ARRANGE

        var data = new JObject
        {
            ["data1"] = "data1-changed",
            ["child1"] = new JObject
            {
                ["data2"] = "data2",
            }
        };
        await File.WriteAllTextAsync(this.jsonFilePath, data.ToString());

        var rootProvider = JsonFsRootProvider.FromFile(path: this.jsonFilePath);

        var rootNode = rootProvider.GetRootJObject();

        // ACT
        using (var autoSave = rootProvider.BeginModify())
        {
            rootNode.Add("data3", (JToken)"value");

            using (var ausoSaveInner = rootProvider.BeginModify())
            {
                rootNode.Add("data4", (JToken)"value");
            }
        }

        // ASSERT
        // file has been modified
        var afterWrite = JObject.Parse(await File.ReadAllTextAsync(this.jsonFilePath));

        Assert.Equal("value", afterWrite["data3"]!.Value<string>());
        Assert.Equal("value", afterWrite["data4"]!.Value<string>());
    }

    [Fact]
    public async Task Verify_after_last_autosave_disposed()
    {
        // ARRANGE

        var jsonSchemaPath = Path.Combine(directory!, "example-schema.json");

        var schema = await JsonSchema.FromJsonAsync(@"{
          'type': 'object',
          'properties': {
            'data1': {'type':'string'},
            'child1': {'type':'object'},
          },
          'required':['data1']
        }");

        await File.WriteAllTextAsync(jsonSchemaPath, schema.ToJson());

        var data = new JObject
        {
            ["data1"] = "data1-changed",
            ["child1"] = new JObject
            {
                ["data2"] = "data2",
            }
        };
        await File.WriteAllTextAsync(this.jsonFilePath, data.ToString());

        var rootProvider = JsonFsRootProvider.FromFileAndSchema(path: this.jsonFilePath, schemaPath: jsonSchemaPath);

        var rootNode = rootProvider.GetRootJObject();

        // ACT
        InvalidOperationException? result = null;

        result = Assert.Throws<InvalidOperationException>(() =>
        {
            using (var autoSave = rootProvider.BeginModify())
            {
                rootNode.Add("data3", (JToken)"value");

                using (var autoSaveInner = rootProvider.BeginModify())
                {
                    // violate the the schena rules
                    rootNode.Remove("data1");
                }
            }
        });

        // ASSERT
        Assert.Equal("PropertyRequired: #/data1", result.Message);

        // file hasn't been modified
        var afterWrite = JObject.Parse(await File.ReadAllTextAsync(this.jsonFilePath));

        Assert.False(afterWrite.TryGetValue("data3", out var _));
        Assert.True(afterWrite.TryGetValue("data1", out var _));
    }
}