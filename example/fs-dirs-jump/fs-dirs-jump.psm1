# mount the default dreive with the jumps
if($null -eq (Get-PSDrive -Name "Jumps")) {
    New-PSDrive -Name "Jumps" -PSProvider "JsonFS" -Root "$PSScriptRoot\jumps.json" -Scope Global
}

# Make sure that the current computer name is there as container
if(!(Test-Path -Path "jumps:\$Env:COMPUTERNAME")) {
    New-Item -Path "jumps:\$Env:COMPUTERNAME"
}

function  create_jump_completer {
     [scriptblock]{
        param($commandName, $parameterName, $wordToComplete, $commandAst, $fakeBoundParameters)
        Get-Jump | Where-Object { $_.Name -like "*$($wordToComplete)*" } | Select-Object -ExpandProperty Name
    }
}

Register-ArgumentCompleter -CommandName @("Get-Jump","Remove-Jump","Invoke-Jump") -ParameterName "Name" -ScriptBlock (create_jump_completer)

function Set-Jump {
    <#
    .SYNOPSIS
	    Creates or changes a jump alias for the specified directory.
    .DESCRIPTION
	    Aliases are stored in the drive 'jumps'
    .PARAMETER Name
        Name of the created or changed jump destination. If no name is given the base name of the 
        specified destination is taken as the name.
    .PARAMETER Destination
        Path to the jump destination, may be an absolute path or a relative path. 
        If no destination is specified the current working directory is assumed as destination.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Position=0)]
        [string]$Name,

        [Parameter(Position=1)]
        [ValidateNotNullOrEmpty()]
        $Destination = $PWD
    )
    process {
        
        # Take the last name in the parth as $Name if $Name was omitted
        if([string]::IsNullOrEmpty($Name)) {
            $Name = Split-Path $Destination -Leaf
        }
        
        $Path = "jumps:\$Env:COMPUTERNAME\$Name"

        # Create or skip creation if the jump already exists
        if(!(Test-Path -Path $Path)) {
            New-Item -Path $Path 
        }

        # Set the item property 'destination' to the path. -Force creates the property if it 
        # doen't exists yet
        Set-ItemProperty -Path $Path -Name "destination" -Value ([string]$Destination) -Force
        Set-ItemProperty -Path $jump.Path -Name modified -Value (Get-Date).ToString("yyyy-MM-dd-hh-mm-ss") -Force
    }
}

# Provide a strongly typed return value 
class Jump {
    $Name
    $Destination
    $Path 
    Jump($name, $destination, $path) {
        $this.Name = $name
        $this.Destination = $destination
        $this.Path = $path
    }
}

function Get-Jump {
    <#
    .SYNOPSIS
	    Get specified jump adresses stored or all jumps adresses if the name isn't found    
    .PARAMETER Name
        Name of the created or changed jump destination
    #>
    [CmdletBinding(DefaultParameterSetName="asList")]
    [OutputType([Jump])]
    param(
        [Parameter(Position=0,ParameterSetName="byName")]
        [string]$Name
    )
    process {
        switch($PSCmdlet.ParameterSetName) {
            "byName" {
                if(Test-Path -Path "jumps:\$Env:COMPUTERNAME\$Name") {
                    $jumpItem = Get-Item -Path "jumps:\$Env:COMPUTERNAME\$Name"
                    [Jump]::new($jumpItem.PSChildName, $jumpItem.destination, "jumps:\$Env:COMPUTERNAME\$Name") | Write-Output
                }
            }
            "asList" {
                Get-ChildItem -Path "jumps:\$Env:ComputerName" | ForEach-Object -Process {
                    [Jump]::new($_.PSChildName, $_.destination, "jumps:\$Env:ComputerName\$($_.PSChildName)")
                }
            }
        }
    }
}

function Remove-Jump {
    <#
    .SYNOPSIS
	    Removed specified jump adresses stored or all jumps adresses if the name isn't found    
    .PARAMETER Name
        Name of the created or changed jump destination
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Name
    )
    process {
        # Just remove the container. If it doesn't exists it
        Remove-Item -Path "jumps:\$Env:COMPUTERNAME\$Name" -ErrorAction SilentlyContinue -Force
    }
}

function Invoke-Jump {
    <#
    .SYNOPSIS
	    Jumps to the specifed location.
    .DESCRIPTION
        Destination 'back' allows to jump to the position before the last jump-. This is alos the defauklt destination
        if no name was given
    .PARAMETER Name
        Name of the jump destination. 
    #>
    [CmdletBinding()]
    [Alias("j")]
    param(
        [Parameter(ValueFromPipeline,Position=0)]
        [string]$Name = "back"
    )
    process {
        if(Test-Path -Path "jumps:\$Env:COMPUTERNAME\$Name") {
            # Fetch the 'destinatin' property and set location to it.
            $jump = Get-Jump -Name $Name
            
            if($null -ne $jump) {
                if(Test-Path -Path $jump.Destination -PathType Container) {
                    # if the location change will be successful remember the current location as 'back'
                    Set-Jump -Name 'back' -Destination $PWD | Out-Null
                    Set-ItemProperty -Path $jump.Path -Name visited -Value (Get-Date).ToString("yyyy-MM-dd-hh-mm-ss") -Force
                    # now jump ...
                    Set-Location -Path $jump.Destination
                }
            }
        }
    }
}