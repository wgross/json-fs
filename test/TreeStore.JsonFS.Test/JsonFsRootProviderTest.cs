namespace TreeStore.JsonFS.Test;

[Collection(nameof(File))]
public class JsonFsRootProviderTest
{
    private readonly string filePath;

    public JsonFsRootProviderTest()
    {
        var directory = Path.GetDirectoryName(typeof(JsonFsRootProviderTest).Assembly.Location);
        this.filePath = Path.Combine(directory!, "example-data.json");

        var data = new JObject
        {
            ["data1"] = "data1",
            ["child1"] = new JObject
            {
                ["data2"] = "data2",
            }
        };
        File.WriteAllText(this.filePath, data.ToString());
    }

    [Fact]
    public void Create_root_node_from_js_file()
    {
        // ARRANGE
        var rootProvider = JsonFsRootProvider.FromFile(path: this.filePath);

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
        var rootProvider = JsonFsRootProvider.FromFile(path: this.filePath);
        var firstRead = rootProvider.GetRootJObject();

        var data = new JObject
        {
            ["data1"] = "data1-changed",
            ["child1"] = new JObject
            {
                ["data2"] = "data2",
            }
        };
        await File.WriteAllTextAsync(this.filePath, data.ToString());

        await Task.Delay(TimeSpan.FromMilliseconds(100));

        // ACT
        var result = rootProvider.GetRootJObject();

        // ASSERT
        // an Object was created
        Assert.True(result.TryGetValue("data1", out var value));
        Assert.Equal("data1-changed", ((JValue)value).Value);
    }

    //[Fact]
    //public async Task Lock()
    //{
    //    // ARRANGE
    //    var rootProvider = JsonFsRootProvider.FromFile(path: this.filePath);
    //    var firstRead = rootProvider.GetRootJObject();

    //    var data = new JObject
    //    {
    //        ["data1"] = "data1-changed",
    //        ["child1"] = new JObject
    //        {
    //            ["data2"] = "data2",
    //        }
    //    };
    //    await File.WriteAllTextAsync(this.filePath, data.ToString());

    //    await Task.Delay(TimeSpan.FromMilliseconds(100));

    //    var result = rootProvider.GetRootJObject();

    //    // ACT
    //    using var fileLock = rootProvider.Lock();

    //    // ASSERT
    //    // file is locked
    //    Assert.Throws<IOException>(() => File.OpenRead(this.filePath));
    //    Assert.Throws<IOException>(() => File.OpenWrite(this.filePath));
    //}

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
        await File.WriteAllTextAsync(this.filePath, data.ToString());

        var rootProvider = JsonFsRootProvider.FromFile(path: this.filePath);

        var rootNode = rootProvider.GetRootJObject();

        // ACT
        using (var autoSave = rootProvider.BeginModify())
        {
            rootNode.Add("data2", (JToken)"value");
        }

        // ASSERT
        // file has been modified
        var afterWrite = JObject.Parse(await File.ReadAllTextAsync(this.filePath));

        Assert.Equal("value", afterWrite["data2"].Value<string>());
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
        await File.WriteAllTextAsync(this.filePath, data.ToString());

        var rootProvider = JsonFsRootProvider.FromFile(path: this.filePath);

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
        var afterWrite = JObject.Parse(await File.ReadAllTextAsync(this.filePath));

        Assert.Equal("value", afterWrite["data3"].Value<string>());
        Assert.Equal("value", afterWrite["data4"].Value<string>());
    }

    //[Fact]
    //public async Task Lock_Dispose_unlocks_file()
    //{
    //    // ARRANGE
    //    var rootProvider = JsonFsRootProvider.FromFile(path: this.filePath);
    //    var firstRead = rootProvider.GetRootJObject();

    //    var data = new JObject
    //    {
    //        ["data1"] = "data1-changed",
    //        ["child1"] = new JObject
    //        {
    //            ["data2"] = "data2",
    //        }
    //    };
    //    await File.WriteAllTextAsync(this.filePath, data.ToString());

    //    await Task.Delay(TimeSpan.FromMilliseconds(100));

    //    var result = rootProvider.GetRootJObject();

    //    // ACT
    //    using (var fileLock = rootProvider.Lock())
    //    {
    //        // ASSERT
    //        // file is locked
    //        Assert.Throws<IOException>(() => File.OpenRead(this.filePath));
    //    }

    //    // ASSERT
    //    // file is not locked anymore
    //    using var file = File.OpenRead(this.filePath);
    //}
}