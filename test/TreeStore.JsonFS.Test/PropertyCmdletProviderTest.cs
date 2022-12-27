using Newtonsoft.Json.Schema;

namespace TreeStore.JsonFS.Test;

[Collection(nameof(PowerShell))]
public class PropertyCmdletProviderTest : PowerShellTestBase
{
    #region Get-ItemProperty -Path -Name

    [Fact]
    public void Powershell_gets_item_property()
    {
        // ARRANGE
        var root = this.ArrangeFileSystem(new JObject
        {
            ["value"] = 1,
            ["value_skipped"] = 2,
            ["array"] = new JArray(1, 2)
        });

        // ACT
        var result = this.PowerShell
            .AddCommand("Get-ItemProperty")
            .AddParameter("Path", @"test:\")
            .AddParameter("Name", new[] { "value", "array" })
            .Invoke()
            .Single();

        // ASSERT
        // an object having the requested properties only was returned
        Assert.False(this.PowerShell.HadErrors);
        Assert.Equal(1L, result.Property<long>("value"));
        Assert.Equal(new long[] { 1, 2 }, result.Property<object[]>("array").Select(o => (long)o));
        Assert.True(result.Property<bool>("PSIsContainer"));
        Assert.Equal("test", result.Property<PSDriveInfo>("PSDrive").Name);
        Assert.Equal("JsonFS", result.Property<ProviderInfo>("PSProvider").Name);
        Assert.Equal(@"JsonFS\JsonFS::test:\", result.Property<string>("PSPath"));
        Assert.Equal("", result.Property<string>("PSParentPath"));

        // the property value_skipped is missing in the result
        Assert.DoesNotContain(result.Properties, p => p.Name == "value_skipped");
    }

    #endregion Get-ItemProperty -Path -Name

    #region Clear-ItemProperty -Path -Name

    [Fact]
    public void PowerShell_clears_item_property_value()
    {
        // ARRANGE
        var root = this.ArrangeFileSystem(new JObject
        {
            ["value"] = 1,
            ["value_skipped"] = 2,
            ["array"] = new JArray(1, 2)
        });

        // ACT
        var result = this.PowerShell
            .AddCommand("Clear-ItemProperty")
            .AddParameter("Path", @"test:\")
            .AddParameter("Name", "value")
            .AddStatement()
            .AddCommand("Get-Item")
            .AddParameter("Path", @"test:\")
            .Invoke()
            .Single();

        // ASSERT
        Assert.False(this.PowerShell.HadErrors);

        // property is nulled
        Assert.Null(result.Property<object>("value"));

        // other properties are still there
        Assert.Equal(2, result.Property<long>("value_skipped"));
        Assert.Equal(new long[] { 1, 2 }, result.Property<object[]>("array").Select(o => (long)o));
    }

    [Fact]
    public void PowerShell_clears_item_property_value_and_validates()
    {
        // ARRANGE
        var jsonSchema = JSchema.Parse("""
        {
            "type":"object",
            "properties":{
                "value": {
                    "type":["integer","null"]
                }
            }
        }
        """);
        var root = this.ArrangeFileSystem(new JObject
        {
            ["value"] = 1,
            ["value_skipped"] = 2,
            ["array"] = new JArray(1, 2)
        }, jsonSchema);

        // ACT
        var result = this.PowerShell
            .AddCommand("Clear-ItemProperty")
            .AddParameter("Path", @"test:\")
            .AddParameter("Name", "value")
            .AddStatement()
            .AddCommand("Get-Item")
            .AddParameter("Path", @"test:\")
            .Invoke()
            .Single();

        // ASSERT
        Assert.False(this.PowerShell.HadErrors);

        // property is nulled
        Assert.Null(result.Property<object>("value"));

        // other properties are still there
        Assert.Equal(2, result.Property<long>("value_skipped"));
        Assert.Equal(new long[] { 1, 2 }, result.Property<object[]>("array").Select(o => (long)o));
    }

    [Fact]
    public void PowerShell_clears_item_property_value_and_invalidates()
    {
        // ARRANGE
        var jsonSchema = JSchema.Parse("""
        {
            "type":"object",
            "properties":{
                "value": {
                    "type":["integer"] // property can't be null == cleared
                }
            }
        }
        """);
        var root = this.ArrangeFileSystem(new JObject
        {
            ["value"] = 1,
            ["value_skipped"] = 2,
            ["array"] = new JArray(1, 2)
        }, jsonSchema);

        var originalRoot = root.ToString();

        // ACT
        var result = this.PowerShell
            .AddCommand("Clear-ItemProperty")
            .AddParameter("Path", @"test:\")
            .AddParameter("Name", "value")
            .AddStatement()
            .AddCommand("Get-Item")
            .AddParameter("Path", @"test:\")
            .Invoke()
            .Single();

        // ASSERT
        Assert.True(this.PowerShell.HadErrors);
        this.AssertSingleError<InvalidOperationException>(e => Assert.Equal("Invalid type. Expected Integer but got Null. Path 'value'.", e.Message));

        // file is unchanged
        this.AssertJsonFileContent(c => Assert.Equal(originalRoot, c.ToString()));
    }

    #endregion Clear-ItemProperty -Path -Name

    #region Set-ItemProperty -Path -Name -Force

    [Fact]
    public void Powershell_sets_item_property()
    {
        // ARRANGE
        var root = this.ArrangeFileSystem(new JObject
        {
            ["data"] = 1
        });

        // ACT
        var result = this.PowerShell.AddCommand("Set-ItemProperty")
            .AddParameter("Path", @"test:\")
            .AddParameter("Name", "data")
            .AddParameter("Value", "text")
            .AddStatement()
            .AddCommand("Get-Item")
            .AddParameter("Path", @"test:\")
            .Invoke()
            .Single();

        // ASSERT
        // value has changed
        Assert.False(this.PowerShell.HadErrors);
        Assert.Equal("text", result.Property<string>("data"));
    }

    [Fact]
    public void Powershell_sets_item_property_and_validates()
    {
        // ARRANGE
        var jsonSchema = JSchema.Parse("""
        {
            "type":"object",
            "properties":{
                "data":{
                    "type":"string"
                }
            }
        }
        """);

        var root = this.ArrangeFileSystem(new JObject
        {
            ["data"] = 1
        }, jsonSchema);

        // ACT
        var result = this.PowerShell.AddCommand("Set-ItemProperty")
            .AddParameter("Path", @"test:\")
            .AddParameter("Name", "data")
            .AddParameter("Value", "text")
            .AddStatement()
            .AddCommand("Get-Item")
            .AddParameter("Path", @"test:\")
            .Invoke()
            .Single();

        // ASSERT
        Assert.False(this.PowerShell.HadErrors);

        // value hasn't changed
        Assert.Equal("text", result.Property<string>("data"));

        this.AssertJsonFileContent(c =>
        {
            Assert.Equal("text", c.Property("data")!.Value.ToString());
        });
    }

    [Fact]
    public void Powershell_sets_item_property_and_invalidates()
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

        var root = this.ArrangeFileSystem(new JObject
        {
            ["data"] = 1
        }, jsonSchema);

        var originalRoot = root.ToString();

        // ACT
        var result = this.PowerShell.AddCommand("Set-ItemProperty")
            .AddParameter("Path", @"test:\")
            .AddParameter("Name", "data")
            .AddParameter("Value", "text") // this must be a number
            .AddStatement()
            .AddCommand("Get-Item")
            .AddParameter("Path", @"test:\")
            .Invoke()
            .Single();

        // ASSERT
        Assert.True(this.PowerShell.HadErrors);

        this.AssertSingleError<InvalidOperationException>(e => Assert.Equal("Invalid type. Expected Integer but got String. Path 'data'.", e.Message));

        // value hasn't changed
        Assert.Equal(1L, result.Property<long>("data"));

        // file hasn't changed
        this.AssertJsonFileContent(c => Assert.Equal(originalRoot, c.ToString()));
    }

    [Fact]
    public void Powershell_setting_item_property_fails()
    {
        // ARRANGE
        var root = this.ArrangeFileSystem(new JObject
        {
            ["data"] = 1
        });

        // ACT
        var result = this.PowerShell.AddCommand("Set-ItemProperty")
            .AddParameter("Path", @"test:\")
            .AddParameter("Name", "missing")
            .AddParameter("Value", "text")
            .AddStatement()
            .AddCommand("Get-Item")
            .AddParameter("Path", @"test:\")
            .Invoke()
            .Single();

        // ASSERT
        // value has changed
        Assert.True(this.PowerShell.HadErrors);

        var error = this.PowerShell.Streams.Error.Single();

        Assert.Equal("Can't set property(name='missing'): it doesn't exist", error.Exception.Message);
    }

    [Fact]
    public void Powershell_creates_item_property_on_Force()
    {
        // ARRANGE
        var root = this.ArrangeFileSystem(new JObject
        {
        });

        // ACT
        var result = this.PowerShell.AddCommand("Set-ItemProperty")
            .AddParameter("Path", @"test:\")
            .AddParameter("Name", "data")
            .AddParameter("Value", "text")
            .AddParameter("Force")
            .AddStatement()
            .AddCommand("Get-Item")
            .AddParameter("Path", @"test:\")
            .Invoke()
            .Single();

        // ASSERT
        // value has changed
        Assert.False(this.PowerShell.HadErrors);
        Assert.Equal("text", result.Property<string>("data"));
    }

    #endregion Set-ItemProperty -Path -Name -Force
}