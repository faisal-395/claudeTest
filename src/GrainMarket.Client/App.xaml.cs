using Microsoft.UI;
using Microsoft.UI.Windowing;
using WinRT.Interop;

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

        // Maximize via the native AppWindow once it exists, instead of computing DPI-adjusted
        // Width/Height/X/Y in CreateWindow itself. CreateWindow runs before the native window is
        // attached to a monitor, so DeviceDisplay.Current.MainDisplayInfo.Density can be wrong at
        // that point (e.g. reads 1.0 on a scaled display) — that mis-sized the window just enough
        // to desync WebView2's screen-space popups (the date-input calendar, native <select>
        // dropdowns) from where the control actually is. AppWindow.Presenter.Maximize() lets
        // Windows itself place the window on the correct monitor at the correct DPI, so nothing
        // here has to compute pixels by hand.
        window.Created += (_, _) =>
        {
            if (window.Handler?.PlatformView is not Microsoft.UI.Xaml.Window nativeWindow) return;

            var hwnd = WindowNative.GetWindowHandle(nativeWindow);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);
            if (appWindow?.Presenter is OverlappedPresenter presenter)
            {
                presenter.Maximize();
            }
        };

        return window;
    }
}
