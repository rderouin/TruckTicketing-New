#r "nuget: EPPlus, 5.0.6"

using System;
using System.Collections.Generic;
using System.IO;
using OfficeOpenXml;

// ================================================================================
static Dictionary<string, string> ReadExcelToDictionary(string filePath, string sheetName, string keyColumnHeader, string valueColumnHeader, bool includeHeader = false)
{
    Dictionary<string, string> dataDictionary = new Dictionary<string, string>();
    List<string> headers = new List<string>();

    using (var package = new ExcelPackage(new FileInfo(filePath)))
    {
        var workbook = package.Workbook;
        var worksheet = workbook.Worksheets[sheetName];

        int startRow = includeHeader ? 1 : 2;

        for (int row = startRow; row <= worksheet.Dimension.End.Row; row++)
        {
            if (includeHeader && row == 1)
            {
                // Collect headers
                headers = worksheet.Cells[row, 1, row, worksheet.Dimension.End.Column]
                                   .Select(cell => cell.Text)
                                   .ToList();
            }
            else
            {
                int keyColumnIndex = headers.IndexOf(keyColumnHeader);
                int valueColumnIndex = headers.IndexOf(valueColumnHeader);

                if (keyColumnIndex != -1 && valueColumnIndex != -1)
                {
                    string key = worksheet.Cells[row, keyColumnIndex + 1].Text;
                    string value = worksheet.Cells[row, valueColumnIndex + 1].Text;

                    dataDictionary[key] = value;
                }
                else
                {
                    Console.WriteLine("Key or Value column not found in headers.");
                }
            }
        }
    }

    return dataDictionary;
}