using System.Management.Automation;

namespace TreeStore.JsonFS.Test;

[Collection(nameof(PowerShell))]
public class ContainerCmdletProviderTest : PowerShellTestBase
{
    #region Get-ChildItem -Path -Recurse

    [Fact]
    public void Powershell_reads_roots_childnodes()
    {
        // ARRANGE
        var root = this.ArrangeFileSystem(DefaultRoot());

        // ACT
        var result = this.PowerShell.AddCommand("Get-ChildItem")
            .AddParameter("Path", @"test:\")
            .Invoke()
            .ToArray();

        // ASSERT
        Assert.False(this.PowerShell.HadErrors);
        Assert.Single(result);

        var psobject = result[0];

        Assert.Equal("object", psobject.Property<string>("PSChildName"));
        Assert.True(psobject.Property<bool>("PSIsContainer"));
        Assert.Equal("test", psobject.Property<PSDriveInfo>("PSDrive").Name);
        Assert.Equal("JsonFS", psobject.Property<ProviderInfo>("PSProvider").Name);
        Assert.Equal(@"JsonFS\JsonFS::test:\object", psobject.Property<string>("PSPath"));
        Assert.Equal(@"JsonFS\JsonFS::test:", psobject.Property<string>("PSParentPath"));
    }

    [Fact]
    public void Powershell_reads_roots_childnodes_names()
    {
        // ARRANGE
        var root = this.ArrangeFileSystem(DefaultRoot());

        // ACT
        var result = this.PowerShell.AddCommand("Get-ChildItem")
            .AddParameter("Path", @"test:\")
            .AddParameter("Name")
            .Invoke()
            .ToArray();

        // ASSERT
        Assert.False(this.PowerShell.HadErrors);
        Assert.Single(result);

        var psobject = result[0];

        Assert.IsType<string>(psobject.ImmediateBaseObject);
        Assert.Equal("object", psobject.ImmediateBaseObject as string);
        Assert.Equal("object", psobject.Property<string>("PSChildName"));
        Assert.True(psobject.Property<bool>("PSIsContainer"));
        Assert.Equal("test", psobject.Property<PSDriveInfo>("PSDrive").Name);
        Assert.Equal("JsonFS", psobject.Property<ProviderInfo>("PSProvider").Name);
        Assert.Equal(@"JsonFS\JsonFS::test:\object", psobject.Property<string>("PSPath"));
        Assert.Equal(@"JsonFS\JsonFS::test:", psobject.Property<string>("PSParentPath"));
    }

    [Fact]
    public void Powershell_retrieves_roots_childnodes_recursive()
    {
        // ARRANGE
        var root = this.ArrangeFileSystem(new JObject
        {
            ["object"] = new JObject
            {
                ["grandchild"] = new JObject()
                {
                    ["property"] = new JValue("text")
                }
            }
        });

        // ACT
        var result = this.PowerShell.AddCommand("Get-ChildItem")
            .AddParameter("Path", @"test:\")
            .AddParameter("Recurse")
            .Invoke()
            .ToArray();

        // ASSERT
        Assert.False(this.PowerShell.HadErrors);
        Assert.Equal(2, result.Length);

        var psobject = result[0];

        Assert.Equal("object", psobject.Property<string>("PSChildName"));
        Assert.True(psobject.Property<bool>("PSIsContainer"));
        Assert.Equal("test", psobject.Property<PSDriveInfo>("PSDrive").Name);
        Assert.Equal("JsonFS", psobject.Property<ProviderInfo>("PSProvider").Name);
        Assert.Equal(@"JsonFS\JsonFS::test:\object", psobject.Property<string>("PSPath"));
        Assert.Equal(@"JsonFS\JsonFS::test:", psobject.Property<string>("PSParentPath"));

        psobject = result[1];

        Assert.Equal("grandchild", psobject.Property<string>("PSChildName"));
        Assert.True(psobject.Property<bool>("PSIsContainer"));
        Assert.Equal("test", psobject.Property<PSDriveInfo>("PSDrive").Name);
        Assert.Equal("JsonFS", psobject.Property<ProviderInfo>("PSProvider").Name);
        Assert.Equal(@"JsonFS\JsonFS::test:\object\grandchild", psobject.Property<string>("PSPath"));
        Assert.Equal(@"JsonFS\JsonFS::test:\object", psobject.Property<string>("PSParentPath"));
    }

    [Fact]
    public void Powershell_retrieves_roots_childnodes_recursive_upto_depth()
    {
        // ARRANGE
        var root = this.ArrangeFileSystem(new JObject
        {
            ["object"] = new JObject
            {
                ["grandchild"] = new JObject()
                {
                    ["grandgrandchild"] = new JObject()
                }
            },
            ["property"] = "text",
        });

        // ACT
        var result = this.PowerShell.AddCommand("Get-ChildItem")
            .AddParameter("Path", @"test:\")
            .AddParameter("Recurse")
            .AddParameter("Depth", 1) // only children, no grandchildren
            .Invoke()
            .ToArray();

        // ASSERT
        Assert.False(this.PowerShell.HadErrors);
        Assert.Equal(2, result.Length);

        var psobject = result[0];

        Assert.Equal("object", psobject.Property<string>("PSChildName"));
        Assert.True(psobject.Property<bool>("PSIsContainer"));
        Assert.Equal("test", psobject.Property<PSDriveInfo>("PSDrive").Name);
        Assert.Equal("JsonFS", psobject.Property<ProviderInfo>("PSProvider").Name);
        Assert.Equal(@"JsonFS\JsonFS::test:\object", psobject.Property<string>("PSPath"));
        Assert.Equal(@"JsonFS\JsonFS::test:", psobject.Property<string>("PSParentPath"));

        psobject = result[1];

        Assert.Equal("grandchild", psobject.Property<string>("PSChildName"));
        Assert.True(psobject.Property<bool>("PSIsContainer"));
        Assert.Equal("test", psobject.Property<PSDriveInfo>("PSDrive").Name);
        Assert.Equal("JsonFS", psobject.Property<ProviderInfo>("PSProvider").Name);
        Assert.Equal(@"JsonFS\JsonFS::test:\object\grandchild", psobject.Property<string>("PSPath"));
        Assert.Equal(@"JsonFS\JsonFS::test:\object", psobject.Property<string>("PSParentPath"));
    }

    #endregion Get-ChildItem -Path -Recurse

    #region Remove-Item -Path -Recurse

    [Fact]
    public void Powershell_removes_root_child_node()
    {
        // ARRANGE
        var root = this.ArrangeFileSystem(DefaultRoot());

        // ACT
        var _ = this.PowerShell.AddCommand("Remove-Item")
            .AddParameter("Path", @"test:\object")
            .Invoke()
            .ToArray();

        // ASSERT
        Assert.False(this.PowerShell.HadErrors);

        this.AssertJsonFileContent(r =>
        {
            Assert.False(r.TryGetJObject("object", out var _));
        });
    }

    [Fact]
    public async Task Powershell_removes_root_child_node_and_validates()
    {
        // ARRANGE
        var root = this.ArrangeFileSystem(DefaultRoot(), await this.DefaultRootSchema());

        // ACT
        var _ = this.PowerShell.AddCommand("Remove-Item")
            .AddParameter("Path", @"test:\object")
            .Invoke()
            .ToArray();

        // ASSERT
        Assert.False(this.PowerShell.HadErrors);

        this.AssertJsonFileContent(r =>
        {
            Assert.False(r.TryGetJObject("object", out var _));
        });
    }

    [Fact]
    public async Task Powershell_removes_root_child_node_and_invalidates()
    {
        // ARRANGE
        var jsonSchema = await this.DefaultRootSchema();

        jsonSchema.RequiredProperties.Add("object");

        var root = this.ArrangeFileSystem(DefaultRoot(), jsonSchema);

        var originalRoot = root.ToString();

        // ACT
        var result = Assert.Throws<CmdletProviderInvocationException>(() => this.PowerShell.AddCommand("Remove-Item")
            .AddParameter("Path", @"test:\object")
            .Invoke()
            .ToArray());

        // ASSERT
        Assert.True(this.PowerShell.HadErrors);
        Assert.Equal("PropertyRequired: #/object", result.Message);

        this.AssertJsonFileContent(c => Assert.Equal(originalRoot, c.ToString()));
    }

    [Fact]
    public void Powershell_removes_root_child_node_fails_if_node_has_children()
    {
        // ARRANGE
        var root = this.ArrangeFileSystem(new JObject
        {
            ["object"] = new JObject()
            {
                ["grand-object"] = new JObject()
            }
        });

        // ACT
        var result = Assert.Throws<CmdletInvocationException>(() => this.PowerShell
            .AddCommand("Remove-Item")
            .AddParameter("Path", @"test:\object")
            .AddParameter("Recurse", false)
            .Invoke());

        // ASSERT
        Assert.True(this.PowerShell.HadErrors);

        this.AssertJsonFileContent(r =>
        {
            Assert.True(r.TryGetJObject("object", out var o));
            Assert.True(o!.TryGetJObject("grand-object", out var _));
        });
    }

    [Fact]
    public void Powershell_removes_root_child_node_recursive()
    {
        // ARRANGE
        var root = this.ArrangeFileSystem(new JObject
        {
            ["object"] = new JObject
            {
                ["grand-object"] = new JObject()
            }
        });

        // ACT
        var _ = this.PowerShell.AddCommand("Remove-Item")
            .AddParameter("Path", @"test:\object")
            .AddParameter("Recurse", true)
            .Invoke()
            .ToArray();

        // ASSERT
        Assert.False(this.PowerShell.HadErrors);

        this.AssertJsonFileContent(r =>
        {
            Assert.Empty(r);
        });
    }

    #endregion Remove-Item -Path -Recurse

    #region New-Item -Path -ItemType -Value

    [Fact]
    public void Powershell_creates_child_item_from_JObject()
    {
        // ARRANGE
        var root = this.ArrangeFileSystem(new JObject());
        var child = new JObject
        {
            ["child"] = new JObject()
        };

        // ACT
        var result = this.PowerShell.AddCommand("New-Item")
            .AddParameter("Path", @"test:\child1")
            .AddParameter("Value", child)
            .Invoke()
            .ToArray();

        // ASSERT
        Assert.False(this.PowerShell.HadErrors);

        var psobject = result.Single();

        Assert.Equal("child1", psobject.Property<string>("PSChildName"));
        Assert.True(psobject.Property<bool>("PSIsContainer"));
        Assert.Equal("test", psobject.Property<PSDriveInfo>("PSDrive").Name);
        Assert.Equal("JsonFS", psobject.Property<ProviderInfo>("PSProvider").Name);
        Assert.Equal(@"JsonFS\JsonFS::test:\child1", psobject.Property<string>("PSPath"));
        Assert.Equal(@"JsonFS\JsonFS::test:", psobject.Property<string>("PSParentPath"));

        this.AssertJsonFileContent(r =>
        {
            Assert.True(r.TryGetValue("child1", out var added));
            Assert.Empty(added);
        });
    }

    [Fact]
    public async Task Powershell_creates_child_item_from_JObject_and_validates()
    {
        // ARRANGE
        var jsonSchema = await JsonSchema.FromJsonAsync("""
        {
            "type":"object",
            "properties":{
                "child1" : {
                    "type":"object"
                }
            }
        }
        """);

        var root = this.ArrangeFileSystem(new JObject(), jsonSchema);
        var child = new JObject
        {
            ["child"] = new JObject()
        };

        // ACT
        var result = this.PowerShell.AddCommand("New-Item")
            .AddParameter("Path", @"test:\child1")
            .AddParameter("Value", child)
            .Invoke()
            .ToArray();

        // ASSERT
        Assert.False(this.PowerShell.HadErrors);

        var psobject = result.Single();

        Assert.Equal("child1", psobject.Property<string>("PSChildName"));
        Assert.True(psobject.Property<bool>("PSIsContainer"));
        Assert.Equal("test", psobject.Property<PSDriveInfo>("PSDrive").Name);
        Assert.Equal("JsonFS", psobject.Property<ProviderInfo>("PSProvider").Name);
        Assert.Equal(@"JsonFS\JsonFS::test:\child1", psobject.Property<string>("PSPath"));
        Assert.Equal(@"JsonFS\JsonFS::test:", psobject.Property<string>("PSParentPath"));

        this.AssertJsonFileContent(r =>
        {
            Assert.True(r.TryGetValue("child1", out var added));
            Assert.Empty(added);
        });
    }

    [Fact]
    public async Task Powershell_creates_child_item_from_JObject_and_invalidates()
    {
        // ARRANGE
        var jsonSchema = await JsonSchema.FromJsonAsync("""
            {
                "type":"object",
                "properties":{
                    "child1" : {
                        "type":"integer" // expects integer not object
                    }
                }
            }
            """);

        var root = this.ArrangeFileSystem(new JObject(), jsonSchema);

        var originalRoot = root.ToString();

        var child = new JObject
        {
            ["child"] = new JObject()
        };

        // ACT
        var result = Assert.Throws<CmdletProviderInvocationException>(() => this.PowerShell.AddCommand("New-Item")
            .AddParameter("Path", @"test:\child1")
            .AddParameter("Value", child)
            .Invoke()
            .ToArray());

        // ASSERT
        Assert.True(this.PowerShell.HadErrors);
        Assert.Equal("IntegerExpected: #/child1", result.Message);

        // File remains unchanged
        this.AssertJsonFileContent(r => Assert.Equal(originalRoot, r.ToString()));
    }

    [Fact]
    public async Task Powershell_creates_child_item_from_object_and_validates()
    {
        // ARRANGE
        var jsonSchema = await JsonSchema.FromJsonAsync("""
        {
            "type":"object",
            "properties":{
                "child1" : {
                    "type":"object"
                }
            }
        }
        """);

        var root = this.ArrangeFileSystem(new JObject(), jsonSchema);
        var child = new
        {
            child = new object { }
        };

        // ACT
        var result = this.PowerShell.AddCommand("New-Item")
            .AddParameter("Path", @"test:\child1")
            .AddParameter("Value", child)
            .Invoke()
            .ToArray();

        // ASSERT
        Assert.False(this.PowerShell.HadErrors);

        var psobject = result.Single();

        Assert.Equal("child1", psobject.Property<string>("PSChildName"));
        Assert.True(psobject.Property<bool>("PSIsContainer"));
        Assert.Equal("test", psobject.Property<PSDriveInfo>("PSDrive").Name);
        Assert.Equal("JsonFS", psobject.Property<ProviderInfo>("PSProvider").Name);
        Assert.Equal(@"JsonFS\JsonFS::test:\child1", psobject.Property<string>("PSPath"));
        Assert.Equal(@"JsonFS\JsonFS::test:", psobject.Property<string>("PSParentPath"));

        this.AssertJsonFileContent(r =>
        {
            Assert.True(r.TryGetValue("child1", out var added));
            Assert.Empty(added);
        });
    }

    [Fact]
    public void Powershell_creating_child_fails_with_non_JObject()
    {
        // ARRANGE
        var root = this.ArrangeFileSystem(new JObject());

        // ACT
        var result = Assert.Throws<CmdletProviderInvocationException>(() => this.PowerShell.AddCommand("New-Item")
            .AddParameter("Path", @"test:\child1")
            .AddParameter("Value", "value")
            .Invoke());

        // ASSERT
        Assert.True(this.PowerShell.HadErrors);
        Assert.Equal("Unexpected character encountered while parsing value: v. Path '', line 0, position 0.", result.Message);
    }

    [Fact]
    public void Powershell_creates_child_item_from_Hashtable()
    {
        // ARRANGE
        var root = this.ArrangeFileSystem(new JObject());
        var child = new Hashtable
        {
            ["value"] = 1,
            ["child2"] = new Hashtable()
        };

        // ACT
        var result = this.PowerShell.AddCommand("New-Item")
            .AddParameter("Path", @"test:\child1")
            .AddParameter("Value", child)
            .Invoke()
            .ToArray();

        // ASSERT
        Assert.False(this.PowerShell.HadErrors);

        var psobject = result.Single();

        Assert.Equal("child1", psobject.Property<string>("PSChildName"));
        Assert.True(psobject.Property<bool>("PSIsContainer"));
        Assert.Equal("test", psobject.Property<PSDriveInfo>("PSDrive").Name);
        Assert.Equal("JsonFS", psobject.Property<ProviderInfo>("PSProvider").Name);
        Assert.Equal(@"JsonFS\JsonFS::test:\child1", psobject.Property<string>("PSPath"));
        Assert.Equal(@"JsonFS\JsonFS::test:", psobject.Property<string>("PSParentPath"));

        this.AssertJsonFileContent(r =>
        {
            Assert.True(r.TryGetValue("child1", out var added));

            var addedObject = (JObject)added;
            Assert.True(addedObject.TryGetValue("value", out var value));
            Assert.Equal(new JValue(1), value);
            Assert.False(addedObject.TryGetJObject("child2", out var _));
        });
    }

    #endregion New-Item -Path -ItemType -Value

    #region Rename-Item -Path -NewName

    [Fact]
    public void Powershell_renames_childitem()
    {
        // ARRANGE
        var child = new JObject();
        var root = this.ArrangeFileSystem(new JObject
        {
            ["child1"] = child
        });

        // ACT
        var _ = this.PowerShell.AddCommand("Rename-Item")
            .AddParameter("Path", @"test:\child1")
            .AddParameter("NewName", "newName")
            .Invoke()
            .ToArray();

        // ASSERT
        Assert.False(this.PowerShell.HadErrors);

        this.AssertJsonFileContent(r =>
        {
            Assert.True(r.TryGetValue("newName", out var _));
            Assert.False(r.TryGetValue("child1", out var _));
        });
    }

    [Fact]
    public async Task Powershell_renames_childitem_and_validates()
    {
        // ARRANGE
        var jsonSchema = await JsonSchema.FromJsonAsync("""
        {
            "type":"object",
            "properties": {
                "child1" : {
                    "type":"object"
                },
                "newName" : {
                    "type":"object"
                }
            }
        }
        """);

        var child = new JObject();
        var root = this.ArrangeFileSystem(new JObject
        {
            ["child1"] = child
        }, jsonSchema);

        // ACT
        var _ = this.PowerShell.AddCommand("Rename-Item")
            .AddParameter("Path", @"test:\child1")
            .AddParameter("NewName", "newName")
            .Invoke()
            .ToArray();

        // ASSERT
        Assert.False(this.PowerShell.HadErrors);

        this.AssertJsonFileContent(r =>
        {
            Assert.True(r.TryGetValue("newName", out var _));
            Assert.False(r.TryGetValue("child1", out var _));
        });
    }

    [Fact]
    public async Task Powershell_renames_childitem_and_invalidates()
    {
        // ARRANGE
        var jsonSchema = await JsonSchema.FromJsonAsync("""
        {
            "type":"object",
            "properties": {
                "child1" : {
                    "type":"object"
                },
                "newName" : {
                    "type":"object"
                }
            },
            "required":["child1"] // can't be removed by renaming it
        }
        """);

        var child = new JObject();
        var root = this.ArrangeFileSystem(new JObject
        {
            ["child1"] = child
        }, jsonSchema);

        // ACT
        var result = Assert.Throws<CmdletProviderInvocationException>(() => this.PowerShell.AddCommand("Rename-Item")
            .AddParameter("Path", @"test:\child1")
            .AddParameter("NewName", "newName")
            .Invoke()
            .ToArray());

        // ASSERT
        Assert.True(this.PowerShell.HadErrors);
        Assert.Equal("PropertyRequired: #/child1", result.Message);

        // file remains unchanged
        this.AssertJsonFileContent(r =>
        {
            Assert.False(r.TryGetValue("newName", out var _));
            Assert.True(r.TryGetValue("child1", out var _));
        });
    }

    #endregion Rename-Item -Path -NewName

    #region Copy-Item -Path -Destination -Recurse

    [Fact]
    public void Powershell_copies_child()
    {
        // ARRANGE
        var child1 = new JObject()
        {
            ["property"] = new JValue(1),
            ["grandchild"] = new JObject()
        };

        var root = this.ArrangeFileSystem(new JObject
        {
            ["child1"] = child1,
            ["child2"] = new JObject()
        });

        // ACT
        // copy child1 under child2
        var _ = this.PowerShell.AddCommand("Copy-Item")
            .AddParameter("Path", @"test:\child1")
            .AddParameter("Destination", @"test:\child2")
            .Invoke()
            .ToArray();

        // ASSERT
        Assert.False(this.PowerShell.HadErrors);

        this.AssertJsonFileContent(r =>
        {
            // child1 is still there
            Assert.NotNull(r.ChildObject("child1"));

            // child2 has a new child: child1
            Assert.NotNull(r.ChildObject("child2").ChildObject("child1"));

            // copy is shallow and contains the property but not the child node
            Assert.True(r.ChildObject("child2").ChildObject("child1").TryGetValue("property", out var _));
            Assert.False(r.ChildObject("child2").ChildObject("child1").TryGetValue("grandchild", out var _));
        });
    }

    [Fact]
    public async Task Powershell_copies_child_and_validates()
    {
        // ARRANGE
        var jsonSchema = await JsonSchema.FromJsonAsync("""
        {
            "type":"object",
            "properties": {
                "child1" : {
                    "type":"object"
                },
                "child2" : {
                    "type":"object",
                    "properties":
                    {
                        "child2": {
                            "type":"object"
                        }
                    }
                }
            }
        }
        """);

        var child1 = new JObject()
        {
            ["property"] = new JValue(1),
            ["grandchild"] = new JObject()
        };

        var root = this.ArrangeFileSystem(new JObject
        {
            ["child1"] = child1,
            ["child2"] = new JObject()
        }, jsonSchema);

        // ACT
        // copy child1 under child2
        var _ = this.PowerShell.AddCommand("Copy-Item")
            .AddParameter("Path", @"test:\child1")
            .AddParameter("Destination", @"test:\child2")
            .Invoke()
            .ToArray();

        // ASSERT
        Assert.False(this.PowerShell.HadErrors);

        this.AssertJsonFileContent(r =>
        {
            // child1 is still there
            Assert.NotNull(r.ChildObject("child1"));

            // child2 has a new child: child1
            Assert.NotNull(r.ChildObject("child2").ChildObject("child1"));

            // copy is shallow and contains the property but not the child node
            Assert.True(r.ChildObject("child2").ChildObject("child1").TryGetValue("property", out var _));
            Assert.False(r.ChildObject("child2").ChildObject("child1").TryGetValue("grandchild", out var _));
        });
    }

    [Fact]
    public async Task Powershell_copies_child_and_invalidates()
    {
        // ARRANGE
        var jsonSchema = await JsonSchema.FromJsonAsync("""
        {
            "type":"object",
            "properties": {
                "child1" : {
                    "type":"object"
                },
                "child2" : {
                    "type":"object",
                    "additionalProperties":false // must not have child proprties
                }
            }
        }
        """);

        var child1 = new JObject()
        {
            ["property"] = new JValue(1),
            ["grandchild"] = new JObject()
        };

        var root = this.ArrangeFileSystem(new JObject
        {
            ["child1"] = child1,
            ["child2"] = new JObject()
        }, jsonSchema);

        var originalRoot = root.ToString();

        // ACT
        // copy child1 under child2
        var result = Assert.Throws<CmdletProviderInvocationException>(() => this.PowerShell.AddCommand("Copy-Item")
            .AddParameter("Path", @"test:\child1")
            .AddParameter("Destination", @"test:\child2")
            .Invoke()
            .ToArray());

        // ASSERT
        Assert.True(this.PowerShell.HadErrors);
        Assert.Equal("NoAdditionalPropertiesAllowed: #/child2.child1", result.Message);

        // file is unchanged
        this.AssertJsonFileContent(r => Assert.Equal(originalRoot, r.ToString()));
    }

    [Fact]
    public void Powershell_copies_child_to_second_jsonfs()
    {
        // ARRANGE
        var secondfile = $"{Guid.NewGuid()}.json";

        var childA_1 = new JObject()
        {
            ["property"] = new JValue(1),
            ["grandchild"] = new JObject()
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
        var _ = this.PowerShell
            .AddCommand("Copy-Item")
            .AddParameter("Path", @"test:\child1")
            .AddParameter("Destination", @"test-2:\")
            .Invoke()
            .ToArray();

        // ASSERT
        Assert.False(this.PowerShell.HadErrors);

        this.AssertJsonFileContent(secondfile, r =>
        {
            // new node has been created.
            Assert.NotNull(r.ChildObject("child1"));

            // copy is shallow and contains the property but not the child node
            Assert.True(r.ChildObject("child1").TryGetValue("property", out var _));
            Assert.False(r.ChildObject("child1").TryGetValue("grandchild", out var _));
        });
    }

    [Fact]
    public void Powershell_copies_child_item_with_new_name_to_second_jsonfs()
    {
        // ARRANGE
        var secondfile = $"{Guid.NewGuid()}.json";

        var childA_1 = new JObject()
        {
            ["property"] = new JValue(1),
            ["grandchild"] = new JObject()
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
        var _ = this.PowerShell
            .AddCommand("Copy-Item")
            .AddParameter("Path", @"test:\child1")
            .AddParameter("Destination", @"test-2:\new")
            .Invoke()
            .ToArray();

        // ASSERT
        Assert.False(this.PowerShell.HadErrors);

        this.AssertJsonFileContent(secondfile, r =>
        {
            // new node has been created
            Assert.NotNull(r.ChildObject("new"));

            // copy is shallow and contains the property but not the child node
            Assert.True(r.ChildObject("new").TryGetValue("property", out var _));
            Assert.False(r.ChildObject("new").TryGetValue("grandchild", out var _));
        });
    }

    [Fact]
    public void Powershell_copies_child_item_recursive()
    {
        // ARRANGE
        var child1 = new JObject()
        {
            ["property"] = new JValue(1),
            ["grandchild"] = new JObject()
            {
                ["property2"] = new JValue(2)
            }
        };

        var root = this.ArrangeFileSystem(new JObject
        {
            ["child1"] = child1,
            ["child2"] = new JObject()
        });

        // ACT
        // copy child1 under child2
        var _ = this.PowerShell.AddCommand("Copy-Item")
            .AddParameter("Path", @"test:\child1")
            .AddParameter("Destination", @"test:\child2")
            .AddParameter("Recurse")
            .Invoke()
            .ToArray();

        // ASSERT
        Assert.False(this.PowerShell.HadErrors);

        this.AssertJsonFileContent(r =>
        {
            // child1 is still there
            Assert.NotNull(r.ChildObject("child1"));

            // child2 has a new child: child1
            Assert.NotNull(r.ChildObject("child2").ChildObject("child1"));

            // copy is shallow and contains the property but not the child node
            Assert.True(r.ChildObject("child2").ChildObject("child1").TryGetValue("property", out var _));
            Assert.True(r.ChildObject("child2").ChildObject("child1").TryGetValue("grandchild", out var _));
            Assert.True(r.ChildObject("child2").ChildObject("child1").ChildObject("grandchild").TryGetValue("property2", out var property2));
            Assert.Equal(2, property2!.Value<int>());
        });
    }

    [Fact]
    public void Powershell_copies_child_item_recursive_to_second_jsonfs()
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
        var _ = this.PowerShell.AddCommand("Copy-Item")
            .AddParameter("Path", @"test:\child1")
            .AddParameter("Destination", @"test-2:\")
            .AddParameter("Recurse")
            .Invoke()
            .ToArray();

        // ASSERT
        Assert.False(this.PowerShell.HadErrors);

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
    public void Powershell_copies_child_item_with_new_name_recursive_to_second_jsonfs()
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
        var _ = this.PowerShell.AddCommand("Copy-Item")
            .AddParameter("Path", @"test:\child1")
            .AddParameter("Destination", @"test-2:\new")
            .AddParameter("Recurse")
            .Invoke()
            .ToArray();

        // ASSERT
        Assert.False(this.PowerShell.HadErrors);

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

    #endregion Copy-Item -Path -Destination -Recurse
}