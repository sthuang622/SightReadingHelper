using SightReadingHelper.Services;

namespace SightReadingHelper;

public partial class MainPage : ContentPage
{
    private readonly PracticeDataService _practiceDataService;

    public MainPage(PracticeDataService practiceDataService)
    {
        InitializeComponent();
        _practiceDataService = practiceDataService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadHomeAsync();
    }

    private async Task LoadHomeAsync()
    {
        var instrument = await _practiceDataService.GetSelectedInstrumentAsync();
        var calibration = await _practiceDataService.GetCalibrationAsync(instrument.InstrumentName);
        var settings = await _practiceDataService.GetPracticeSettingsAsync();
        var upperNoteName = settings.UseBeginnerRange && !string.IsNullOrWhiteSpace(instrument.BeginnerHighestNoteName)
            ? instrument.BeginnerHighestNoteName
            : instrument.HighestNoteName;

        SelectedInstrumentLabel.Text = instrument.InstrumentName;
        InstrumentSummaryLabel.Text = $"Built for {instrument.DefaultClef.ToLowerInvariant()} sight-reading and single-note pitch work.";
        SettingsSummaryLabel.Text = $"{settings.ExerciseLength} notes, ±{settings.ToleranceCents} cents, hold 1 beat at {settings.BeatTempoBpm} BPM, {GetAccidentalModeLabel(settings)}, {(settings.UseBeginnerRange ? "beginner" : "full")} range, {GetBaseJumpLabel(settings.MaxBaseNoteJump ?? 1).ToLowerInvariant()}, strings: {GetAllowedBaseNotesLabel(settings)}.";
        ClefValueLabel.Text = instrument.DefaultClef;
        RangeValueLabel.Text = $"{PitchMath.ToDisplayNoteName(instrument.LowestNoteName)} to {PitchMath.ToDisplayNoteName(upperNoteName)}";
        BaseNotesValueLabel.Text = string.Join("  ", instrument.BaseNotes.Select(note => PitchMath.ToStringLabel(note.NoteName)));
        CalibrationStatusLabel.Text = calibration is null
            ? "Not started"
            : $"{calibration.Notes.Count} notes saved";
    }

    private async void OnOpenPracticeClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//practice");
    }

    private async void OnOpenCalibrationClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//calibration");
    }

    private async void OnOpenSettingsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//settings");
    }

    private static string GetBaseJumpLabel(int maxBaseNoteJump)
    {
        return maxBaseNoteJump switch
        {
            0 => "Same string only",
            1 => "Adjacent strings only",
            2 => "Skip one string",
            _ => "Any string"
        };
    }

    private static string GetAllowedBaseNotesLabel(Models.PracticeSettings settings)
    {
        return settings.AllowedBaseNoteNames.Count == 0
            ? "all"
            : string.Join(", ", settings.AllowedBaseNoteNames.Select(PitchMath.ToStringLabel));
    }

    private static string GetAccidentalModeLabel(Models.PracticeSettings settings)
    {
        if (!settings.AllowSharps)
        {
            return "natural notes only";
        }

        return settings.AvoidOpenStringSharps ? "no open-string sharps" : "sharps allowed";
    }
}
