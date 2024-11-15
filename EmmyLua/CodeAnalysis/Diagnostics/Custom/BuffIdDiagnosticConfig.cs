using System.Collections.ObjectModel;
using DocumentFormat.OpenXml.Math;
using DocumentFormat.OpenXml.Wordprocessing;

namespace EmmyLua.CodeAnalysis.Diagnostics.Custom;
using EmmyLua.Configuration;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

public class BuffIdFunctionInfo
{
    public string FunctionName { get; set; }
    public List<int> ArgPositions = [];

    public static bool operator ==(BuffIdFunctionInfo lhs, BuffIdFunctionInfo rhs)
    {
        return lhs.FunctionName == rhs.FunctionName && lhs.ArgPositions.SequenceEqual(rhs.ArgPositions);
    }

    public static bool operator !=(BuffIdFunctionInfo lhs, BuffIdFunctionInfo rhs)
    {
        return lhs.FunctionName != rhs.FunctionName || !lhs.ArgPositions.SequenceEqual(rhs.ArgPositions);
    }
}
public class BuffIdDiagnosticConfig
{
    public Collection<long> ValidBuffIds { get; } = new();
    public List<BuffIdFunctionInfo> FunctionInfos { get; } = new();
    public List<string> DesignerScriptPaths { get; } = [];

    public void LoadBuffIds(Setting setting)
    {
        var buffIdConfigFileInfo = setting.CustomDiagnostics.BuffIdConfig.BuffIdConfigFileInfo;
        if (buffIdConfigFileInfo.Path == string.Empty || buffIdConfigFileInfo.Sheet == string.Empty || buffIdConfigFileInfo.Path == string.Empty)
        {
            return;
        }

        if (setting.CustomDiagnostics.BuffIdConfig.Functions.Count == 0)
        {
            return;
        }

        using (SpreadsheetDocument doc = SpreadsheetDocument.Open(buffIdConfigFileInfo.Path, false))
        {
            var workbookpart = doc.WorkbookPart;
            Sheet sheet = workbookpart.Workbook.Descendants<Sheet>().Where(s => s.Name == buffIdConfigFileInfo.Sheet).FirstOrDefault();
            WorksheetPart worksheetPart = workbookpart.GetPartById(sheet.Id) as WorksheetPart;
            Worksheet workSheet = worksheetPart.Worksheet;
            foreach (var row in workSheet.Descendants<Row>())
            {
                foreach (var cell in row.Descendants<Cell>().Where(c => c.CellValue is not null && (c.DataType is null || c.DataType == CellValues.Number) && c.CellReference.Value.StartsWith(buffIdConfigFileInfo.Column)))
                {
                    string value = cell.CellValue.InnerText;
                    int buffID;
                    if (int.TryParse(value, out buffID))
                    {
                        ValidBuffIds.Add(buffID);
                    }
                }
            }
        }

        foreach (var config in setting.CustomDiagnostics.BuffIdConfig.Functions)
        {
            BuffIdFunctionInfo info = new();
            info.FunctionName = config.FuncName;
            info.ArgPositions.AddRange(config.ArgPos);
            FunctionInfos.Add(info);
        }

        DesignerScriptPaths.AddRange(setting.CustomDiagnostics.BuffIdConfig.DesignerScriptPaths);
    }
}

