using Microsoft.Maui.ApplicationModel.DataTransfer;
using SightReadingHelper.Models;
using SightReadingHelper.Services;

namespace SightReadingHelper;

public partial class MusicXmlPage : ContentPage
{
    private readonly PracticeDataService _practiceDataService;
    private readonly MusicXmlService _musicXmlService;
    private IReadOnlyList<InstrumentProfile> _instruments = Array.Empty<InstrumentProfile>();
    private PracticeSettings _settings = new();
    private bool? _isCompactLayout;
    private bool _isCurrentMusicXmlValid;

    public MusicXmlPage(
        PracticeDataService practiceDataService,
        MusicXmlService musicXmlService)
    {
        InitializeComponent();
        _practiceDataService = practiceDataService;
        _musicXmlService = musicXmlService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _instruments = await _practiceDataService.GetInstrumentsAsync();
        _settings = await _practiceDataService.GetPracticeSettingsAsync();
        MusicXmlEditor.Text = string.Empty;
        UpdateMusicXmlStatus(string.Empty);
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        ApplyResponsiveLayout(width);
    }

    private void ApplyResponsiveLayout(double width)
    {
        var isCompact = width > 0 && width < 720;
        if (_isCompactLayout == isCompact)
        {
            return;
        }

        _isCompactLayout = isCompact;
        MusicXmlTitleLabel.FontSize = isCompact ? 26 : 30;
        MusicXmlEditor.HeightRequest = isCompact ? 220 : 300;

        MusicXmlHeaderGrid.ColumnDefinitions.Clear();
        MusicXmlHeaderGrid.RowDefinitions.Clear();

        if (isCompact)
        {
            MusicXmlHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            MusicXmlHeaderGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            MusicXmlHeaderGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var homeButton = MusicXmlHeaderGrid.Children.OfType<Button>().FirstOrDefault();
            if (homeButton is not null)
            {
                Grid.SetRow(homeButton, 1);
                Grid.SetColumn(homeButton, 0);
                homeButton.HorizontalOptions = LayoutOptions.Start;
                homeButton.Margin = new Thickness(0, 10, 0, 0);
            }

            return;
        }

        MusicXmlHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        MusicXmlHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        MusicXmlHeaderGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var desktopHomeButton = MusicXmlHeaderGrid.Children.OfType<Button>().FirstOrDefault();
        if (desktopHomeButton is not null)
        {
            Grid.SetRow(desktopHomeButton, 0);
            Grid.SetColumn(desktopHomeButton, 1);
            desktopHomeButton.HorizontalOptions = LayoutOptions.Fill;
            desktopHomeButton.Margin = Thickness.Zero;
        }
    }

    private async void OnImportFromClipboardClicked(object sender, EventArgs e)
    {
        var clipboardText = await Clipboard.Default.GetTextAsync();
        if (string.IsNullOrWhiteSpace(clipboardText))
        {
            MusicXmlStatusLabel.Text = "Clipboard is empty.";
            MusicXmlStatusLabel.TextColor = GetColor("Tertiary");
            StartLoopPracticeButton.IsEnabled = false;
            return;
        }

        MusicXmlEditor.Text = clipboardText;
        UpdateMusicXmlStatus(clipboardText);
    }

    private void OnMusicXmlTextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateMusicXmlStatus(e.NewTextValue ?? string.Empty);
    }

    private async void OnStartLoopPracticeClicked(object sender, EventArgs e)
    {
        var musicXml = MusicXmlEditor.Text ?? string.Empty;
        if (!TryValidateMusicXml(musicXml, out _, out var errorMessage))
        {
            MusicXmlStatusLabel.Text = errorMessage;
            MusicXmlStatusLabel.TextColor = GetColor("Tertiary");
            StartLoopPracticeButton.IsEnabled = false;
            return;
        }

        _settings.UseCustomMusicXmlLoop = true;
        _settings.CustomMusicXml = string.Empty;
        await _practiceDataService.SavePracticeSettingsAsync(_settings);
        _practiceDataService.SetSessionMusicXml(musicXml);
        await Shell.Current.GoToAsync("//practice?newSession=true");
    }

    private async void OnHomeClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//home");
    }

    private void UpdateMusicXmlStatus(string musicXml)
    {
        if (string.IsNullOrWhiteSpace(musicXml))
        {
            _isCurrentMusicXmlValid = false;
            MusicXmlStatusLabel.Text = "Paste or import MusicXML to start.";
            MusicXmlStatusLabel.TextColor = GetColor("Gray600");
            StartLoopPracticeButton.IsEnabled = false;
            return;
        }

        _isCurrentMusicXmlValid = TryValidateMusicXml(musicXml, out var playableNoteCount, out var errorMessage);
        StartLoopPracticeButton.IsEnabled = _isCurrentMusicXmlValid;

        if (_isCurrentMusicXmlValid)
        {
            MusicXmlStatusLabel.Text = $"{playableNoteCount} notes ready.";
            MusicXmlStatusLabel.TextColor = GetColor("Primary");
            return;
        }

        MusicXmlStatusLabel.Text = errorMessage;
        MusicXmlStatusLabel.TextColor = GetColor("Tertiary");
    }

    private bool TryValidateMusicXml(string musicXml, out int playableNoteCount, out string errorMessage)
    {
        playableNoteCount = 0;
        errorMessage = string.Empty;

        try
        {
            var midiNotes = _musicXmlService.ParseDisplayMidiNotes(musicXml);
            if (midiNotes.Count == 0)
            {
                errorMessage = "No playable notes found.";
                return false;
            }

            var instrument = _instruments.FirstOrDefault(item => item.InstrumentName == _settings.InstrumentName);
            if (instrument is null)
            {
                errorMessage = "Select an instrument first.";
                return false;
            }

            playableNoteCount = midiNotes.Count(midiNote =>
                midiNote >= instrument.LowestMidiNote && midiNote <= instrument.HighestMidiNote);

            if (playableNoteCount == 0)
            {
                errorMessage = "No notes fit the selected instrument.";
                return false;
            }

            return true;
        }
        catch
        {
            errorMessage = "MusicXML is not valid yet.";
            return false;
        }
    }

    private static Color GetColor(string key)
    {
        return (Color)Application.Current!.Resources[key];
    }
}
