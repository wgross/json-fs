# JsonFS

Mounts an existing JSON file as PowerShell drive for editing with the PowerShell `*-Item`and `*-ItemProperty` commands.

## Install from PowerShell Gallery

The current version of [JsonFs](https://www.powershellgallery.com/packages/JsonFS) can be downloaded form PowerShell Gallery or install with a PowerShellGet command:

```PowerShell
Install-Module JsonFS
```

## Building the Module

To build the module just publish the project with the dotnet CLI

```powershell
dotnet publish .\src\TreeStore.JsonFS\
```

The implementation depends on base classes from [TreeStore.Core](https://www.nuget.org/packages/TreeStore.Core)

## An Example Module

The folder `example` contains an small powershell module which is using `TreeStore.JsonFS` as its persistence. 
It stores provides directory aliases in a json file.

## Mounting a JSON File

To mount a json file `test.json` use the command:

```powershell
Import-Module TreeStore.JsonFs
New-PSDrive -PSProvider "JsonFs" -Name json -Root "./test.json"
```

The provider won't create the JSON file nor will it create missing directories in the `Root` path.
The current location can be set to the JSON drive with the `Set-Location` command.

```powershell
cd json:\
```

The provider also creates a global function for setting the current location to the JsonFS drive:

```powershell
json: <Return>
```

A JsonFS drive only knows container nodes (directories).
All JSON objects are shown as containers, all JSON properties having a non-scalar value type are shown as child containers.
All JSON properties having a scalar value type (number, string etc) are shown as PowerShell properties of the file system item.

For arrays a similar semantic is applied: if the first value of an array is a scalar value the whole array is shows as property value.
If the first item of the array is an object type the array is show as a collection of child containers having names `"0","1",..`.

A JSON object like this..

```json
{
    "object": {
        "value1" : 1,
        "text1" : "text1"
    },
    "value2":2.0
    "text2":"text2"
}
```

..would create a file system with:

- object: a sub directory of the Root when calling `Get-ChildItem -Path /`
- object.value1, object.text1: properties of the item when calling `Get-Item -Path /object`
- value2, text2: properties of the root item when calling `Get-Item -Path /`

## Accessing JSON Filesystem Nodes

The common PowerShell `*-Item`-Commands can be used to modify the JSON filesystem and with it the underlying JSON file.
Changes are made persistent immediately.
Implemented are:

- `Clear-Item`: remove JSON node content
- `Copy-Item`: create a copy of JSON node at another place of the JSON filesystem
- `Get-Item`: `PSObject` from a JSON node
- `Move-Item`: unlink the JSON node from its current parent and link as a child to another parent node
- `New-Item`: create a node child JSON node
- `Remove-Item`: remove a JSON node completely, including child nodes (`-Recurse`)
- `Rename-Item`: change the property name of a JSON node at its parent
- `Set-Item`: replace the a JSON nodes value properties wit a new set of value properties

Also properties may be modified using PowerShell `*-ItemProperty` commands.
Implemented are:

- `Clear-ItemProperty`: clear a properties content
- `Copy-ItemProperty`: clone the property at another node and/or with a new name
- `Get-ItemProperty`: get a JSON properties value
- `Move-ItemProperty`: move a property to another JSON node
- `New-ItemProperty`: create a new property
- `Remove-ItemProperty`: remove a property from a JSON node
- `Rename-ItemProperty`: change the name of a JSON property
- `Set-ItemProperty`: ste the value of a JSON property (-Force creates a new property)

### Copy JSON data between two instances of the JsonFS provider

It is possible to copy a Container (recursively or not) to another JsonFS drive:

```powershell
Copy-Item -Path json-1:\parent\child -Destination json-2:\another\container -Recurse
```

Also moving is implemented between to JSON file systems

### Splatting Value Properties

For configuration scenarios it is possible to retrieve the value properties of a JSON node as a hash table for splatting:

```PowerShell
$parameters = Get-Item json:\path\to\node -AsHashtable

Any-Powershell-Function @parameters
```

### Working with JSON Schemas

JsonFs supports JSON Schema validation before writing changes nack to the JSON file.
A Schema may be supplied either by a parameter while drive creation or from with the JSON file.

```PowerShell
New-PSDrive -PSProvider "JsonFs" -Name json -Root "./test.json" -JsonSchema ./test-schema.json
```

From within the json file the `Uri` must ether be a rooted `file:` scheme or an web address using `http:` or `https:`.
The `$schema`attribute is expected in the root node of the JSON document.

```json
{
    "$schema":"file:///c:/data/test.json"
}
```

If the schema validation fails the change is thrown away ad the the file is reread from the disk.

# Releases

- 0.1.0: first release
- 0.2.0:
  - Set-Item: replaces all value properties, doesn't touch child properties
  - New-Item: like Set-Item, only value properties are created
- 0.3.0:
  - Support for Get-/Set-/Clear-Content cmdlets using JSON text added
  - fs items have base object of type 'JsonFsItem'
    - having a property 'Name'
    - having a property 'PropertyName' with all value property names
  - format for fs items
    - table format with Columns 'Name' and 'PropertyNames'.
  - improvements with value arrays: value array are converted from `object[]` provider by powershell from `@(..)`
- 0.4.0:
  - create global function `<drive name>:` to change location to newly created JsonFS drive
- 0.5.0:
  - Support for splatting with `Get-Item -AsHashtable`
  - Set-ItemProperty raise exception if property is unkwoen and -Force is not given
  - New-Item with hash table value `New-Item -Value @ { key = "value" }`
  - Set-Item with hash table value `Set-Item -Value @ { key = "value" }`
- v0.5.1:
  - Updates dependencies (.Net7)
  - Uses PSChildName as default name property to avoid overlapping with a custom `name`-property
- v0.5.2:
  - fixes linux by updating TreeStore.Core to v0.5.0
- v0.6.0:
  - validates JSON schema before writing to file.
- v0.6.1:
  - replaces JSON.Net schema with NJsonSchema b/c of licensing
