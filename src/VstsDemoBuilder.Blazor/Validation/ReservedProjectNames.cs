using System;
using System.Collections.Generic;

namespace VstsDemoBuilder.Blazor.Validation;

public static class ReservedProjectNames
{
    private static readonly HashSet<string> Names = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON",
        "PRN",
        "AUX",
        "NUL",
        "COM1",
        "COM2",
        "COM3",
        "COM4",
        "COM5",
        "COM6",
        "COM7",
        "COM8",
        "COM9",
        "COM10",
        "LPT1",
        "LPT2",
        "LPT3",
        "LPT4",
        "LPT5",
        "LPT6",
        "LPT7",
        "LPT8",
        "LPT9",
        "LTP",
        "LTP8",
        "LTP9",
        "SERVER",
        "SignalR",
        "DefaultCollection",
        "Web",
        "App_code",
        "App_Browesers",
        "App_Data",
        "App_GlobalResources",
        "App_LocalResources",
        "App_Themes",
        "App_WebResources",
        "bin",
        "web.config"
    };

    public static bool IsReserved(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        return Names.Contains(name.Trim());
    }
}
