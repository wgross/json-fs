# JsonFS

Mounts an existing JSON files as PowerShell drives.

## Mounting a JSON File

To mount a json file `test.json` use this command:

```powershell
PS> Import-Module TreeStore.JsonFs
PS> New-PSDrive -PSProvider "JsonFs" -Name json -Root "./test.json"
```

The provider won't create the JSON file nor will it create missing directories in the `Root` path.
The current location can be set to teh JSON drive with the `Set-Location` command.

```powershell
PS> cd json:\
```

The provider also creates a global function for setting the current location to the JsonFS drive

```powershell
PS> json: <Return>
```

will set the shell location to the current location at the JsonFS drive `json`.

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

## Modifying a JSON Filesystem

The common PowerShell Item-Commands can be used to modify the JSON filesystem and with it the underlying JSON file.
Changes are made persistent immediately.
Implemented are:

- Clear-Item: remove JSON node content
- Copy-Item: create a copy of JSON node at another place of the JSON filesystem
- Get-Item: `PSObject` fro a JSON node, also as an hashtable (-AsHashtable)
- Move-Item: unlink the JSON node from its current parent and link as a child to another parent node.
- New-Item: create a node child JSON node
- Remove-Item: remove a JSON node completely
- Rename-Item: change the property name of a JSON node at its parent
- Set-Item: replace the a JOS nodes content

Also properties may be modified using PowerShell Item-Property commands.
Implemented are:

- Clear-ItemProperty: clear a properties content
- Copy-ItemProperty: clone the property at another node and/or with a new name
- Get-ItemProperty: get a JSON properties value
- Move-ItemProperty: move a property to another JSON node
- New-ItemProperty: create a new property
- Remove-ItemProperty: remove a property from a JSON node
- Rename-ItemProperty: change the name of a JSON property
- Set-ItemProperty: ste the value of a JSON property (-Force creates a new property)

## Copy JSON data between two instances of the JsonFS provider

It is possible to copy a Container (recursively or not) to another JsonFS drive:

```powershell
PS> Copy-Item -Path json-1:\parent\child -Destination json-2:\another\container -Recurse
```

Also moving is implemented between to JSON file systems

## Release Notes

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
v0.5.1:
- Updates dependencies (.Net7)
- Uses PSChildName as default name property to avoid overlapping with a custom `name`-property
v0.5.2:
- fixes linux by updating TreeStore.Core to v0.5.0

## Building the module

To build the module just publish the project:

```powershell
PS> dotnet publish .\src\TreeStore.JsonFS\
```

## Splat the content of a JSON node

An JSON file system ite may be retrieved as an hashtable to splat its value properties a PowerShell function:

```PowerShell
PS> $params = Get-Item Path json:\any\json\node -AsHashtable
PS> Invoke-AnyFunction @params
```

## An Example module

The folder `example` contains an small powershell module which is using `TreeStore.JsonFS` as its persistence. It provides directory alias which are stored in a json file.
