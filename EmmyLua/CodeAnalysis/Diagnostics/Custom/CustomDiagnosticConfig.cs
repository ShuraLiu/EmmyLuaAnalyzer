using System.Collections.ObjectModel;
using EmmyLua.Configuration;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace EmmyLua.CodeAnalysis.Diagnostics.Custom;

public class CustomDiagnosticConfig
{
    public BuffIdDiagnosticConfig BuffIdDiagnosticConfig = new();

    public void LoadConfig(Setting setting)
    {
        BuffIdDiagnosticConfig.LoadBuffIds(setting);
    }
}
