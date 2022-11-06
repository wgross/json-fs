# JsonFS

Mounting of JSON files as PowerShell drives.

## Using the Powershell Module

To mount a json file `test.json` use this command:

```powershell
PS> Import-Module TreeStore.JsonFs
PS> New-PSDrive -PSProvider "JsonFs" -Name json -Root "./test.json"
```

Afterwards you may navigate to the drive and use the common item commandlets:

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

A JSON object like this:

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

Properties at items can be created using `New-ItemProperty` or `Set-ItemProperty -Force`.
All other item property commandlets are applicable too.

### Copy JSON data between two instances of the JsonFS provider

It is possble to copy a Container (recursively or not) to another JsonFS drive:

```powershell
PS> Copy-Item -Path json-1:\parent\child -Destination json-2:\another\container -Recurse
```

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

## Building the module

To build the module just publish the project:

```powershell
PS> dotnet publish .\src\TreeStore.JsonFS\
```

The folder `.\src\TreeStore.JsonFS\bin\Debug\net6.0\publish` will contain an importable build of the module.

## Example

The folder `example` contains an small powershell module which is using `TreeStore.JsonFS` as its persistence. It provides directory alias which are stored in a json file.
