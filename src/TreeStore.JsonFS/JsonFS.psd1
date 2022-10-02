#
# Module manifest for module 'JsonFS'
#
# Generated by: github.com/wgross
#
# Generated on: 24.07.2022
#

@{
    RootModule = 'TreeStore.JsonFS.dll'
    ModuleVersion = '0.3.0'
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
0.2.0: Support for Set-ItemContent            
0.3.0: Create new node with Set-ItemContent
 - base object JsonFsItem having properties Name and PropertyNames'
        }
    }
}

