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

A json drive only knows container nodes (directories). All JSON objects are shown as containers, all properties having a non-scalar value type are shown as child containers. All properties having a scalar value type (int, string etc) are show as properties of the file system item.

For arrays a similar semantic is applied: if the first value of an array is a scalar value the whole array is shows as a property. If the first item of the array is an object type the array is show as a child container having children with names `"0","1",..`.

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

## Release Notes

- 0.1.0: first release
- 0.2.0:
  - Set-Item: replaces all value properties, doesn't touch child properties
  - New-Item: like Set-Item, only value properties are created
- 0.3.0:
  - Support for Get-/Set-/Clear-Content cmdlets using JSON text added
  - fs item have and underlying type 'JsonFsItem' having a property 'Name'
  - defualt table type for 'JsonFsItem'

## Features to come

- Set-/Get-Content support

## Building the module

To build the module just publish the project:

```powershell
PS> dotnet publish .\src\TreeStore.JsonFS\
```

The folder `.\src\TreeStore.JsonFS\bin\Debug\net6.0\publish` will contain an importable build of the module.

## Example

The folder `example` contains an small powershell module which is using `TreeStore.JsonFS` as its persistence. It provides directory alias which are stored in a json file.
