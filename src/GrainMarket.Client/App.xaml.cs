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

        // Size to 90% of the till PC's actual display instead of a fixed 1366x800, so the app
        // fits monitors both smaller and larger than that. MainDisplayInfo is in raw pixels;
        // MAUI Window.Width/Height are device-independent units, so divide out the density.
        var display = DeviceDisplay.Current.MainDisplayInfo;
        var screenWidth = display.Width / display.Density;
        var screenHeight = display.Height / display.Density;

        window.Width = screenWidth * 0.9;
        window.Height = screenHeight * 0.9;
        window.X = (screenWidth - window.Width) / 2;
        window.Y = (screenHeight - window.Height) / 2;

        return window;
    }
}
