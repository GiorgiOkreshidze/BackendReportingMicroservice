using OfficeOpenXml;

namespace Reporting.Application.Formatters.Utils;

public static class ReportFormattingUtils
{
    public static string FormatPercentage(double value)
    {
        return value >= 0
            ? $"+{value:0.0%}"
            : $"{value:0.0%}";
    }
    
    public static void ApplyPercentageFormat(ExcelRange cell)
    {
        cell.Style.Numberformat.Format = "+0.0%;-0.0%";
    }
}