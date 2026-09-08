namespace GrainMarket.Client;

public partial class App : Application
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
