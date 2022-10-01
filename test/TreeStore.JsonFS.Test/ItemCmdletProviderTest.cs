namespace TreeStore.JsonFS.Test;

[Collection(nameof(PowerShell))]
public class ItemCmdletProviderTest : PowerShellTestBase
{
    #region Get-Item -Path

    [Fact]
    public void Powershell_create_file_and_reads_root_node()
    {
        // ARRANGE
        this.ArrangeFileSystem(this.JsonFilePath);

        // ACT
        var result = this.PowerShell
            .AddCommand("Get-Item")
            .AddParameter("Path", @"test:\")
            .Invoke()
            .ToArray();

        // ASSERT
        Assert.False(this.PowerShell.HadErrors);

        var psobject = result.Single();

        Assert.IsType<JsonFsItem>(psobject.BaseObject);
        Assert.Equal("", psobject.Property<string>("Name"));
        Assert.Equal("test:", psobject.Property<string>("PSChildName"));
        Assert.True(psobject.Property<bool>("PSIsContainer"));
        Assert.Equal("test", psobject.Property<PSDriveInfo>("PSDrive").Name);
        Assert.Equal("JsonFS", psobject.Property<ProviderInfo>("PSProvider").Name);
        Assert.Equal(@"JsonFS\JsonFS::test:\", psobject.Property<string>("PSPath"));
        Assert.Equal(string.Empty, psobject.Property<string>("PSParentPath"));
    }

    [Fact]
    public void Powershell_opens_empty_file_and_reads_root_node()
    {
        // ARRANGE
        File.WriteAllText(this.JsonFilePath, "");
        this.ArrangeFileSystem(this.JsonFilePath);

        // ACT
        var result = this.PowerShell
            .AddCommand("Get-Item")
            .AddParameter("Path", @"test:\")
            .Invoke()
            .ToArray();

        // ASSERT
        Assert.False(this.PowerShell.HadErrors);

        var psobject = result.Single();

        Assert.Equal("test:", psobject.Property<string>("PSChildName"));
        Assert.True(psobject.Property<bool>("PSIsContainer"));
        Assert.Equal("test", psobject.Property<PSDriveInfo>("PSDrive").Name);
        Assert.Equal("JsonFS", psobject.Property<ProviderInfo>("PSProvider").Name);
        Assert.Equal(@"JsonFS\JsonFS::test:\", psobject.Property<string>("PSPath"));
        Assert.Equal(string.Empty, psobject.Property<string>("PSParentPath"));
    }

    [Fact]
    public void Powershell_reads_root_node()
    {
        // ARRANGE
        this.ArrangeFileSystem(DefaultRoot());

        // ACT
        var result = this.PowerShell
            .AddCommand("Get-Item")
            .AddParameter("Path", @"test:\")
            .Invoke()
            .ToArray();

        // ASSERT
        Assert.False(this.PowerShell.HadErrors);

        var psobject = result.Single();

        Assert.IsType<JsonFsItem>(psobject.BaseObject);
        Assert.Equal("", psobject.Property<string>("Name"));
        Assert.Equal("test:", psobject.Property<string>("PSChildName"));
        Assert.True(psobject.Property<bool>("PSIsContainer"));
        Assert.Equal("test", psobject.Property<PSDriveInfo>("PSDrive").Name);
        Assert.Equal("JsonFS", psobject.Property<ProviderInfo>("PSProvider").Name);
        Assert.Equal(@"JsonFS\JsonFS::test:\", psobject.Property<string>("PSPath"));
        Assert.Equal(string.Empty, psobject.Property<string>("PSParentPath"));
    }

    [Fact]
    public void Powershell_reads_root_child_node()
    {
        // ARRANGE
        this.ArrangeFileSystem(new JObject()
        {
            ["object"] = new JObject(),
        });

        // ACT
        var result = this.PowerShell.AddCommand("Get-Item")
            .AddParameter("Path", @"test:\object")
            .Invoke()
            .ToArray();

        // ASSERT
        Assert.False(this.PowerShell.HadErrors);

        var psobject = result.Single();

        Assert.IsType<JsonFsItem>(psobject.BaseObject);
        Assert.Equal("object", psobject.Property<string>("Name"));
        Assert.Equal("object", psobject.Property<string>("PSChildName"));
        Assert.True(psobject.Property<bool>("PSIsContainer"));
        Assert.Equal("test", psobject.Property<PSDriveInfo>("PSDrive").Name);
        Assert.Equal("JsonFS", psobject.Property<ProviderInfo>("PSProvider").Name);
        Assert.Equal(@"JsonFS\JsonFS::test:\object", psobject.Property<string>("PSPath"));
        Assert.Equal(@"JsonFS\JsonFS::test:", psobject.Property<string>("PSParentPath"));
    }

    [Fact]
    public void Powershell_reads_root_child_node_with_provider_qualified_path()
    {
        // ARRANGE
        this.ArrangeFileSystem(new JObject()
        {
            ["object"] = new JObject(),
        });

        // ACT
        var result = this.PowerShell.AddCommand("Get-Item")
            .AddParameter("Path", @"JsonFS\JsonFS::test:\object")
            .Invoke()
            .ToArray();

        // ASSERT
        Assert.False(this.PowerShell.HadErrors);

        var psobject = result.Single();

        Assert.Equal("object", psobject.Property<string>("PSChildName"));
        Assert.True(psobject.Property<bool>("PSIsContainer"));
        Assert.True(psobject.PropertyIsNull("PSDrive"), "No PSDrive when using provider path");
        Assert.Equal("JsonFS", psobject.Property<ProviderInfo>("PSProvider").Name);
        Assert.Equal(@"JsonFS\JsonFS::test:\object", psobject.Property<string>("PSPath"));
        Assert.Equal(@"JsonFS\JsonFS::test:", psobject.Property<string>("PSParentPath"));
    }

    [Fact]
    public void Powershell_reads_root_grand_child_node_from_object()
    {
        // ARRANGE
        this.ArrangeFileSystem(new JObject()
        {
            ["object"] = new JObject()
            {
                ["object"] = new JObject()
            }
        });

        // ACT
        var result = this.PowerShell.AddCommand("Get-Item")
            .AddParameter("Path", @"test:\object\object")
            .Invoke()
            .ToArray();

        // ASSERT
        Assert.False(this.PowerShell.HadErrors);

        var psobject = result.Single();

        Assert.IsType<JsonFsItem>(psobject.BaseObject);
        Assert.Equal("object", psobject.Property<string>("Name"));
        Assert.Equal("object", psobject.Property<string>("PSChildName"));
        Assert.True(psobject.Property<bool>("PSIsContainer"));
        Assert.Equal("test", psobject.Property<PSDriveInfo>("PSDrive").Name);
        Assert.Equal("JsonFS", psobject.Property<ProviderInfo>("PSProvider").Name);
        Assert.Equal(@"JsonFS\JsonFS::test:\object\object", psobject.Property<string>("PSPath"));
        Assert.Equal(@"JsonFS\JsonFS::test:\object", psobject.Property<string>("PSParentPath"));
    }

    [Fact]
    public void Powershell_reads_root_grand_child_node_from_jarray()
    {
        // ARRANGE
        this.ArrangeFileSystem(new JObject()
        {
            ["object"] = new JArray(new JObject())
        });

        // ACT
        var result = this.PowerShell.AddCommand("Get-Item")
            .AddParameter("Path", @"test:\object\0")
            .Invoke()
            .ToArray();

        // ASSERT
        Assert.False(this.PowerShell.HadErrors);

        var psobject = result.Single();

        Assert.IsType<JsonFsItem>(psobject.BaseObject);
        Assert.Equal("0", psobject.Property<string>("Name"));
        Assert.Equal("0", psobject.Property<string>("PSChildName"));
        Assert.True(psobject.Property<bool>("PSIsContainer"));
        Assert.Equal("test", psobject.Property<PSDriveInfo>("PSDrive").Name);
        Assert.Equal("JsonFS", psobject.Property<ProviderInfo>("PSProvider").Name);
        Assert.Equal(@"JsonFS\JsonFS::test:\object\0", psobject.Property<string>("PSPath"));
        Assert.Equal(@"JsonFS\JsonFS::test:\object", psobject.Property<string>("PSParentPath"));
    }

    #endregion Get-Item -Path

    #region Resolve-Path -Path

    [Fact]
    public void Powershell_resolves_root_node_path()
    {
        // ARRANGE
        this.ArrangeFileSystem(this.JsonFilePath);

        // ACT
        var result = this.PowerShell.AddCommand("Resolve-Path")
            .AddParameter("Path", @"test:\")
            .Invoke()
            .Single();

        // ASSERT
        Assert.False(this.PowerShell.HadErrors);
        Assert.IsType<PathInfo>(result.BaseObject);
        Assert.Equal("test", result.Property<PSDriveInfo>("Drive").Name);
        Assert.Equal("JsonFS", result.Property<ProviderInfo>("Provider").Name);
    }

    #endregion Resolve-Path -Path

    #region Set-Item -Path

    [Fact]
    public void Powershell_sets_item_value_from_JObject()
    {
        // ARRANGE
        var root = this.ArrangeFileSystem(new JObject
        {
            ["child"] = new JObject(),
            ["value1"] = new JValue("text")
        });

        var newValue = new JObject
        {
            ["value1"] = new JValue(1)
        };

        // ACT
        var result = this.PowerShell.AddCommand("Set-Item")
            .AddParameter("Path", @"test:\child")
            .AddParameter("Value", newValue)
            .AddStatement()
            .AddCommand("Get-Item")
            .AddParameter("Path", @"test:\child")
            .Invoke()
            .Single();

        // ASSERT
        Assert.False(this.PowerShell.HadErrors);
        Assert.Equal(1, result.Property<long>("value1"));

        this.AssertJsonFileContent(r =>
        {
            Assert.Equal(newValue["data"], ((JObject)r["child"]!)["data"]);
        });
    }

    [Fact]
    public void Powershell_sets_item_value_from_hashtable()
    {
        // ARRANGE

        var root = this.ArrangeFileSystem(new JObject
        {
            ["child"] = new JObject(),
            ["value1"] = new JValue("text")
        });

        var newValue = new JObject
        {
            ["value1"] = new JValue(1)
        };

        var content = this.PowerShell
            .AddCommand("ConvertFrom-Json")
            .AddArgument(newValue.ToString())
            .Invoke().Single();

        this.PowerShell.Commands.Clear();

        // ACT
        var result = this.PowerShell
            .AddCommand("Set-Item")
            .AddParameter("Path", @"test:\child")
            .AddParameter("Value", content)
            .AddStatement()
            .AddCommand("Get-Item")
            .AddParameter("Path", @"test:\child")
            .Invoke()
            .Single();

        // ASSERT
        Assert.False(this.PowerShell.HadErrors);
        Assert.Equal(1, result.Property<long>("value1"));

        this.AssertJsonFileContent(r =>
        {
            Assert.Equal(newValue["data"], ((JObject)r["child"]!)["data"]);
        });
    }

    [Fact]
    public void Powershell_sets_item_value_from_string()
    {
        // ARRANGE

        var root = this.ArrangeFileSystem(new JObject
        {
            ["child"] = new JObject(),
            ["value1"] = new JValue("text")
        });

        var newValue = new JObject
        {
            ["value1"] = new JValue(1)
        };

        // ACT
        var result = this.PowerShell.AddCommand("Set-Item")
            .AddParameter("Path", @"test:\child")
            .AddParameter("Value", newValue.ToString())
            .AddStatement()
            .AddCommand("Get-Item")
            .AddParameter("Path", @"test:\child")
            .Invoke()
            .Single();

        // ASSERT
        Assert.False(this.PowerShell.HadErrors);
        Assert.Equal(1, result.Property<long>("value1"));

        this.AssertJsonFileContent(r =>
        {
            Assert.Equal(newValue["data"], ((JObject)r["child"]!)["data"]);
        });
    }

    #endregion Set-Item -Path

    #region Clear-Item -Path

    [Fact]
    public void Powershell_clears_item_value()
    {
        // ARRANGE
        var root = this.ArrangeFileSystem(new JObject
        {
            ["child"] = new JObject(),
            ["value"] = new JValue(1),
            ["array"] = new JArray(1, 2)
        });

        // ACT
        var result = this.PowerShell
            .AddCommand("Clear-Item")
            .AddParameter("Path", @"test:\")
            .AddStatement()
            .AddCommand("Get-Item")
            .AddParameter("Path", @"test:\")
            .Invoke()
            .Single();

        // ASSERT
        // underlying JObject has value properties nulled
        Assert.False(this.PowerShell.HadErrors);
        Assert.Null(result.Property<int?>("value"));
        Assert.Null(result.Property<int[]>("array"));

        this.AssertJsonFileContent(r =>
        {
            Assert.Equal(JValue.CreateNull(), r.Property("value")!.Value);
            Assert.Equal(JValue.CreateNull(), r.Property("array")!.Value);
        });
    }

    #endregion Clear-Item -Path

    #region Test-Path -Path -PathType

    [Fact]
    public void Powershell_tests_root_path()
    {
        // ARRANGE
        var root = this.ArrangeFileSystem(DefaultRoot());

        // ACT
        var result = this.PowerShell.AddCommand("Test-Path")
            .AddParameter("Path", @"test:\")
            .Invoke()
            .ToArray();

        // ASSERT
        Assert.False(this.PowerShell.HadErrors);
        Assert.True((bool)result.Single().BaseObject);
    }

    [Fact]
    public void Powershell_tests_root_path_as_container()
    {
        // ARRANGE
        var root = this.ArrangeFileSystem(DefaultRoot());

        // ACT
        var result = this.PowerShell.AddCommand("Test-Path")
            .AddParameter("Path", @"test:\")
            .AddParameter("PathType", "Container")
            .Invoke()
            .ToArray();

        // ASSERT
        Assert.False(this.PowerShell.HadErrors);
        Assert.True((bool)result.Single().BaseObject);
    }

    [Fact]
    public void Powershell_testing_root_path_as_leaf_fails()
    {
        // ARRANGE
        var root = this.ArrangeFileSystem(DefaultRoot());

        // ACT
        var result = this.PowerShell.AddCommand("Test-Path")
            .AddParameter("Path", @"test:\")
            .AddParameter("PathType", "Leaf")
            .Invoke()
            .ToArray();

        // ASSERT
        Assert.False(this.PowerShell.HadErrors);
        Assert.False((bool)result.Single().BaseObject);
    }

    [Fact]
    public void Powershell_tests_child_path_as_container()
    {
        // ARRANGE
        this.ArrangeFileSystem(DefaultRoot());

        // ACT
        var result = this.PowerShell.AddCommand("Test-Path")
            .AddParameter("Path", @"test:\object")
            .Invoke()
            .ToArray();

        // ASSERT
        Assert.False(this.PowerShell.HadErrors);
        Assert.True((bool)result.Single().BaseObject);
    }

    #endregion Test-Path -Path -PathType

    #region Get-Content

    [Fact]
    public void Powershell_retrieves_JObject_content()
    {
        // ARRANGE
        var json = new JObject
        {
            ["child"] = new JObject(),
            ["value1"] = new JValue("text")
        };

        var root = this.ArrangeFileSystem(json);

        // ACT
        var result = this.PowerShell.AddCommand("Get-Content")
            .AddParameter("Path", @"test:\")
            .Invoke()
            .Single();

        // ASSERT
        Assert.False(this.PowerShell.HadErrors);
        Assert.Equal(json.ToString(), result);
    }

    [Fact]
    public void Powershell_retrieves_JArray_content()
    {
        // ARRANGE
        var json = new JObject
        {
            ["child"] = new JArray(
                new JObject
                {
                    ["value1"] = 1
                },
                new JObject
                {
                    ["value2"] = 1
                }),
        };

        var root = this.ArrangeFileSystem(json);

        // ACT
        var result = this.PowerShell.AddCommand("Get-Content")
            .AddParameter("Path", @"test:\child")
            .Invoke()
            .ToArray();

        // ASSERT
        Assert.False(this.PowerShell.HadErrors);
        Assert.Equal(json.ChildArray("child")[0].ToString(), result[0]);
        Assert.Equal(json.ChildArray("child")[1].ToString(), result[1]);
    }

    #endregion Get-Content

    #region Set-Content

    [Fact]
    public void Powershell_writes_JObject_content()
    {
        // ARRANGE
        var json = new JObject();

        var root = this.ArrangeFileSystem(json);

        var content = new JObject
        {
            ["child"] = new JObject(),
            ["value1"] = new JValue("text")
        };

        // ACT
        var result = this.PowerShell
            .AddCommand("Set-Content")
                .AddParameter("Path", @"test:\")
                    .AddParameter("Value", content.ToString())
            .AddStatement()
                .AddCommand("Get-Content")
                    .AddParameter("Path", @"test:\")
            .Invoke()
            .Single();

        // ASSERT
        Assert.False(this.PowerShell.HadErrors);
        Assert.Equal(content.ToString(), result);

        this.AssertJsonFileContent(r =>
        {
            Assert.Equal(content.ToString(), r.ToString());
        });
    }

    [Fact]
    public void Powershell_copy_JObject_by_content()
    {
        // ARRANGE
        var root = this.ArrangeFileSystem(new JObject
        {
            ["child"] = new JObject
            {
                ["value"] = new JValue("value")
            },
            ["value1"] = new JValue("text")
        });

        // ACT
        // pipe the content of test:\child in test:\new-child
        var result = this.PowerShell
            .AddScript(@"Get-Content -Path test:\child | Set-Content -Path test:\new-child")
            .AddStatement()
                .AddCommand("Get-Content")
                    .AddParameter("Path", @"test:\new-child")
            .Invoke()
            .ToArray();

        // ASSERT
        Assert.False(this.PowerShell.HadErrors);
        //Assert.Equal(content.ToString(), result);

        this.AssertJsonFileContent(r =>
        {
            //Assert.Equal(content.ToString(), r.ToString());
        });
    }

    #endregion Set-Content
}