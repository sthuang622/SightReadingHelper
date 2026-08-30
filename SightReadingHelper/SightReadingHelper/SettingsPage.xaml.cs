using SightReadingHelper.Models;
using SightReadingHelper.Services;

namespace SightReadingHelper;

public partial class SettingsPage : ContentPage
{
    private readonly PracticeDataService _practiceDataService;
    private IReadOnlyList<InstrumentProfile> _instruments = Array.Empty<InstrumentProfile>();
    private PracticeOptionsConfig _practiceOptions = new();
    private PracticeSettings _settings = new();
    private readonly Dictionary<string, CheckBox> _baseNoteCheckBoxes = [];

    public SettingsPage(PracticeDataService practiceDataService)
    {
        InitializeComponent();
        _practiceDataService = practiceDataService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadSettingsAsync();
    }

    private async Task LoadSettingsAsync()
    {
        _instruments = await _practiceDataService.GetInstrumentsAsync();
        _practiceOptions = await _practiceDataService.GetPracticeOptionsAsync();
        _settings = await _practiceDataService.GetPracticeSettingsAsync();

        InstrumentPicker.ItemsSource = _instruments.Select(instrument => instrument.InstrumentName).ToList();
        ExerciseLengthPicker.ItemsSource = GetNumberOptions(_practiceOptions.ExerciseLengths, _settings.ExerciseLength, "notes");
        TolerancePicker.ItemsSource = GetNumberOptions(_practiceOptions.ToleranceCents, _settings.ToleranceCents, "cents");
        BeatTempoPicker.ItemsSource = GetNumberOptions(_practiceOptions.BeatTempoBpms, _settings.BeatTempoBpm, "BPM");
        var instrument = _instruments.FirstOrDefault(item => item.InstrumentName == _settings.InstrumentName)
            ?? _instruments.First();
        var jumpLabels = GetMaxJumpLabels(_practiceOptions, instrument);
        MaxBaseNoteJumpPicker.ItemsSource = jumpLabels;

        InstrumentPicker.SelectedItem = _settings.InstrumentName;
        ExerciseLengthPicker.SelectedItem = $"{_settings.ExerciseLength} notes";
        TolerancePicker.SelectedItem = $"{_settings.ToleranceCents} cents";
        BeatTempoPicker.SelectedItem = $"{_settings.BeatTempoBpm} BPM";
        MaxBaseNoteJumpPicker.SelectedIndex = Math.Clamp(
            _settings.MaxBaseNoteJump ?? 1,
            0,
            Math.Max(0, jumpLabels.Count - 1));
        BeginnerRangeSwitch.IsToggled = _settings.UseBeginnerRange;
        ShowNoteNameSwitch.IsToggled = _settings.ShowNoteName;
        AllowSharpsSwitch.IsToggled = _settings.AllowSharps;
        RenderBaseNoteOptions();
    }

    private async void OnSaveSettingsClicked(object sender, EventArgs e)
    {
        if (InstrumentPicker.SelectedItem is not string instrumentName
            || ExerciseLengthPicker.SelectedItem is not string exerciseLength
            || TolerancePicker.SelectedItem is not string tolerance
            || BeatTempoPicker.SelectedItem is not string beatTempo
            || MaxBaseNoteJumpPicker.SelectedIndex < 0)
        {
            return;
        }

        var settings = new PracticeSettings
        {
            InstrumentName = instrumentName,
            ExerciseLength = int.Parse(exerciseLength.Split(' ')[0]),
            ToleranceCents = int.Parse(tolerance.Split(' ')[0]),
            BeatTempoBpm = int.Parse(beatTempo.Split(' ')[0]),
            MaxBaseNoteJump = MaxBaseNoteJumpPicker.SelectedIndex,
            AllowedBaseNoteNames = GetSelectedBaseNoteNames(),
            UseBeginnerRange = BeginnerRangeSwitch.IsToggled,
            AllowSharps = AllowSharpsSwitch.IsToggled,
            ShowNoteName = ShowNoteNameSwitch.IsToggled
        };

        await _practiceDataService.SavePracticeSettingsAsync(settings);
        await DisplayAlert("Saved", "Saved.", "OK");
    }

    private void OnInstrumentSelectionChanged(object sender, EventArgs e)
    {
        RefreshJumpOptionsForSelectedInstrument();
        RenderBaseNoteOptions();
    }

    private async void OnHomeClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//home");
    }

    private void RefreshJumpOptionsForSelectedInstrument()
    {
        if (InstrumentPicker.SelectedItem is not string instrumentName)
        {
            return;
        }

        var instrument = _instruments.FirstOrDefault(item => item.InstrumentName == instrumentName);
        if (instrument is null)
        {
            return;
        }

        var selectedIndex = MaxBaseNoteJumpPicker.SelectedIndex < 0
            ? _settings.MaxBaseNoteJump ?? 1
            : MaxBaseNoteJumpPicker.SelectedIndex;
        var jumpLabels = GetMaxJumpLabels(_practiceOptions, instrument);
        MaxBaseNoteJumpPicker.ItemsSource = jumpLabels;
        MaxBaseNoteJumpPicker.SelectedIndex = Math.Clamp(selectedIndex, 0, Math.Max(0, jumpLabels.Count - 1));
    }

    private void RenderBaseNoteOptions()
    {
        BaseNoteOptionsLayout.Children.Clear();
        _baseNoteCheckBoxes.Clear();

        if (InstrumentPicker.SelectedItem is not string instrumentName)
        {
            return;
        }

        var instrument = _instruments.FirstOrDefault(item => item.InstrumentName == instrumentName);
        if (instrument is null)
        {
            return;
        }

        AllowedBaseNotesLabel.Text = $"Allowed {instrument.BaseNoteGroupLabel}";

        var allowedBaseNoteNames = new HashSet<string>(
            _settings.AllowedBaseNoteNames,
            StringComparer.OrdinalIgnoreCase);
        var allAllowed = allowedBaseNoteNames.Count == 0;

        foreach (var baseNote in instrument.BaseNotes)
        {
            var checkBox = new CheckBox
            {
                IsChecked = allAllowed || allowedBaseNoteNames.Contains(baseNote.NoteName),
                VerticalOptions = LayoutOptions.Center
            };

            var row = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                }
            };

            row.Add(new Label
            {
                Text = PitchMath.ToAnchorLabel(baseNote.NoteName, instrument.BaseNoteItemSuffix),
                Style = (Style)Application.Current!.Resources["BodyTextStyle"],
                VerticalOptions = LayoutOptions.Center
            }, 0);
            row.Add(checkBox, 1);

            _baseNoteCheckBoxes[baseNote.NoteName] = checkBox;
            BaseNoteOptionsLayout.Children.Add(row);
        }
    }

    private List<string> GetSelectedBaseNoteNames()
    {
        var selectedBaseNoteNames = _baseNoteCheckBoxes
            .Where(item => item.Value.IsChecked)
            .Select(item => item.Key)
            .ToList();

        return selectedBaseNoteNames.Count == _baseNoteCheckBoxes.Count
            ? []
            : selectedBaseNoteNames;
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

    private static List<string> GetMaxJumpLabels(PracticeOptionsConfig practiceOptions, InstrumentProfile instrument)
    {
        return practiceOptions.MaxTuningNoteJumpLabelsByInstrumentType.TryGetValue(instrument.InstrumentType, out var labels)
            ? labels
            : practiceOptions.MaxTuningNoteJumpLabelsByInstrumentType["string"];
    }
}
