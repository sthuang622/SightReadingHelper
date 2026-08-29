using System.Collections.ObjectModel;
using System.Globalization;
using SightReadingHelper.Controls;
using SightReadingHelper.Models;
using SightReadingHelper.Services;

namespace SightReadingHelper;

public partial class CalibrationPage : ContentPage
{
    private const int SampleRate = 44100;
    private const int MaxCalibrationCents = 80;
    private static readonly TimeSpan CalibrationStableWindow = TimeSpan.FromSeconds(1);

    private readonly PracticeDataService _practiceDataService;
    private readonly PitchDetectionService _pitchDetectionService;
    private readonly IAudioInputService _audioInputService;

    private InstrumentProfile? _currentInstrument;
    private ObservableCollection<CalibrationEntryViewModel> _entries = [];
    private int _currentCalibrationIndex;
    private bool _isListening;
    private bool _isHoldingCurrentNote;
    private DateTimeOffset _stableCandidateStartedAt;
    private readonly List<double> _stableFrequencies = [];
    private readonly StaffNotationDrawable _notationDrawable = new();

    public CalibrationPage(
        PracticeDataService practiceDataService,
        PitchDetectionService pitchDetectionService,
        IAudioInputService audioInputService)
    {
        InitializeComponent();
        _practiceDataService = practiceDataService;
        _pitchDetectionService = pitchDetectionService;
        _audioInputService = audioInputService;
        _audioInputService.SamplesAvailable += OnSamplesAvailable;
        CalibrationNotationView.Drawable = _notationDrawable;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadCalibrationAsync();
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        await StopListeningAsync();
    }

    private async Task LoadCalibrationAsync()
    {
        _currentInstrument = await _practiceDataService.GetSelectedInstrumentAsync();
        var existingCalibration = await _practiceDataService.GetCalibrationAsync(_currentInstrument.InstrumentName);

        CalibrationHeaderLabel.Text = $"{_currentInstrument.InstrumentName} reference tones";

        var entries = _currentInstrument.BaseNotes
            .Select((note, index) =>
            {
                var existingNote = existingCalibration?.Notes.FirstOrDefault(saved => saved.MidiNote == note.MidiNote);

                return new CalibrationEntryViewModel
                {
                    ReferenceNumber = index + 1,
                    NoteName = note.NoteName,
                    MidiNote = note.MidiNote + _currentInstrument.SoundingTransposeSemitones,
                    DisplayMidiNote = note.MidiNote,
                    ExpectedFrequencyHz = PitchMath.MidiToFrequency(note.MidiNote + _currentInstrument.SoundingTransposeSemitones),
                    MeasuredText = existingNote?.MeasuredFrequencyHz.ToString("0.0", CultureInfo.InvariantCulture) ?? string.Empty
                };
            })
            .ToList();

        _entries = new ObservableCollection<CalibrationEntryViewModel>(entries);
        CalibrationCollectionView.ItemsSource = _entries;
        _currentCalibrationIndex = Math.Clamp(_currentCalibrationIndex, 0, Math.Max(0, _entries.Count - 1));
        RefreshCurrentCalibrationNote();
    }

    private void RefreshCurrentCalibrationNote()
    {
        if (_entries.Count == 0 || _currentCalibrationIndex >= _entries.Count)
        {
            RenderCalibrationNotation();
            CurrentExpectedPitchLabel.Text = _entries.Count == 0
                ? "No calibration notes available."
                : "Calibration complete.";
            CalibrationMicrophoneStatusLabel.Text = _entries.Count == 0
                ? "No calibration notes available."
                : "All tuning notes are saved.";
            return;
        }

        var currentEntry = _entries[_currentCalibrationIndex];
        RenderCalibrationNotation();
        CurrentExpectedPitchLabel.Text = currentEntry.ExpectedDisplay;
        CalibrationMicrophoneStatusLabel.Text = _isListening
            ? "Listening for the highlighted note. Hold it for 1 second."
            : "Press Start Listening, then play each highlighted note for 1 second.";
    }

    private async void OnStartCalibrationClicked(object sender, EventArgs e)
    {
        if (_isListening)
        {
            return;
        }

        try
        {
            ResetStableCandidate();
            await _audioInputService.StartAsync(SampleRate);
            _isListening = true;
            StartCalibrationButton.IsEnabled = false;
            StopCalibrationButton.IsEnabled = true;
            CalibrationMicrophoneStatusLabel.Text = "Listening...";
        }
        catch (Exception ex)
        {
            CalibrationMicrophoneStatusLabel.Text = "Could not start the microphone.";
            await DisplayAlert("Microphone error", ex.Message, "OK");
        }
    }

    private async void OnStopCalibrationClicked(object sender, EventArgs e)
    {
        await StopListeningAsync();
    }

    private void OnNextCalibrationNoteClicked(object sender, EventArgs e)
    {
        MoveToNextCalibrationNote();
    }

    private async Task StopListeningAsync()
    {
        if (!_isListening)
        {
            return;
        }

        await _audioInputService.StopAsync();
        _isListening = false;
        StartCalibrationButton.IsEnabled = true;
        StopCalibrationButton.IsEnabled = false;
        CalibrationMicrophoneStatusLabel.Text = "Stopped.";
        ResetStableCandidate();
    }

    private void OnSamplesAvailable(float[] samples, int sampleRate)
    {
        var detectedPitch = _pitchDetectionService.AnalyzeSamples(samples, sampleRate);
        MainThread.BeginInvokeOnMainThread(() => HandleDetectedPitch(detectedPitch));
    }

    private async void HandleDetectedPitch(PitchDetectionResult detectedPitch)
    {
        if (!_isListening || _entries.Count == 0 || _currentCalibrationIndex >= _entries.Count)
        {
            return;
        }

        CalibrationMicrophoneLevelBar.Progress = Math.Clamp(detectedPitch.Volume * 8, 0, 1);

        if (!detectedPitch.HasPitch)
        {
            ResetStableCandidate();
            CalibrationMicrophoneStatusLabel.Text = detectedPitch.Volume < 0.012
                ? "Too quiet. Move closer or play a little stronger."
                : "Listening for a stable note...";
            return;
        }

        var currentEntry = _entries[_currentCalibrationIndex];
        var centsFromTarget = PitchMath.CalculateCentsDifference(detectedPitch.FrequencyHz, currentEntry.ExpectedFrequencyHz);

        if (Math.Abs(centsFromTarget) > MaxCalibrationCents)
        {
            ResetStableCandidate();
            CalibrationMicrophoneStatusLabel.Text = "That pitch is not close to the highlighted note yet.";
            return;
        }

        if (!_isHoldingCurrentNote)
        {
            _isHoldingCurrentNote = true;
            _stableCandidateStartedAt = DateTimeOffset.UtcNow;
            _stableFrequencies.Clear();
        }

        _stableFrequencies.Add(detectedPitch.FrequencyHz);
        var remainingHoldTime = CalibrationStableWindow - (DateTimeOffset.UtcNow - _stableCandidateStartedAt);
        CalibrationMicrophoneStatusLabel.Text = remainingHoldTime > TimeSpan.Zero
            ? $"Hold steady: {Math.Ceiling(remainingHoldTime.TotalMilliseconds / 100d) / 10d:0.0}s left"
            : "Captured.";

        if (DateTimeOffset.UtcNow - _stableCandidateStartedAt < CalibrationStableWindow)
        {
            return;
        }

        var measuredFrequency = _stableFrequencies.Average();
        currentEntry.MeasuredText = measuredFrequency.ToString("0.0", CultureInfo.InvariantCulture);
        CalibrationMicrophoneStatusLabel.Text = $"Captured at {measuredFrequency:0.0} Hz.";
        ResetStableCandidate();
        MoveToNextCalibrationNote();
        await SaveCalibrationAsync(showConfirmation: false);
    }

    private async void OnSaveCalibrationClicked(object sender, EventArgs e)
    {
        await SaveCalibrationAsync(showConfirmation: true);
    }

    private async Task SaveCalibrationAsync(bool showConfirmation)
    {
        if (_currentInstrument is null)
        {
            return;
        }

        var notes = new List<NoteCalibration>();

        foreach (var item in _entries)
        {
            if (!double.TryParse(item.MeasuredText, NumberStyles.Float, CultureInfo.InvariantCulture, out var measuredFrequencyHz))
            {
                continue;
            }

            notes.Add(new NoteCalibration
            {
                NoteName = item.NoteName,
                MidiNote = item.MidiNote,
                ExpectedFrequencyHz = item.ExpectedFrequencyHz,
                MeasuredFrequencyHz = measuredFrequencyHz,
                CentsOffset = PitchMath.CalculateCentsDifference(measuredFrequencyHz, item.ExpectedFrequencyHz)
            });
        }

        var calibration = new CalibrationProfile
        {
            InstrumentName = _currentInstrument.InstrumentName,
            CreatedAtUtc = DateTime.UtcNow,
            Notes = notes
        };

        await _practiceDataService.SaveCalibrationAsync(calibration);

        if (showConfirmation)
        {
            await DisplayAlert("Calibration saved", $"Saved {notes.Count} calibration tones for {_currentInstrument.InstrumentName}.", "OK");
        }
    }

    private void MoveToNextCalibrationNote()
    {
        if (_entries.Count == 0)
        {
            return;
        }

        _currentCalibrationIndex = Math.Min(_currentCalibrationIndex + 1, _entries.Count);
        ResetStableCandidate();
        RefreshCurrentCalibrationNote();
    }

    private void ResetStableCandidate()
    {
        _isHoldingCurrentNote = false;
        _stableCandidateStartedAt = DateTimeOffset.MinValue;
        _stableFrequencies.Clear();
    }

    private void RenderCalibrationNotation()
    {
        var notes = _entries
            .Select(entry => new ExerciseNote
            {
                SequenceNumber = entry.ReferenceNumber,
                MidiNote = entry.MidiNote,
                DisplayMidiNote = entry.DisplayMidiNote,
                NoteName = entry.NoteName,
                DisplayNoteName = entry.NoteName,
                FrequencyHz = entry.ExpectedFrequencyHz
            })
            .ToList();

        _notationDrawable.Notes = notes;
        _notationDrawable.Clef = _currentInstrument?.DefaultClef ?? "Alto clef";
        _notationDrawable.CurrentNoteIndex = _currentCalibrationIndex;
        _notationDrawable.GhostMidiNote = null;
        CalibrationNotationView.WidthRequest = StaffNotationDrawable.GetRequiredWidth(notes.Count);
        CalibrationNotationView.Invalidate();
    }
}
