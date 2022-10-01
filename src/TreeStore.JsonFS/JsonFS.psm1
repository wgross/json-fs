function create_jsonfs_argument_completer{
    [scriptblock]{
        param($c, $p, $w, $a, $fb)
        
        [TreeStore.JsonFS.JsonFsCmdletProvider]::CompleteArgument($c, $p, $w, $a, $fb)
    }
}

Register-ArgumentCompleter -CommandName "Get-ItemProperty" -ParameterName Name -ScriptBlock (create_jsonfs_argument_completer)
