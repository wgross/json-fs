namespace TreeStore.JsonFS.Test;

[Collection(nameof(PowerShell))]
public class NavigationCmdletProvider : PowerShellTestBase
{
    #region Move-Item -Path -Destination

    [Fact]
    public void PowerShell_moves_node_to_child()
    {
        // ARRANGE
        var root = this.ArrangeFileSystem(new JObject
        {
            ["child1"] = new JObject
            {
                ["property"] = new JValue("text")
            },
            ["child2"] = new JObject(),
        });
        var child1 = root["child1"];

        // ACT
        var result = this.PowerShell.AddCommand("Move-Item")
            .AddParameter("Path", @"test:\child1")
            .AddParameter("Destination", @"test:\child2")
            .AddStatement()
            .AddCommand("Get-Item")
            .AddParameter("Path", @"test:\child2\child1")
            .Invoke()
            .Single();

        // ASSERT
        Assert.False(this.PowerShell.HadErrors);

        this.AssertJsonFileContent(r =>
        {
            Assert.False(r.TryGetJObject("child1", out var _));
            Assert.True(r.TryGetJObject("child2", out var c2));
            Assert.True(c2!.TryGetJObject("child1", out var c1));
            Assert.Equal("text", c1!.Value<string>("property"));
        });
    }

    [Fact]
    public async Task PowerShell_moves_node_to_child_and_validates()
    {
        // ARRANGE
        var jsonSchema = await JsonSchema.FromJsonAsync("""
        {
            "type":"object",
            "properties":{
                "child2":{
                    "type":"object",
                    "properties":{
                        "child1":{
                            "type":"object"
                        }
                    },
                    "required":["child1"] // child1 is expected here
                },
            },
            "additionalPropperties":false // child1 must not be here
        }
        """);

        var root = this.ArrangeFileSystem(new JObject
        {
            ["child1"] = new JObject
            {
                ["property"] = new JValue("text")
            },
            ["child2"] = new JObject(),
        }, jsonSchema);

        var child1 = root["child1"];

        // ACT
        var result = this.PowerShell.AddCommand("Move-Item")
            .AddParameter("Path", @"test:\child1")
            .AddParameter("Destination", @"test:\child2")
            .AddStatement()
            .AddCommand("Get-Item")
            .AddParameter("Path", @"test:\child2\child1")
            .Invoke()
            .Single();

        // ASSERT
        Assert.False(this.PowerShell.HadErrors);

        this.AssertJsonFileContent(r =>
        {
            Assert.False(r.TryGetJObject("child1", out var _));
            Assert.True(r.TryGetJObject("child2", out var c2));
            Assert.True(c2!.TryGetJObject("child1", out var c1));
            Assert.Equal("text", c1!.Value<string>("property"));
        });
    }

    [Fact]
    public async Task PowerShell_moves_node_to_child_and_invalidates()
    {
        // ARRANGE
        var jsonSchema = await JsonSchema.FromJsonAsync("""
        {
            "type":"object",
            "properties":{
                "child1": {
                    "type":"object"
                },
                "child2":{
                    "type":"object"
                },
            },
            "required":["child1"] // child1 must not be removed
        }
        """);

        var root = this.ArrangeFileSystem(new JObject
        {
            ["child1"] = new JObject
            {
                ["property"] = new JValue("text")
            },
            ["child2"] = new JObject(),
        }, jsonSchema);

        var originalRoot = root.ToString();

        var child1 = root["child1"];

        // ACT
        var result = this.PowerShell.AddCommand("Move-Item")
            .AddParameter("Path", @"test:\child1")
            .AddParameter("Destination", @"test:\child2")
            .AddStatement()
            .AddCommand("Get-Item")
            .AddParameter("Path", @"test:\child1")
            .Invoke()
            .ToArray();

        // ASSERT
        Assert.True(this.PowerShell.HadErrors);
        this.AssertSingleError<InvalidOperationException>(e => Assert.Equal("PropertyRequired: #/child1", e.Message));

        // child1 is still there
        Assert.Single(result);

        // file remains unchanged
        this.AssertJsonFileContent(r => Assert.Equal(originalRoot, r.ToString()));
    }

    [Fact]
    public void Powershell_moves_child_item_to_second_jsonfs()
    {
        // ARRANGE
        var secondfile = $"{Guid.NewGuid()}.json";

        var childA_1 = new JObject()
        {
            ["property"] = new JValue(1),
            ["grandchild"] = new JObject()
            {
                ["property2"] = new JValue(2)
            }
        };

        var rootA = this.ArrangeFileSystem(new JObject
        {
            ["child1"] = childA_1,
            ["child2"] = new JObject()
        });

        var rootB = this.ArrangeFileSystem("test-2", secondfile, new JObject
        {
        });

        // ACT
        // copy child1 under child2
        var _ = this.PowerShell.AddCommand("Move-Item")
            .AddParameter("Path", @"test:\child1")
            .AddParameter("Destination", @"test-2:\")
            .Invoke()
            .ToArray();

        // ASSERT
        Assert.False(this.PowerShell.HadErrors);

        this.AssertJsonFileContent(r =>
        {
            // child1 was remove from first file system
            Assert.False(r.TryGetJObject("child1", out var _));
        });

        this.AssertJsonFileContent(secondfile, r =>
        {
            // new node has been created
            Assert.NotNull(r.ChildObject("child1"));

            // copy is shallow and contains the property but not the child node
            Assert.True(r.ChildObject("child1").TryGetValue("property", out var _));
            Assert.True(r.ChildObject("child1").TryGetValue("grandchild", out var _));
            Assert.True(r.ChildObject("child1").ChildObject("grandchild").TryGetValue("property2", out var property2));
            Assert.Equal(2, property2!.Value<int>());
        });
    }

    [Fact]
    public void PowerShell_moves_node_to_child_with_new_name()
    {
        // ARRANGE
        var root = this.ArrangeFileSystem(new JObject
        {
            ["child1"] = new JObject()
            {
                ["property"] = new JValue("text")
            },
            ["child2"] = new JObject()
        });

        var child1 = root["child1"];

        // ACT
        var result = this.PowerShell.AddCommand("Move-Item")
            .AddParameter("Path", @"test:\child1")
            .AddParameter("Destination", @"test:\child2\new-name")
            .AddStatement()
            .AddCommand("Get-Item")
            .AddParameter("Path", @"test:\child2\new-name")
            .Invoke()
            .Single();

        // ASSERT
        Assert.False(this.PowerShell.HadErrors);
        Assert.Equal("text", result.Property<string>("property"));

        this.AssertJsonFileContent(r =>
        {
            Assert.False(r.TryGetJObject("child1", out var _));
            Assert.True(r.TryGetJObject("child2", out var c2));
            Assert.True(c2!.TryGetJObject("new-name", out var nn));
            Assert.Equal("text", nn!.Value<string>("property"));
        });
    }

    [Fact]
    public void Powershell_moves_child_item_with_new_name_to_second_jsonfs()
    {
        // ARRANGE
        var secondfile = $"{Guid.NewGuid()}.json";

        var childA_1 = new JObject()
        {
            ["property"] = new JValue(1),
            ["grandchild"] = new JObject()
            {
                ["property2"] = new JValue(2)
            }
        };

        var rootA = this.ArrangeFileSystem(new JObject
        {
            ["child1"] = childA_1,
            ["child2"] = new JObject()
        });

        var rootB = this.ArrangeFileSystem("test-2", secondfile, new JObject
        {
        });

        // ACT
        // copy child1 under child2
        var _ = this.PowerShell.AddCommand("Move-Item")
            .AddParameter("Path", @"test:\child1")
            .AddParameter("Destination", @"test-2:\new")
            .Invoke()
            .ToArray();

        // ASSERT
        Assert.False(this.PowerShell.HadErrors);

        this.AssertJsonFileContent(r =>
        {
            // child1 was remove from first file system
            Assert.False(r.TryGetJObject("child1", out var _));
        });

        this.AssertJsonFileContent(secondfile, r =>
        {
            // new node has been created
            Assert.NotNull(r.ChildObject("new"));

            // copy is shallow and contains the property but not the child node
            Assert.True(r.ChildObject("new").TryGetValue("property", out var _));
            Assert.True(r.ChildObject("new").TryGetValue("grandchild", out var _));
            Assert.True(r.ChildObject("new").ChildObject("grandchild").TryGetValue("property2", out var property2));
            Assert.Equal(2, property2!.Value<int>());
        });
    }

    //[Fact]
    //public void PowerShell_moves_node_to_child_with_new_name()
    //{
    //    // ARRANGE
    //    var root = this.ArrangeFileSystem(new JObject
    //    {
    //        ["child1"] = new JObject(),
    //        ["child2"] = new JObject(),
    //        ["property"] = "text"
    //    });
    //    var child1 = root["child1"];

    //    // ACT
    //    this.PowerShell.AddCommand("Move-Item")
    //        .AddParameter("Path", @"test:\child1")
    //        .AddParameter("Destination", @"test:\child2\new-name")
    //        .Invoke()
    //        .ToArray();

    //    // ASSERT
    //    Assert.False(this.PowerShell.HadErrors);
    //    Assert.False(root.TryGetValue("child1", out var _));
    //    Assert.Same(child1, ((JObject)root.Property("child2").Value).Property["new-name"]);
    //}

    //[Fact]
    //public void PowerShell_moves_node_to_grandchild_with_new_name()
    //{
    //    // ARRANGE
    //    var root = this.ArrangeFileSystem(new JObject
    //    {
    //        ["child1"] = new JObject(),
    //        ["child2"] = new JObject(),
    //        ["property"] = "text"
    //    });
    //    var child1 = root["child1"];

    //    // ACT
    //    this.PowerShell.AddCommand("Move-Item")
    //        .AddParameter("Path", @"test:\child1")
    //        .AddParameter("Destination", @"test:\child2\child3\new-name")
    //        .Invoke()
    //        .ToArray();

    //    // ASSERT
    //    Assert.False(this.PowerShell.HadErrors);
    //    Assert.False(root.TryGetValue("child1", out var _));
    //    Assert.Same(child1, ((JObject)((JObject)root["child2"]!)["child3"])["new-name"]);
    //}

    #endregion Move-Item -Path -Destination
}