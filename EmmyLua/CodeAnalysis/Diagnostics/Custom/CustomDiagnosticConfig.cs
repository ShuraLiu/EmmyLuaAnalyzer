using System.Collections.ObjectModel;
using EmmyLua.Configuration;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace EmmyLua.CodeAnalysis.Diagnostics.Custom;

public class CustomDiagnosticConfig
{
    public Collection<int> ValidBuffIds { get; } = new();

    public void LoadConfig(Setting setting)
    {
        LoadBuffIds(setting);
    }

    private void LoadBuffIds(Setting setting)
    {
        var buffIdConfigFileInfo = setting.CustomDiagnostics.BuffIdConfig.BuffIdConfigFileInfo;
        if (buffIdConfigFileInfo.Path == string.Empty || buffIdConfigFileInfo.Sheet == string.Empty || buffIdConfigFileInfo.Path == string.Empty)
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
    }
}
