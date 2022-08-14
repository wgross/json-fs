<#
.SYNOPSIS
	Build the nuget package of the project
	Requires some local-onel modules
#>

[CmdletBinding()]
param()

Import-Module fs-dirs
Import-Module dotnet-fs

# create a fresh publish of the module
"$PSScriptRoot/src/TreeStore.JsonFS" | fs-dirs\Invoke-AtContainer {
	# Clean older builds
	dotnet clean
	dotnet publish
	
	# get the project version
	$script:projectVersion = dotnet-fs\Get-DotNetProjectItem -CSproj | dotnet-csproj\Get-DotNetProjectVersion
}

if(Test-Path -Path "$PSScriptRoot\json-fs\$($script:projectVersion.VersionPrefix)") {
	Remove-Item "$PSScriptRoot\json-fs\$($script:projectVersion.VersionPrefix)" -Recurse -Force
}

$packageDirectory = New-Item "$PSScriptRoot\json-fs\$($script:projectVersion.VersionPrefix)" -ItemType Directory
$packageDirectory | Write-Debug

"$PSScriptRoot\src\TreeStore.JsonFS\bin\debug\net6.0\publish"  | fs-dirs\Invoke-AtContainer {
	Get-ChildItem -File | Copy-Item -Destination $packageDirectory -Exclude @("run.ps1","init.ps1","TreeStore.JsonFS.deps.json") -Force
}


