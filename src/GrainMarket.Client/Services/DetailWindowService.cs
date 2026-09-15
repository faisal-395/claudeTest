namespace GrainMarket.Client.Services;

// Fully qualified Microsoft.Maui.Controls.Application/Window throughout — see App.xaml.cs's own
// comment: unqualified "Application" resolves to the sibling GrainMarket.Application project's
// namespace instead of the MAUI base class from inside GrainMarket.Client.
public class DetailWindowService : IDetailWindowService
{
    public void OpenPurchaseDetail(int purchaseId) => Open("Purchase Details", "purchase", purchaseId);
    public void OpenSaleDetail(int saleInvoiceId) => Open("Sale Invoice Details", "sale", saleInvoiceId);

    private static void Open(string title, string recordType, int recordId)
    {
        var page = new DetailWindowPage(recordType, recordId);
        var window = new Microsoft.Maui.Controls.Window(page) { Title = title, Width = 900, Height = 700 };
        page.CloseRequested += (_, _) => Microsoft.Maui.Controls.Application.Current?.CloseWindow(window);
        Microsoft.Maui.Controls.Application.Current?.OpenWindow(window);
    }
}
