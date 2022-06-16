using Newtonsoft.Json.Linq;
using System.Linq;
using System.Management.Automation;
using TreeStore.Core;
using Xunit;

namespace TreeStore.JsonFS.Test
{
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
            Assert.Equal(@"TreeStore.JsonFS\JsonFS::test:\object", psobject.Property<string>("PSPath"));
            Assert.Equal(@"TreeStore.JsonFS\JsonFS::test:", psobject.Property<string>("PSParentPath"));
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
            Assert.Equal(@"TreeStore.JsonFS\JsonFS::test:\object", psobject.Property<string>("PSPath"));
            Assert.Equal(@"TreeStore.JsonFS\JsonFS::test:", psobject.Property<string>("PSParentPath"));
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
            Assert.Equal(@"TreeStore.JsonFS\JsonFS::test:\object", psobject.Property<string>("PSPath"));
            Assert.Equal(@"TreeStore.JsonFS\JsonFS::test:", psobject.Property<string>("PSParentPath"));

            psobject = result[1];

            Assert.Equal("grandchild", psobject.Property<string>("PSChildName"));
            Assert.True(psobject.Property<bool>("PSIsContainer"));
            Assert.Equal("test", psobject.Property<PSDriveInfo>("PSDrive").Name);
            Assert.Equal("JsonFS", psobject.Property<ProviderInfo>("PSProvider").Name);
            Assert.Equal(@"TreeStore.JsonFS\JsonFS::test:\object\grandchild", psobject.Property<string>("PSPath"));
            Assert.Equal(@"TreeStore.JsonFS\JsonFS::test:\object", psobject.Property<string>("PSParentPath"));
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
            Assert.Equal(@"TreeStore.JsonFS\JsonFS::test:\object", psobject.Property<string>("PSPath"));
            Assert.Equal(@"TreeStore.JsonFS\JsonFS::test:", psobject.Property<string>("PSParentPath"));

            psobject = result[1];

            Assert.Equal("grandchild", psobject.Property<string>("PSChildName"));
            Assert.True(psobject.Property<bool>("PSIsContainer"));
            Assert.Equal("test", psobject.Property<PSDriveInfo>("PSDrive").Name);
            Assert.Equal("JsonFS", psobject.Property<ProviderInfo>("PSProvider").Name);
            Assert.Equal(@"TreeStore.JsonFS\JsonFS::test:\object\grandchild", psobject.Property<string>("PSPath"));
            Assert.Equal(@"TreeStore.JsonFS\JsonFS::test:\object", psobject.Property<string>("PSParentPath"));
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
            Assert.False(root.TryGetValue("object", out var _));
        }

        [Fact]
        public void Powershell_removes_root_child_node_fails_if_node_has_children()
        {
            // ARRANGE
            var root = this.ArrangeFileSystem(new JObject
            {
                ["object"] = new JObject()
                {
                    ["grandobject"] = new JObject()
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
        }

        [Fact]
        public void Powershell_removes_root_child_node_recursive()
        {
            // ARRANGE
            var root = this.ArrangeFileSystem(new JObject
            {
                ["object"] = new JObject
                {
                    ["grandobject"] = new JObject()
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
            Assert.Empty(root);
        }

        #endregion Remove-Item -Path -Recurse

        #region New-Item -Path -ItemType -Value

        [Fact]
        public void Powershell_creates_child_item()
        {
            // ARRANGE
            var root = this.ArrangeFileSystem(new JObject());
            var child = new JObject();

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
            Assert.Equal(@"TreeStore.JsonFS\JsonFS::test:\child1", psobject.Property<string>("PSPath"));
            Assert.Equal(@"TreeStore.JsonFS\JsonFS::test:", psobject.Property<string>("PSParentPath"));

            Assert.True(root.TryGetValue("child1", out var added));
            Assert.Same(child, added);
        }

        [Fact]
        public void Powershell_creating_child_fails_with_non_dictionary()
        {
            // ARRANGE
            var root = this.ArrangeFileSystem(new JObject());
            var child = new JObject();

            // ACT
            var result = Assert.Throws<CmdletProviderInvocationException>(() => this.PowerShell.AddCommand("New-Item")
                .AddParameter("Path", @"test:\child1")
                .AddParameter("Value", "value")
                .Invoke());

            // ASSERT
            Assert.True(this.PowerShell.HadErrors);
            Assert.Equal("Unexpected character encountered while parsing value: v. Path '', line 0, position 0.", result.Message);
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
            Assert.True(root.TryGetValue("newName", out var renamed));
        }

        #endregion Rename-Item -Path -NewName

        #region Copy-Item -Path -Destination -Recurse

        [Fact]
        public void Powershell_copies_child()
        {
            // ARRANGE
            var child1 = new JObject()
            {
                ["child1"] = new JObject()
            };

            var root = this.ArrangeFileSystem(new JObject
            {
                ["child1"] = child1,
                ["child2"] = new JObject()
            });

            // ACT
            var _ = this.PowerShell.AddCommand("Copy-Item")
                .AddParameter("Path", @"test:\child1")
                .AddParameter("Destination", @"test:\child2")
                .Invoke()
                .ToArray();

            // ASSERT
            Assert.False(this.PowerShell.HadErrors);
            Assert.True(root.TryGetValue("child2", out var child2token));

            var child2object = child2token as JObject;

            Assert.True(child2object.TryGetValue("child1", out var copy_child1token));
            Assert.NotNull(copy_child1token!);
            Assert.NotSame(child1, copy_child1token);
        }

        //[Fact]
        //public void Powershell_copy_child_with_new_name()
        //{
        //    // ARRANGE
        //    var child1 = new JObject()
        //    {
        //        ["child1"] = new JObject()
        //    };

        //    var root = this.ArrangeFileSystem(new JObject
        //    {
        //        ["child1"] = child1,
        //        ["child2"] = new JObject()
        //    });

        //    // ACT
        //    var _ = this.PowerShell.AddCommand("Copy-Item")
        //        .AddParameter("Path", @"test:\child1")
        //        .AddParameter("Destination", @"test:\child2\newname")
        //        .Invoke()
        //        .ToArray();

        //    // ASSERT
        //    Assert.False(this.PowerShell.HadErrors);
        //    Assert.True(root.TryGetValue<JObject>("child2", out var child2));
        //    Assert.True(child2!.TryGetValue<JObject>("newname", out var copy_child1));
        //    Assert.NotNull(copy_child1!);
        //    Assert.NotSame(child1, copy_child1);
        //}

        //[Fact]
        //public void Powershell_copies_child_recursive()
        //{
        //    // ARRANGE
        //    var child1 = new JObject()
        //    {
        //        ["grandchild"] = new JObject(),
        //        ["data"] = 1,
        //    };

        //    var root = this.ArrangeFileSystem(new JObject
        //    {
        //        ["child1"] = child1,
        //        ["child2"] = new JObject()
        //    });

        //    // ACT
        //    var _ = this.PowerShell.AddCommand("Copy-Item")
        //        .AddParameter("Path", @"test:\child1")
        //        .AddParameter("Destination", @"test:\child2")
        //        .AddParameter("Recurse")
        //        .Invoke()
        //        .ToArray();

        //    // ASSERT
        //    Assert.False(this.PowerShell.HadErrors);
        //    Assert.True(root.TryGetValue<JObject>("child2", out var child2));
        //    Assert.True(child2!.TryGetValue<JObject>("child1", out var copy_child1));
        //    Assert.NotNull(copy_child1!);
        //    Assert.NotSame(child1, copy_child1);
        //    Assert.True(copy_child1!.TryGetValue<JObject>("grandchild", out var _));
        //    Assert.True(copy_child1!.TryGetValue<int>("data", out var data));
        //    Assert.Equal(1, data);
        //}

        //[Fact]
        //public void Powershell_copies_child_item_with_new_name_and_parent_recursive()
        //{
        //    // ARRANGE
        //    var child1 = new JObject()
        //    {
        //        ["grandchild"] = new JObject(),
        //        ["data"] = 1,
        //    };

        //    var root = this.ArrangeFileSystem(new JObject
        //    {
        //        ["child1"] = child1,
        //        ["child2"] = new JObject()
        //    });

        //    // ACT
        //    var _ = this.PowerShell.AddCommand("Copy-Item")
        //        .AddParameter("Path", @"test:\child1")
        //        .AddParameter("Destination", @"test:\child2\parent\newname")
        //        .AddParameter("Recurse")
        //        .Invoke()
        //        .ToArray();

        //    // ASSERT
        //    Assert.False(this.PowerShell.HadErrors);
        //    Assert.True(root.TryGetValue<JObject>("child2", out var child2));
        //    Assert.True(child2!.TryGetValue<JObject>("parent", out var parent));
        //    Assert.True(parent!.TryGetValue<JObject>("newname", out var newname));

        //    Assert.NotNull(newname!);
        //    Assert.NotSame(child1, newname);
        //    Assert.True(newname!.TryGetValue<JObject>("grandchild", out var _));
        //    Assert.True(newname!.TryGetValue<int>("data", out var data));
        //    Assert.Equal(1, data);
        //}

        #endregion Copy-Item -Path -Destination -Recurse
    }
}