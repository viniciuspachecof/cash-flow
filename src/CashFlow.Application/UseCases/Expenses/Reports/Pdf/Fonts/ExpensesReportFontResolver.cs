using PdfSharp.Fonts;

namespace CashFlow.Application.UseCases.Expenses.Reports.Pdf.Fonts;
public class ExpensesReportFontResolver : IFontResolver
{
    public byte[]? GetFont(string faceName)
    {
        var assembly = typeof(ExpensesReportFontResolver).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith($"{faceName}.ttf", StringComparison.OrdinalIgnoreCase));

        if (resourceName is null)
        {
            throw new NotImplementedException($"Font '{faceName}' is not embedded as a resource.");
        }

        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);

        return memoryStream.ToArray();
    }

    public FontResolverInfo? ResolveTypeface(string familyName, bool bold, bool italic)
    {
        return new FontResolverInfo(familyName);
    }
}
