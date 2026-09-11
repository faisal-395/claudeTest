namespace GrainMarket.Client.Services;

/// <summary>
/// Tracks whether the app's nav sidebar should render collapsed — used by wide batch-entry
/// screens (e.g. Pakki's grid) to reclaim horizontal space while rows are being added, then
/// restored once the operator saves or leaves the page.
/// </summary>
public class UiState
{
    public bool SidebarCollapsed { get; private set; }

    public event Action? Changed;

    public void CollapseSidebar()
    {
        if (SidebarCollapsed) return;
        SidebarCollapsed = true;
        Changed?.Invoke();
    }

    public void ExpandSidebar()
    {
        if (!SidebarCollapsed) return;
        SidebarCollapsed = false;
        Changed?.Invoke();
    }
}
