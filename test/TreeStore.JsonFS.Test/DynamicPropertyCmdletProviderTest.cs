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

    #endregion Move-ItemProperty -Path -Destination -Name

    #region New-ItemProperty -Path -Name -Value

    [Fact]
    public void Powershell_creates_item_property()
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