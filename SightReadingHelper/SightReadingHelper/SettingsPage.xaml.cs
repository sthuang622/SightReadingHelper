using SightReadingHelper.Models;
using SightReadingHelper.Services;

namespace SightReadingHelper;

public partial class SettingsPage : ContentPage
{
    private readonly PracticeDataService _practiceDataService;
    private IReadOnlyList<InstrumentProfile> _instruments = Array.Empty<InstrumentProfile>();
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
        _settings = await _practiceDataService.GetPracticeSettingsAsync();

        InstrumentPicker.ItemsSource = _instruments.Select(instrument => instrument.InstrumentName).ToList();
        ExerciseLengthPicker.ItemsSource = new List<string> { "5 notes", "10 notes", "20 notes" };
        TolerancePicker.ItemsSource = new List<string> { "50 cents", "30 cents", "15 cents" };
        BeatTempoPicker.ItemsSource = GetBeatTempoOptions(_settings.BeatTempoBpm);
        MaxBaseNoteJumpPicker.ItemsSource = new List<string>
        {
            "Same string only",
            "Adjacent strings only",
            "Skip one string",
            "Any string"
        };

        InstrumentPicker.SelectedItem = _settings.InstrumentName;
        ExerciseLengthPicker.SelectedItem = $"{_settings.ExerciseLength} notes";
        TolerancePicker.SelectedItem = $"{_settings.ToleranceCents} cents";
        BeatTempoPicker.SelectedItem = $"{_settings.BeatTempoBpm} BPM";
        MaxBaseNoteJumpPicker.SelectedIndex = Math.Clamp(_settings.MaxBaseNoteJump ?? 1, 0, 3);
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
        await DisplayAlert("Settings updated", $"Saved the MVP practice defaults for {instrumentName}.", "OK");
    }

    private void OnInstrumentSelectionChanged(object sender, EventArgs e)
    {
        RenderBaseNoteOptions();
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
                Text = PitchMath.ToStringLabel(baseNote.NoteName),
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

    private static List<string> GetBeatTempoOptions(int selectedBeatTempoBpm)
    {
        var beatTempoOptions = new SortedSet<int> { 60, 80, 100, 120 };

        if (selectedBeatTempoBpm > 0)
        {
            beatTempoOptions.Add(selectedBeatTempoBpm);
        }

        return beatTempoOptions
            .Select(beatTempoBpm => $"{beatTempoBpm} BPM")
            .ToList();
    }
}
