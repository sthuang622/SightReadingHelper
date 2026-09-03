using SightReadingHelper.Models;
using SightReadingHelper.Services;

namespace SightReadingHelper;

public partial class MainPage : ContentPage
{
    private const string GeneratedPracticeMode = "Generated notes";
    private const string MusicXmlLoopMode = "MusicXML loop";

    private readonly PracticeDataService _practiceDataService;
    private IReadOnlyList<InstrumentProfile> _instruments = Array.Empty<InstrumentProfile>();
    private PracticeOptionsConfig _practiceOptions = new();
    private PracticeSettings _settings = new();
    private bool _isLoadingHomeOptions;
    private bool? _isCompactLayout;

    public MainPage(PracticeDataService practiceDataService)
    {
        InitializeComponent();
        _practiceDataService = practiceDataService;
        AttachHoverEffects();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadHomeAsync();
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
        HeroTitleLabel.FontSize = isCompact ? 28 : 36;
        SelectedInstrumentLabel.FontSize = isCompact ? 20 : 24;

        HeroGrid.ColumnDefinitions.Clear();
        HomeActionGrid.ColumnDefinitions.Clear();
        HomeActionGrid.RowDefinitions.Clear();
        HomeOptionsGrid.ColumnDefinitions.Clear();
        HomeOptionsGrid.RowDefinitions.Clear();

        if (isCompact)
        {
            HeroGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            HeroGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            HomeActionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            HomeActionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(54) });
            HomeActionGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            HomeActionGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Grid.SetRow(StartPracticeButton, 0);
            Grid.SetColumn(StartPracticeButton, 0);
            Grid.SetColumnSpan(StartPracticeButton, 2);
            Grid.SetRow(CalibrateButton, 1);
            Grid.SetColumn(CalibrateButton, 0);
            Grid.SetColumnSpan(CalibrateButton, 1);
            Grid.SetRow(SettingsButton, 1);
            Grid.SetColumn(SettingsButton, 1);
            Grid.SetColumnSpan(SettingsButton, 1);

            HomeOptionsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            for (var row = 0; row < 6; row++)
            {
                HomeOptionsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }

            MoveOptionCard(ClefCard, 0, 0);
            MoveOptionCard(RangeCard, 1, 0);
            MoveOptionCard(InstrumentTypeCard, 2, 0);
            MoveOptionCard(InstrumentCard, 3, 0);
            MoveOptionCard(NoteMovementCard, 4, 0);
            MoveOptionCard(TempoCard, 5, 0);
            return;
        }

        HeroGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        HeroGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        HomeActionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        HomeActionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
        HomeActionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(54) });
        HomeActionGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        Grid.SetRow(StartPracticeButton, 0);
        Grid.SetColumn(StartPracticeButton, 0);
        Grid.SetColumnSpan(StartPracticeButton, 1);
        Grid.SetRow(CalibrateButton, 0);
        Grid.SetColumn(CalibrateButton, 1);
        Grid.SetColumnSpan(CalibrateButton, 1);
        Grid.SetRow(SettingsButton, 0);
        Grid.SetColumn(SettingsButton, 2);
        Grid.SetColumnSpan(SettingsButton, 1);

        HomeOptionsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        HomeOptionsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        for (var row = 0; row < 3; row++)
        {
            HomeOptionsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        MoveOptionCard(ClefCard, 0, 0);
        MoveOptionCard(RangeCard, 0, 1);
        MoveOptionCard(InstrumentTypeCard, 1, 0);
        MoveOptionCard(InstrumentCard, 1, 1);
        MoveOptionCard(NoteMovementCard, 2, 0);
        MoveOptionCard(TempoCard, 2, 1);
    }

    private static void MoveOptionCard(BindableObject card, int row, int column)
    {
        Grid.SetRow(card, row);
        Grid.SetColumn(card, column);
        Grid.SetColumnSpan(card, 1);
    }

    private async Task LoadHomeAsync()
    {
        _isLoadingHomeOptions = true;
        _instruments = await _practiceDataService.GetInstrumentsAsync();
        _practiceOptions = await _practiceDataService.GetPracticeOptionsAsync();
        _settings = await _practiceDataService.GetPracticeSettingsAsync();
        var instrument = _instruments.FirstOrDefault(item => item.InstrumentName == _settings.InstrumentName)
            ?? _instruments.First();

        LoadQuickOptions(instrument);

        SelectedInstrumentLabel.Text = instrument.InstrumentName;
        InstrumentSummaryLabel.Text = $"Built for {instrument.DefaultClef.ToLowerInvariant()} sight-reading and single-note pitch work.";
        ClefValueLabel.Text = instrument.DefaultClef;
        _isLoadingHomeOptions = false;
    }

    private void LoadQuickOptions(InstrumentProfile instrument)
    {
        HomeInstrumentTypePicker.ItemsSource = _instruments
            .Select(item => item.InstrumentType)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(item => item)
            .Select(GetInstrumentTypeLabel)
            .ToList();
        HomeInstrumentTypePicker.SelectedItem = GetInstrumentTypeLabel(instrument.InstrumentType);

        LoadInstrumentPickerItems(instrument.InstrumentType, instrument.InstrumentName);

        PracticeButtonLabel.Text = GetPracticeButtonText(_settings.UseCustomMusicXmlLoop);
        HomeBeatTempoPicker.ItemsSource = GetNumberOptions(_practiceOptions.BeatTempoBpms, _settings.BeatTempoBpm, "BPM");
        var jumpLabels = GetMaxJumpLabels(_practiceOptions, instrument);
        HomeMaxJumpPicker.ItemsSource = jumpLabels;

        HomeBeatTempoPicker.SelectedItem = $"{_settings.BeatTempoBpm} BPM";
        HomeMaxJumpPicker.SelectedIndex = Math.Clamp(
            _settings.MaxBaseNoteJump ?? 1,
            0,
            Math.Max(0, jumpLabels.Count - 1));
        HomeBiggerRangeSwitch.IsToggled = !_settings.UseBeginnerRange;
    }

    private void LoadInstrumentPickerItems(string instrumentType, string? selectedInstrumentName)
    {
        var instrumentNames = _instruments
            .Where(item => item.InstrumentType.Equals(instrumentType, StringComparison.OrdinalIgnoreCase))
            .Select(item => item.InstrumentName)
            .ToList();

        HomeInstrumentPicker.ItemsSource = instrumentNames;
        HomeInstrumentPicker.SelectedItem = selectedInstrumentName is not null && instrumentNames.Contains(selectedInstrumentName)
            ? selectedInstrumentName
            : instrumentNames.FirstOrDefault();
    }

    private async void OnHomeOptionChanged(object sender, EventArgs e)
    {
        await SaveHomeOptionsAsync();
    }

    private async void OnHomeInstrumentTypeChanged(object sender, EventArgs e)
    {
        if (_isLoadingHomeOptions || HomeInstrumentTypePicker.SelectedItem is not string selectedInstrumentTypeLabel)
        {
            return;
        }

        _isLoadingHomeOptions = true;
        LoadInstrumentPickerItems(GetInstrumentTypeFromLabel(selectedInstrumentTypeLabel), null);
        _isLoadingHomeOptions = false;

        await SaveHomeOptionsAsync();
    }

    private async void OnHomeOptionToggled(object sender, ToggledEventArgs e)
    {
        await SaveHomeOptionsAsync();
    }

    private async Task SaveHomeOptionsAsync()
    {
        if (_isLoadingHomeOptions
            || HomeInstrumentPicker.SelectedItem is not string instrumentName
            || HomeBeatTempoPicker.SelectedItem is not string beatTempo
            || HomeMaxJumpPicker.SelectedIndex < 0)
        {
            return;
        }

        _settings = new PracticeSettings
        {
            InstrumentName = instrumentName,
            ExerciseLength = _settings.ExerciseLength,
            ToleranceCents = _settings.ToleranceCents,
            BeatTempoBpm = int.Parse(beatTempo.Split(' ')[0]),
            GeneratedBeatDurations = _settings.GeneratedBeatDurations,
            MaxBaseNoteJump = HomeMaxJumpPicker.SelectedIndex,
            AllowedBaseNoteNames = _settings.AllowedBaseNoteNames,
            UseBeginnerRange = !HomeBiggerRangeSwitch.IsToggled,
            AllowSharps = _settings.AllowSharps,
            AvoidOpenStringSharps = _settings.AvoidOpenStringSharps,
            ShowNoteName = _settings.ShowNoteName,
            UseCustomMusicXmlLoop = _settings.UseCustomMusicXmlLoop,
            CustomMusicXml = _settings.CustomMusicXml
        };

        await _practiceDataService.SavePracticeSettingsAsync(_settings);
        await LoadHomeAsync();
    }

    private async void OnStartPracticeTapped(object sender, TappedEventArgs e)
    {
        await SaveHomeOptionsAsync();
        var route = _settings.UseCustomMusicXmlLoop
            ? "//musicxml"
            : "//practice?newSession=true";

        await Shell.Current.GoToAsync(route);
    }

    private async void OnPracticeModeTapped(object sender, TappedEventArgs e)
    {
        var selectedMode = await DisplayActionSheet(
            "Practice mode",
            "Cancel",
            null,
            GeneratedPracticeMode,
            MusicXmlLoopMode);

        if (selectedMode is null || selectedMode == "Cancel")
        {
            return;
        }

        _settings.UseCustomMusicXmlLoop = selectedMode == MusicXmlLoopMode;
        PracticeButtonLabel.Text = GetPracticeButtonText(_settings.UseCustomMusicXmlLoop);
        await SaveHomeOptionsAsync();
    }

    private async void OnOpenCalibrationClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//calibration");
    }

    private async void OnOpenSettingsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//settings");
    }

    private void AttachHoverEffects()
    {
        if (DeviceInfo.Current.Platform == DevicePlatform.Android
            || DeviceInfo.Current.Platform == DevicePlatform.iOS)
        {
            return;
        }

        AttachButtonHoverEffect(
            StartPracticeButton,
            GetColor("Primary"),
            GetColor("Accent"),
            GetColor("White"));
        AttachButtonHoverEffect(
            CalibrateButton,
            GetColor("Secondary"),
            GetColor("Accent"),
            GetColor("PrimaryDarkText"));
        AttachButtonHoverEffect(
            SettingsButton,
            GetColor("Secondary"),
            GetColor("Accent"),
            GetColor("PrimaryDarkText"));

    }

    private static void AttachButtonHoverEffect(
        View button,
        Color defaultBackgroundColor,
        Color hoverBackgroundColor,
        Color textColor)
    {
        button.BackgroundColor = defaultBackgroundColor;
        if (button is Button platformButton)
        {
            platformButton.TextColor = textColor;
        }

        var pointerGesture = new PointerGestureRecognizer();

        pointerGesture.PointerEntered += async (_, _) =>
        {
            button.BackgroundColor = hoverBackgroundColor;
            await Task.WhenAll(
                button.ScaleTo(1.018, 110, Easing.CubicOut),
                button.TranslateTo(0, -2, 110, Easing.CubicOut));
        };

        pointerGesture.PointerExited += async (_, _) =>
        {
            button.BackgroundColor = defaultBackgroundColor;
            await Task.WhenAll(
                button.ScaleTo(1, 130, Easing.CubicOut),
                button.TranslateTo(0, 0, 130, Easing.CubicOut));
        };

        button.GestureRecognizers.Add(pointerGesture);
    }

    private static List<string> GetNumberOptions(
        IEnumerable<int> configuredValues,
        int selectedValue,
        string suffix)
    {
        var values = new SortedSet<int>(configuredValues.Where(value => value > 0));

        if (selectedValue > 0)
        {
            values.Add(selectedValue);
        }

        return values
            .Select(value => $"{value} {suffix}")
            .ToList();
    }

    private static List<string> GetMaxJumpLabels(Models.PracticeOptionsConfig practiceOptions, Models.InstrumentProfile instrument)
    {
        return practiceOptions.MaxTuningNoteJumpLabelsByInstrumentType.TryGetValue(instrument.InstrumentType, out var labels)
            ? labels
            : practiceOptions.MaxTuningNoteJumpLabelsByInstrumentType["string"];
    }

    private static string GetInstrumentTypeLabel(string instrumentType)
    {
        return string.IsNullOrWhiteSpace(instrumentType)
            ? "Other"
            : $"{char.ToUpperInvariant(instrumentType[0])}{instrumentType[1..].ToLowerInvariant()}";
    }

    private static string GetInstrumentTypeFromLabel(string instrumentTypeLabel)
    {
        return instrumentTypeLabel.ToLowerInvariant();
    }

    private static string GetPracticeButtonText(bool useCustomMusicXmlLoop)
    {
        return useCustomMusicXmlLoop
            ? $"Practice: {MusicXmlLoopMode}"
            : $"Practice: {GeneratedPracticeMode}";
    }

    private static Color GetColor(string key)
    {
        return (Color)Application.Current!.Resources[key];
    }
}
