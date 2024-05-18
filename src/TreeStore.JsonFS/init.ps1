# Load the module to test or debug
Import-Module $PSScriptRoot/JsonFS.psd1

# Initialize the environment
New-PSDrive -PSProvider "JsonFS" -Name json -Root "./test.json"

# Output process id for attaching the debugger
$PID
