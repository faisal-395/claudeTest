namespace GrainMarket.Client;

// Fully qualified: unqualified "Application" would otherwise resolve to the sibling
// GrainMarket.Application project's namespace (a nested namespace under the shared GrainMarket
// root wins over Microsoft.Maui.Controls.Application in C#'s lookup), not the MAUI base class.
public partial class App : Microsoft.Maui.Controls.Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new MainPage()) { Title = "Umer Farooq & Brothers — Grain Market Management" };
        window.Width = 1366;
        window.Height = 800;
        return window;
    }
}
