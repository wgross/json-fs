using Newtonsoft.Json.Linq;
using System.Linq;
using System.Management.Automation;
using TreeStore.Core;
using Xunit;

namespace TreeStore.JsonFS.Test
{
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
            Assert.Equal(new object[] { 1, 2 }, result.Property<object[]>("array"));
            Assert.True(result.Property<bool>("PSIsContainer"));
            Assert.Equal("test", result.Property<PSDriveInfo>("PSDrive").Name);
            Assert.Equal("JsonFS", result.Property<ProviderInfo>("PSProvider").Name);
            Assert.Equal(@"TreeStore.JsonFS\JsonFS::test:\", result.Property<string>("PSPath"));
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
            Assert.Equal(new object[] { 1, 2 }, result.Property<object[]>("array"));
        }

        #endregion Clear-ItemProperty -Path -Name

        #region Set-ItemProperty -Path -Name

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

        #endregion Set-ItemProperty -Path -Name
    }
}