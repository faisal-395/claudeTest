namespace GrainMarket.Client.Services;

/// <summary>Opens a record's detail page in its own native desktop window instead of navigating
/// within the main window — this is a MAUI Windows app, not a browser, so there's no such thing as
/// a new tab; "open in a new page" here means a real second OS window.</summary>
public interface IDetailWindowService
{
    void OpenPurchaseDetail(int purchaseId);
    void OpenSaleDetail(int saleInvoiceId);
}
