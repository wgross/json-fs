@{
    RootModule = 'TreeStore.JsonFS.dll'
    ModuleVersion = '0.8.0'
    GUID = '27ca097c-25c2-465e-8e93-b46a602cf9cd'
    Author = 'github.com/wgross'
    Copyright = '(c) github.com/wgross. All rights reserved.'
    Description = 'Mount JSON files as PowerShell drives'
    FormatsToProcess  = @('JsonFS.Format.ps1xml')
    PrivateData = @{
        PSData = @{
            ProjectUri = 'https://github.com/wgross/json-fs'
            Tags = @('PSEdition_Core','json','Provider')
            RelaseNodes = '
v0.2.0: Support for Set-ItemContent            
v0.3.0: Create new node with Set-ItemContent
- base object JsonFsItem having properties Name and PropertyNames
v0.4.0: 
- adds set-location function from drive name
v0.5.0:
- read an item as an hashtable for easier use with splatting
- Set-ItemProperty raise exception if property is unkwoen and -Force is not given
- New-Item with hash table value `New-Item -Value @ { key = "value" }`
- Set-Item with hash table value `Set-Item -Value @ { key = "value" }`
v0.5.1:
- Updates dependencies (.Net7)
- Uses PSChildName as default name property to avoid overlapping with a custom `name`-property 
v0.5.2:
- fixes linux by updating TreeStore.Core to v0.5.0
v0.6.0:
- validates JSON schema before writing to file
v0.6.1:
  - replaces JSON.Net schema with NJsonSchema b/c of licensing
v0.7.0:
  - Set-Item and New-Item accept class instances (internal conversion to PSObject)
v0.8.0:
  - Get-ItemProperty may expand single property value.
'
        }
    }
}

