@{
    RootModule = 'fs-dirs-jump.psm1'
    ModuleVersion = '1.0'
    GUID = '17b0f7de-af3e-484b-b783-d9055a50eeef'
    Author = 'github.com/wgross'
    Copyright = '(c) github.com/wgross. All rights reserved.'
    Description = 'Jump provides persistent directory shortcuts'
    RequiredModules=@(
        'JsonFS'
    )
    FunctionsToExport = @(
        'Set-Jump'
        'Get-Jump'
        'Remove-Jump'
        'Invoke-Jump'
    )
    # Aliases to export from this module
    AliasesToExport = ''
}

