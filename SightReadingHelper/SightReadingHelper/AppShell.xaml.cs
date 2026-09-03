namespace SightReadingHelper;

public partial class AppShell : Shell
{
    public AppShell(
        MainPage mainPage,
        CalibrationPage calibrationPage,
        PracticePage practicePage,
        MusicXmlPage musicXmlPage,
        SettingsPage settingsPage)
    {
        InitializeComponent();

        Items.Add(new TabBar
        {
            Items =
            {
                CreateShellContent("Home", "home", mainPage),
                CreateShellContent("Calibration", "calibration", calibrationPage),
                CreateShellContent("Practice", "practice", practicePage),
                CreateShellContent("MusicXML", "musicxml", musicXmlPage),
                CreateShellContent("Settings", "settings", settingsPage)
            }
        });
    }

    private static ShellContent CreateShellContent(string title, string route, Page page)
    {
        return new ShellContent
        {
            Title = title,
            Route = route,
            Content = page
        };
    }
}
