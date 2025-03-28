﻿namespace TreeStore.JsonFS;

public sealed class JsonFsNewDriveParameters
{
    [Parameter]
    public string? JsonSchema { get; set; }

    [Parameter]
    public SwitchParameter Force { get; set; }
}