using System.Globalization;
using CashFlow.Application.UseCases.Expenses.Reports.Pdf.Fonts;
using CashFlow.Domain.Enums;
using CashFlow.Domain.Reports;
using CashFlow.Domain.Repositories.Expenses;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Fonts;

namespace CashFlow.Application.UseCases.Expenses.Reports.Pdf;
public class GenerateExpensesReportPdfUseCase : IGenerateExpensesReportPdfUseCase
{
    private const string CURRENCY_SYMBOL = "R$";
    private static readonly CultureInfo REPORT_CULTURE = new("pt-BR");
    private readonly IExpensesReadOnlyRepository _repository;

    public GenerateExpensesReportPdfUseCase(IExpensesReadOnlyRepository repository)
    {
        _repository = repository;

        GlobalFontSettings.FontResolver = new ExpensesReportFontResolver();
    }

    public async Task<byte[]> Execute(DateOnly month)
    {
        var expenses = await _repository.FilterByMonth(month);
        if (expenses.Count == 0)
        {
            return [];
        }

        var document = new Document();

        var section = document.AddSection();
        section.PageSetup.PageFormat = PageFormat.A4;
        section.PageSetup.LeftMargin = Unit.FromCentimeter(2);
        section.PageSetup.RightMargin = Unit.FromCentimeter(2);
        section.PageSetup.TopMargin = Unit.FromCentimeter(2);

        var monthLabel = month.ToString("MMMM yyyy", REPORT_CULTURE);

        var title = section.AddParagraph($"Relatório de despesas - {monthLabel}");
        title.Format.Font.Name = FontHelper.RALEWAY_BLACK;
        title.Format.Font.Size = 16;
        title.Format.SpaceAfter = 12;

        var totalAmount = expenses.Sum(expense => expense.Amount);
        var totalParagraph = section.AddParagraph($"Total gasto: {CURRENCY_SYMBOL} {totalAmount:N2}");
        totalParagraph.Format.Font.Name = FontHelper.WORKSANS_BLACK;
        totalParagraph.Format.Font.Size = 12;
        totalParagraph.Format.SpaceAfter = 16;

        var table = section.AddTable();
        table.Borders.Width = 0.5;

        table.AddColumn(Unit.FromCentimeter(4));
        table.AddColumn(Unit.FromCentimeter(2.3));
        table.AddColumn(Unit.FromCentimeter(3));
        table.AddColumn(Unit.FromCentimeter(2.3));
        table.AddColumn(Unit.FromCentimeter(3));

        var headerRow = table.AddRow();
        headerRow.Shading.Color = Color.Parse("#F5C2B6");
        headerRow.Format.Font.Name = FontHelper.WORKSANS_BLACK;
        headerRow.Format.Font.Size = 10;

        headerRow.Cells[0].AddParagraph(ResourceReportGenerationMessages.TITLE);
        headerRow.Cells[1].AddParagraph(ResourceReportGenerationMessages.DATE);
        headerRow.Cells[2].AddParagraph(ResourceReportGenerationMessages.PAYMENT_TYPE);
        headerRow.Cells[3].AddParagraph(ResourceReportGenerationMessages.AMOUNT);
        headerRow.Cells[4].AddParagraph(ResourceReportGenerationMessages.DESCRIPTION);

        foreach (var expense in expenses)
        {
            var row = table.AddRow();
            row.Format.Font.Name = FontHelper.WORKSANS_REGULAR;
            row.Format.Font.Size = 10;

            row.Cells[0].AddParagraph(expense.Title);
            row.Cells[1].AddParagraph(expense.Date.ToString("d", REPORT_CULTURE));
            row.Cells[2].AddParagraph(ConvertPaymentType(expense.PaymentType));
            row.Cells[3].AddParagraph($"{CURRENCY_SYMBOL} {expense.Amount:N2}");
            row.Cells[4].AddParagraph(expense.Description ?? string.Empty);
        }

        var renderer = new PdfDocumentRenderer { Document = document };
        renderer.RenderDocument();

        using var file = new MemoryStream();
        renderer.PdfDocument.Save(file);

        return file.ToArray();
    }

    private static string ConvertPaymentType(PaymentType payment)
    {
        return payment switch
        {
            PaymentType.Cash => ResourceReportGenerationMessages.CASH,
            PaymentType.CreditCard => ResourceReportGenerationMessages.CREDIT_CARD,
            PaymentType.DebitCard => ResourceReportGenerationMessages.DEBIT_CARD,
            PaymentType.EletronicTransfer => ResourceReportGenerationMessages.ELETRONIC_TRANSFER,
            _ => string.Empty
        };
    }
}
