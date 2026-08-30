using SightReadingHelper.Models;
using SightReadingHelper.Services;

namespace SightReadingHelper;

public partial class MainPage : ContentPage
{
    private readonly PracticeDataService _practiceDataService;
    private IReadOnlyList<InstrumentProfile> _instruments = Array.Empty<InstrumentProfile>();
    private PracticeOptionsConfig _practiceOptions = new();
    private PracticeSettings _settings = new();
    private bool _isLoadingHomeOptions;

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
            MaxBaseNoteJump = HomeMaxJumpPicker.SelectedIndex,
            AllowedBaseNoteNames = _settings.AllowedBaseNoteNames,
            UseBeginnerRange = !HomeBiggerRangeSwitch.IsToggled,
            AllowSharps = _settings.AllowSharps,
            AvoidOpenStringSharps = _settings.AvoidOpenStringSharps,
            ShowNoteName = _settings.ShowNoteName
        };

        await _practiceDataService.SavePracticeSettingsAsync(_settings);
        await LoadHomeAsync();
    }

    private async void OnOpenPracticeClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//practice?newSession=true");
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
        Button button,
        Color defaultBackgroundColor,
        Color hoverBackgroundColor,
        Color textColor)
    {
        button.BackgroundColor = defaultBackgroundColor;
        button.TextColor = textColor;

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

    private static Color GetColor(string key)
    {
        return (Color)Application.Current!.Resources[key];
    }
}
