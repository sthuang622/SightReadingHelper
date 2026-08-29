using System.ComponentModel;
using System.Runtime.CompilerServices;
using SightReadingHelper.Services;

namespace SightReadingHelper.Models;

public class CalibrationEntryViewModel : INotifyPropertyChanged
{
    private string _measuredText = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string NoteName { get; set; } = string.Empty;

    public string DisplayNoteName => PitchMath.ToStringLabel(NoteName);

    public int ReferenceNumber { get; set; }

    public string ReferenceDisplay => $"Reference {ReferenceNumber}";

    public int MidiNote { get; set; }

    public double ExpectedFrequencyHz { get; set; }

    public string MeasuredText
    {
        get => _measuredText;
        set
        {
            if (_measuredText == value)
            {
                return;
            }

            _measuredText = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ResultDisplay));
        }
    }

    public string ExpectedDisplay => "Play for 1 second";

    public string ResultDisplay => string.IsNullOrWhiteSpace(MeasuredText)
        ? "Not calibrated"
        : $"Measured {MeasuredText} Hz";

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
