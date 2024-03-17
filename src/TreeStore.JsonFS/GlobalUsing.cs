// common parts of the framework
global using System;
global using System.Collections;
global using System.Collections.Generic;
global using System.Linq;
global using System.Text;
global using System.Threading.Tasks;
global using Microsoft.Extensions.DependencyInjection;

// main dependency: The project is a powershell cmdlet provider
global using System.Management.Automation;
global using System.Management.Automation.Provider;

// main dependency: Newtonsoft.Json is used for JSON editing
global using Newtonsoft.Json;
global using Newtonsoft.Json.Linq;

// main dependency: TreeStore.Core
global using TreeStore.Core.Capabilities;
global using TreeStore.Core.Providers;
global using TreeStore.Core.Nodes;