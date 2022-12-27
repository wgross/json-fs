using Newtonsoft.Json.Schema;

namespace TreeStore.JsonFS.Test;

[Collection(nameof(PowerShell))]
public class DynamicPropertyCmdletProviderTest : PowerShellTestBase
{
    #region Remove-ItemProperty -Path -Name

    [Fact]
    public void Powershell_removes_item_propery()
    {
        // ARRANGE
        var child = new JObject();
        var root = this.ArrangeFileSystem(new JObject
        {
            ["data"] = 1,
            ["child"] = child
        });

        // ACT
        var result = this.PowerShell
            .AddCommand("Remove-ItemProperty")
            .AddParameter("Path", @"test:\")
            .AddParameter("Name", "data")
            .AddStatement()
            .AddCommand("Get-Item")
            .AddParameter("Path", @"test:\")
            .Invoke()
            .Single();

        // ASSERT
        Assert.False(this.PowerShell.HadErrors);
        Assert.DoesNotContain(result.Properties, p => p.Name == "data");
    }

    [Fact]
    public void Powershell_removes_item_propery_and_validates()
    {
        // ARRANGE
        var jsonSchema = JSchema.Parse("""
        {
            "type":"object",
            "properties":{
                "data":{
                    "type":"integer"
                },
                "child":{
                    "type":"object"
                }
            },
        }
        """);

        var child = new JObject();
        var root = this.ArrangeFileSystem(new JObject
        {
            ["data"] = 1,
            ["child"] = child
        }, jsonSchema);

        // ACT
        var result = this.PowerShell
            .AddCommand("Remove-ItemProperty")
            .AddParameter("Path", @"test:\")
            .AddParameter("Name", "data")
            .AddStatement()
            .AddCommand("Get-Item")
            .AddParameter("Path", @"test:\")
            .Invoke()
            .Single();

        // ASSERT
        Assert.False(this.PowerShell.HadErrors);
        Assert.DoesNotContain(result.Properties, p => p.Name == "data");
    }

    [Fact]
    public void Powershell_removes_item_propery_and_invalidates()
    {
        // ARRANGE
        var jsonSchema = JSchema.Parse("""
        {
            "type":"object",
            "properties":{
                "data":{
                    "type":"integer"
                },
                "child":{
                    "type":"object"
                }
            },
            "required":["data"]
        }
        """);

        var child = new JObject();
        var root = this.ArrangeFileSystem(new JObject
        {
            ["data"] = 1,
            ["child"] = child
        }, jsonSchema);

        var origionalRoot = root.ToString();

        // ACT
        var result = this.PowerShell
            .AddCommand("Remove-ItemProperty")
            .AddParameter("Path", @"test:\")
            .AddParameter("Name", "data")
            .AddStatement()
            .AddCommand("Get-Item")
            .AddParameter("Path", @"test:\")
            .Invoke()
            .Single();

        // ASSERT
        Assert.True(this.PowerShell.HadErrors);

        this.AssertSingleError<InvalidOperationException>(e => Assert.Equal("Required properties are missing from object: data. Path '', line 1, position 1.", e.Message));

        // node is unchanged
        Assert.Equal(1L, result.Property<long>("data"));

        // file is unchanged
        this.AssertJsonFileContent(c => Assert.Equal(origionalRoot, c.ToString()));
    }

    #endregion Remove-ItemProperty -Path -Name

    #region Copy-ItemProperty -Path -Destination -Name

    [Fact]
    public void Powershell_copies_item_property()
    {
        // ARRANGE
        var child = new JObject();
        var root = this.ArrangeFileSystem(new JObject
        {
            ["data"] = 1,
            ["child"] = child
        });

        // ACT
        var result = this.PowerShell.AddCommand("Copy-ItemProperty")
            .AddParameter("Path", @"test:\")
            .AddParameter("Destination", @"test:\child")
            .AddParameter("Name", "data")
            .AddStatement()
            .AddCommand("Get-Item")
            .AddParameter("Path", @"test:\")
            .AddStatement()
            .AddCommand("Get-Item")
            .AddParameter("Path", @"test:\child")
            .Invoke()
            .ToArray();

        // ASSERT
        Assert.False(this.PowerShell.HadErrors);
        Assert.Equal(2, result.Length);

        // the data property is still there
        Assert.Equal(1, result[0].Property<long>("data"));

        // the data property was added to the child
        Assert.Equal(1, result[1].Property<long>("data"));
    }

    [Fact]
    public void Powershell_copies_item_property_and_validates()
    {
        // ARRANGE
        var jsonSchema = JSchema.Parse("""
        {
            "type":"object",
            "properties": {
                "data":{},
                "child":{},
            }
        }
        """);

        var child = new JObject();
        var root = this.ArrangeFileSystem(new JObject
        {
            ["data"] = 1,
            ["child"] = child
        }, jsonSchema);

        var originalRoot = root.ToString();

        // ACT
        var result = this.PowerShell.AddCommand("Copy-ItemProperty")
            .AddParameter("Path", @"test:\")
            .AddParameter("Destination", @"test:\child")
            .AddParameter("Name", "data")
            .AddStatement()
            .AddCommand("Get-Item")
            .AddParameter("Path", @"test:\")
            .AddStatement()
            .AddCommand("Get-Item")
            .AddParameter("Path", @"test:\child")
            .Invoke()
            .ToArray();

        // ASSERT
        Assert.False(this.PowerShell.HadErrors);
        Assert.Equal(2, result.Length);

        // the data property is still there
        Assert.Equal(1, result[0].Property<long>("data"));

        // the data property was added to the child
        Assert.Equal(1, result[1].Property<long>("data"));
    }

    [Fact]
    public void Powershell_copies_item_property_and_invalidates()
    {
        // ARRANGE
        var jsonSchema = JSchema.Parse("""
        {
            "type":"object",
            "properties": {
                "data":{},
                "child":{
                    "properties":{
                        "data":{
                            "type":"string" // forbids prperty of type integer
                        }
                    }
                },
            }
        }
        """);

        var child = new JObject();
        var root = this.ArrangeFileSystem(new JObject
        {
            ["data"] = 1,
            ["child"] = child
        }, jsonSchema);

        var originalRoot = root.ToString();

        // ACT
        var result = this.PowerShell.AddCommand("Copy-ItemProperty")
            .AddParameter("Path", @"test:\")
            .AddParameter("Destination", @"test:\child")
            .AddParameter("Name", "data")
            .AddStatement()
            .AddCommand("Get-Item")
            .AddParameter("Path", @"test:\")
            .AddStatement()
            .AddCommand("Get-Item")
            .AddParameter("Path", @"test:\child")
            .Invoke()
            .ToArray();

        // ASSERT
        Assert.True(this.PowerShell.HadErrors);

        this.AssertSingleError<InvalidOperationException>(e => Assert.Equal("Invalid type. Expected String but got Integer. Path 'child.data', line 2, position 11.", e.Message));

        Assert.Equal(2, result.Length);

        // the data property is still there
        Assert.Equal(1, result[0].Property<long>("data"));

        // the data property wasn't added to the child
        Assert.True(result[1].PropertyIsNull("data"));

        // file is unchanged
        this.AssertJsonFileContent(c => Assert.Equal(originalRoot, c.ToString()));
    }

    #endregion Copy-ItemProperty -Path -Destination -Name

    #region Move-ItemProperty -Path -Destination -Name

    [Fact]
    public void Powershell_moves_item_property()
    {
        // ARRANGE
        var child = new JObject();
        var root = this.ArrangeFileSystem(new JObject
        {
            ["data"] = 1,
            ["child"] = child
        });

        // ACT
        var result = this.PowerShell.AddCommand("Move-ItemProperty")
            .AddParameter("Path", @"test:\")
            .AddParameter("Destination", @"test:\child")
            .AddParameter("Name", "data")
            .AddStatement()
            .AddCommand("Get-Item")
            .AddParameter("Path", @"test:\")
            .AddStatement()
            .AddCommand("Get-Item")
            .AddParameter("Path", @"test:\child")
            .Invoke()
            .ToArray();

        // ASSERT
        Assert.False(this.PowerShell.HadErrors);
        Assert.Equal(2, result.Length);

        // the data property was removed from root
        Assert.DoesNotContain(result[0].Properties, p => p.Name == "data");

        // the data property was added to the child
        Assert.Equal(1, result[1].Property<long>("data"));
    }

    [Fact]
    public void Powershell_moves_item_property_and_validates()
    {
        // ARRANGE
        var jsonSchema = JSchema.Parse("""
        {
            "type":"object",
            "properties":{
                "data":{
                    "type":"integer"
                },
                "child":{
                    "type":"object",
                    "properties":{
                        "data":{
                            "type":"integer"
                        }
                    }
                }
            }
        }
        """);

        var child = new JObject();

        var root = this.ArrangeFileSystem(new JObject
        {
            ["data"] = 1,
            ["child"] = child
        }, jsonSchema);

        // ACT
        var result = this.PowerShell.AddCommand("Move-ItemProperty")
            .AddParameter("Path", @"test:\")
            .AddParameter("Destination", @"test:\child")
            .AddParameter("Name", "data")
            .AddStatement()
            .AddCommand("Get-Item")
            .AddParameter("Path", @"test:\")
            .AddStatement()
            .AddCommand("Get-Item")
            .AddParameter("Path", @"test:\child")
            .Invoke()
            .ToArray();

        // ASSERT
        Assert.False(this.PowerShell.HadErrors);
        Assert.Equal(2, result.Length);

        // the data property was removed from root
        Assert.DoesNotContain(result[0].Properties, p => p.Name == "data");

        // the data property was added to the child
        Assert.Equal(1, result[1].Property<long>("data"));
    }

    [Fact]
    public void Powershell_moves_item_property_and_invalidates()
    {
        // ARRANGE
        var jsonSchema = JSchema.Parse("""
        {
            "type":"object",
            "properties":{
                "data":{
                    "type":"integer"
                },
                "child":{
                    "type":"object",
                    "properties":{
                        "data":{
                            "type":"integer"
                        }
                    }
                }
            },
            "required":["data"] // data can't be removed
        }
        """);

        var child = new JObject();

        var root = this.ArrangeFileSystem(new JObject
        {
            ["data"] = 1,
            ["child"] = child
        }, jsonSchema);

        var originalRoot = root.ToString();

        // ACT
        var result = this.PowerShell.AddCommand("Move-ItemProperty")
            .AddParameter("Path", @"test:\")
            .AddParameter("Destination", @"test:\child")
            .AddParameter("Name", "data")
            .AddStatement()
            .AddCommand("Get-Item")
            .AddParameter("Path", @"test:\")
            .AddStatement()
            .AddCommand("Get-Item")
            .AddParameter("Path", @"test:\child")
            .Invoke()
            .ToArray();

        // ASSERT
        Assert.True(this.PowerShell.HadErrors);

        this.AssertSingleError<InvalidOperationException>(e => Assert.Equal("Required properties are missing from object: data. Path '', line 1, position 1.", e.Message));

        Assert.Equal(2, result.Length);

        // the data property was removed from root
        Assert.Contains(result[0].Properties, p => p.Name == "data");

        // the data property was added to the child
        Assert.DoesNotContain(result[1].Properties, p => p.Name == "data");

        // file is unchanged
        this.AssertJsonFileContent(c => Assert.Equal(originalRoot, c.ToString()));
    }

    #endregion Move-ItemProperty -Path -Destination -Name

    #region New-ItemProperty -Path -Name -Value

    [Fact]
    public void Powershell_creates_item_property_from_scalar()
    {
        // ARRANGE
        var child = new JObject();
        var root = this.ArrangeFileSystem(new JObject
        {
            ["data"] = 1,
            ["child"] = child
        });

        // ACT
        var result = this.PowerShell.AddCommand("New-ItemProperty")
            .AddParameter("Path", @"test:\")
            .AddParameter("Name", "newdata")
            .AddParameter("Value", 1)
            .AddStatement()
            .AddCommand("Get-Item")
            .AddParameter("Path", @"test:\")
            .Invoke()
            .Single();

        // ASSERT
        // property was created with value
        Assert.False(this.PowerShell.HadErrors);
        Assert.Equal(1, result.Property<int>("newdata"));
    }

    [Fact]
    public void Powershell_creates_item_property_from_scalar_and_validates()
    {
        // ARRANGE
        var jsonSchema = JSchema.Parse("""
        {
            "type":"object",
            "properties":{
                "data":{
                    "type":"integer"
                }
            }
        }
        """);

        var child = new JObject();

        var root = this.ArrangeFileSystem(new JObject
        {
            ["data"] = 1,
            ["child"] = child
        }, jsonSchema);

        // ACT
        var result = this.PowerShell.AddCommand("New-ItemProperty")
            .AddParameter("Path", @"test:\")
            .AddParameter("Name", "newdata")
            .AddParameter("Value", 1)
            .AddStatement()
            .AddCommand("Get-Item")
            .AddParameter("Path", @"test:\")
            .Invoke()
            .Single();

        // ASSERT
        // property was created with value
        Assert.False(this.PowerShell.HadErrors);
        Assert.Equal(1, result.Property<int>("newdata"));
    }

    [Fact]
    public void Powershell_creates_item_property_from_scalar_and_invalidates()
    {
        // ARRANGE
        var jsonSchema = JSchema.Parse("""
        {
            "type":"object",
            "properties":{
                "data":{
                    "type":"string" // requires a string
                }
            }
        }
        """);

        var child = new JObject();

        var root = this.ArrangeFileSystem(new JObject
        {
            ["data"] = 1,
            ["child"] = child
        }, jsonSchema);

        var originalRoot = root.ToString();

        // ACT
        var result = this.PowerShell.AddCommand("New-ItemProperty")
            .AddParameter("Path", @"test:\")
            .AddParameter("Name", "newdata")
            .AddParameter("Value", 1) // create with an integer -> invalid
            .AddStatement()
            .AddCommand("Get-Item")
            .AddParameter("Path", @"test:\")
            .Invoke()
            .ToArray();

        // ASSERT
        // property was created with value
        Assert.True(this.PowerShell.HadErrors);

        this.AssertSingleError<InvalidOperationException>(e => Assert.Equal("Invalid type. Expected String but got Integer. Path 'data', line 2, position 11.", e.Message));

        Assert.True(result[0].PropertyIsNull("newdata"));

        // file is unchanged
        this.AssertJsonFileContent(c => Assert.Equal(originalRoot, c.ToString()));
    }

    [Fact]
    public void Powershell_creates_item_property_from_value_array()
    {
        // ARRANGE
        var child = new JObject();
        var root = this.ArrangeFileSystem(new JObject
        {
            ["data"] = 1,
            ["child"] = child
        });

        // ACT
        var result = this.PowerShell.AddCommand("New-ItemProperty")
            .AddParameter("Path", @"test:\")
            .AddParameter("Name", "newdata")
            .AddParameter("Value", new int[] { 1, 2, 3 })
            .AddStatement()
            .AddCommand("Get-Item")
            .AddParameter("Path", @"test:\")
            .Invoke()
            .Single();

        // ASSERT
        // property was created with value
        Assert.False(this.PowerShell.HadErrors);
        Assert.Equal(new object[] { 1, 2, 3 }, result.Property<object[]>("newdata"));
    }

    [Fact]
    public void Powershell_creates_item_property_from_PS_value_array()
    {
        // ARRANGE
        var child = new JObject();
        var root = this.ArrangeFileSystem(new JObject
        {
            ["data"] = 1,
            ["child"] = child
        });

        // ACT
        var result = this.PowerShell.AddCommand("New-ItemProperty")
            .AddParameter("Path", @"test:\")
            .AddParameter("Name", "newdata")
            .AddParameter("Value", new object[] { 1, 2, 3 })
            .AddStatement()
            .AddCommand("Get-Item")
            .AddParameter("Path", @"test:\")
            .Invoke()
            .Single();

        // ASSERT
        // property was created with value
        Assert.False(this.PowerShell.HadErrors);
        Assert.Equal(new object[] { 1, 2, 3 }, result.Property<object[]>("newdata"));
    }

    #endregion New-ItemProperty -Path -Name -Value

    #region Rename-ItemProperty -Path -Name -NewName

    [Fact]
    public void Powershell_renames_item_property()
    {
        // ARRANGE
        var child = new JObject();
        var root = this.ArrangeFileSystem(new JObject
        {
            ["data"] = 1,
            ["child"] = child
        });

        // ACT
        var result = this.PowerShell
            .AddCommand("Rename-ItemProperty")
            .AddParameter("Path", @"test:\")
            .AddParameter("Name", "data")
            .AddParameter("NewName", "newname")
            .AddStatement()
            .AddCommand("Get-Item")
            .AddParameter("Path", @"test:\")
            .Invoke()
            .Single();

        // ASSERT
        Assert.False(this.PowerShell.HadErrors);
        Assert.Equal(1L, result.Property<long>("newname"));
    }

    #endregion Rename-ItemProperty -Path -Name -NewName
}