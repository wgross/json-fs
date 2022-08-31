if(Test-Path "D:\src\TreeStore.Core\submodules\PowerShell\src\powershell-win-core\bin\Debug\net6.0\win7-x64\pwsh.exe") {
    Write-host "Using debug pwsh"
    D:\src\TreeStore.Core\submodules\PowerShell\src\powershell-win-core\bin\Debug\net6.0\win7-x64\pwsh.exe -noprofile -Interactive -NoExit -WorkingDirectory $PSScriptRoot -File $PSScriptRoot/init.ps1
}
else {
    Write-host "Using installed pwsh"
    pwsh.exe -noprofile -Interactive -NoExit -WorkingDirectory $PSScriptRoot -File $PSScriptRoot/init.ps1
}