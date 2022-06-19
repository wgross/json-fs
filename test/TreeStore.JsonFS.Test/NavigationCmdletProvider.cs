using Newtonsoft.Json.Linq;
using System.Linq;
using TreeStore.Core;
using Xunit;

namespace TreeStore.JsonFS.Test
{
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
            Assert.False(root.TryGetValue("child1", out var _));
            Assert.Equal("text", result.Property<string>("property"));
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
                .AddParameter("Destination", @"test:\child2\newname")
                .AddStatement()
                .AddCommand("Get-Item")
                .AddParameter("Path", @"test:\child2\newname")
                .Invoke()
                .Single();

            // ASSERT
            Assert.False(this.PowerShell.HadErrors);
            Assert.Equal("text", result.Property<string>("property"));
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
        //        .AddParameter("Destination", @"test:\child2\newname")
        //        .Invoke()
        //        .ToArray();

        //    // ASSERT
        //    Assert.False(this.PowerShell.HadErrors);
        //    Assert.False(root.TryGetValue("child1", out var _));
        //    Assert.Same(child1, ((JObject)root.Property("child2").Value).Property["newname"]);
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
        //        .AddParameter("Destination", @"test:\child2\child3\newname")
        //        .Invoke()
        //        .ToArray();

        //    // ASSERT
        //    Assert.False(this.PowerShell.HadErrors);
        //    Assert.False(root.TryGetValue("child1", out var _));
        //    Assert.Same(child1, ((JObject)((JObject)root["child2"]!)["child3"])["newname"]);
        //}

        #endregion Move-Item -Path -Destination
    }
}