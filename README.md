# json-fs

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

Would create a file system with:

- object: a sub directory of the Root when calling `Get-ChildItem -Path /`
- object.value1, object.text1: properties of the item when calling `Get-Item -Path /object`
- value2, text2: properties of the root item when calling `Get-Item -Path /`

Properties at items can be created using `New-ItemProperty` or `Set-ItemProperty -Force`. 
All other item property commandlets are applicable too.

Currently arrays are not supported nicely. They are shown as values (properties) instead of child items even iof the have a JSON object as array item. I plan to dynamically decide based on teh array item type (object of not) to show the item as a container of a property having array index as the items or properties name.

Features to come:

- [ ] Support for arrays as values or child containers
- [ ] Download from powershell gallery

## Building the module

Building is currently a bit complicated but will be easier after I'm considering it 'feature complete' for my uses cases.

The file system is based in [TreeStore.Core](https://github.com/wgross/TreeStore.ProviderCore) which isn't available at Nuget.org yet.
You need to download, build and create a local nuget package from it. With this package you can build the Json-Fs repository from the solution file.
