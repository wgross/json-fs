using Newtonsoft.Json.Linq;
using System.Linq;
using System.Management.Automation;
using TreeStore.Core;
using Xunit;

namespace TreeStore.JsonFS.Test;

[Collection(nameof(PowerShell))]
public class ItemCmdletProviderTest : PowerShellTestBase
{
    #region Get-Item -Path

    [Fact]
    public void Powershell_reads_root_node()
    {
        // ARRANGE
        this.ArrangeFileSystem(DefaultRoot());

        // ACT
        var result = this.PowerShell.AddCommand("Get-Item")
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
        Assert.Equal(@"TreeStore.JsonFS\JsonFS::test:\", psobject.Property<string>("PSPath"));
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

        Assert.Equal("object", psobject.Property<string>("PSChildName"));
        Assert.True(psobject.Property<bool>("PSIsContainer"));
        Assert.Equal("test", psobject.Property<PSDriveInfo>("PSDrive").Name);
        Assert.Equal("JsonFS", psobject.Property<ProviderInfo>("PSProvider").Name);
        Assert.Equal(@"TreeStore.JsonFS\JsonFS::test:\object", psobject.Property<string>("PSPath"));
        Assert.Equal(@"TreeStore.JsonFS\JsonFS::test:", psobject.Property<string>("PSParentPath"));
    }

    [Fact]
    public void Powershell_reads_root_grand_child_node()
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

        Assert.Equal("object", psobject.Property<string>("PSChildName"));
        Assert.True(psobject.Property<bool>("PSIsContainer"));
        Assert.Equal("test", psobject.Property<PSDriveInfo>("PSDrive").Name);
        Assert.Equal("JsonFS", psobject.Property<ProviderInfo>("PSProvider").Name);
        Assert.Equal(@"TreeStore.JsonFS\JsonFS::test:\object\object", psobject.Property<string>("PSPath"));
        Assert.Equal(@"TreeStore.JsonFS\JsonFS::test:\object", psobject.Property<string>("PSParentPath"));
    }

    #endregion Get-Item -Path

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
}