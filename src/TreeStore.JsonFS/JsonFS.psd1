@{
    RootModule = 'TreeStore.JsonFS.dll'
    ModuleVersion = '0.5.0'
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
- Updates depencies (.Net7)
- Uses PSChildName as default name property to avoid overlang with a custom `name`-property 
'
        }
    }
}

